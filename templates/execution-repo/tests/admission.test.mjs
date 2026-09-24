import test from 'node:test';
import assert from 'node:assert/strict';
import { admitRequest, observeTerminal, parseMarkedComment, reconcileRecent } from '../lib/admission.mjs';
import { ACK_MARKER, RESULT_MARKER, fenced } from '../lib/protocol.mjs';
import { A, B, bodyDigest, canonicalResult, context, event, fakeApi, fakeSystem, identity,
  profileBytes, profilePolicies, request } from './helpers.mjs';

async function attempt(evt = event(request()), modify = () => {}) {
  const api = fakeApi(evt);
  const system = fakeSystem();
  const ctx = context();
  modify({ api, system, context: ctx });
  const result = await admitRequest({
    event: evt, context: ctx, api, system,
    profilePolicies: profilePolicies(),
    loadProfileBytes: async () => profileBytes
  });
  return { result, api, system, context: ctx };
}

test('contained Stage-1 SHA is admitted with bounded ACK and exact digest', async () => {
  const { result, api } = await attempt();
  assert.equal(result.admitted, true);
  assert.equal(result.identity.target.containing_branch, 'main');
  assert.equal(result.identity.target.branch_head_at_admission, B);
  assert.equal(result.identity.request_body_sha256, bodyDigest(request()));
  assert.equal(api.posted.length, 1);
  const ack = parseMarkedComment(api.posted[0].body, ACK_MARKER);
  assert.equal(ack.request_id, request().request_id);
  assert.equal(ack.run.id, 123);
  assert.equal(ack.admitted, true);
  assert.ok(!api.posted[0].body.includes('"expires_at"'));
  assert.ok(!api.posted[0].body.includes('"timeout_minutes"'));
});

test('Stage-1 repository mismatch, withdrawn and edited comments reject', async t => {
  const mismatch = await attempt(event(request({
    target: { repository: 'owner/other', sha: A }
  })));
  assert.equal(mismatch.result.reasonCode, 'STAGE1_REPOSITORY_MISMATCH');
  for (const [name, update, code] of [
    ['withdrawn', ({ api }) => { api.original = null; }, 'WITHDRAWN'],
    ['edited', ({ api }) => { api.original = { id: 77, body: 'changed' }; }, 'EDITED_REQUEST']
  ]) await t.test(name, async () => {
    assert.equal((await attempt(event(request()), update)).result.reasonCode, code);
  });
});

test('stray SHA is rejected by compare containment', async () => {
  const { result } = await attempt(event(request()), ({ api }) => { api.comparison = 'behind'; });
  assert.equal(result.reasonCode, 'SHA_NOT_CONTAINED');
});

test('replay digest, duplicate run and rerun authorization are distinct', async t => {
  const previous = identity();
  const ack = {
    schema_version: 'grl.ack.v1', ...previous, admitted: true, reason_code: 'ADMITTED'
  };
  const comment = value => ({ body: fenced(ACK_MARKER, value, 4096),
    user: { login: 'github-actions[bot]' } });
  await t.test('different digest', async () => {
    const { result } = await attempt(event(request()), ({ api }) => {
      api.comments = [comment({ ...ack, request_body_sha256: '0'.repeat(64) })];
    });
    assert.equal(result.reasonCode, 'REPLAY_DIGEST_MISMATCH');
  });
  await t.test('different run', async () => {
    const { result } = await attempt(event(request()), ({ api }) => {
      api.comments = [comment({ ...ack, run: { ...ack.run, id: 999 } })];
    });
    assert.equal(result.reasonCode, 'DUPLICATE');
  });
  await t.test('same run authorized retry', async () => {
    const { result } = await attempt(event(request()), ({ api, context: ctx }) => {
      api.comments = [comment(ack)];
      ctx.attempt = 2;
    });
    assert.equal(result.admitted, true);
    assert.equal(result.identity.run.attempt, 2);
  });
  await t.test('same run unauthorized retry', async () => {
    const { result } = await attempt(event(request()), ({ api, context: ctx }) => {
      api.comments = [comment(ack)];
      ctx.triggeringActor = 'other';
    });
    assert.equal(result.reasonCode, 'UNAUTHORIZED_RERUN');
  });
});

test('disk, elevation and capability checks refuse admission', async t => {
  const cases = [
    ['disk', ({ system }) => { system.free = 15; }, 'LOW_DISK'],
    ['elevated', ({ system }) => { system.elevated = true; }, 'ELEVATED_RUNNER'],
    ['capability', ({ system }) => { system.capability = false; }, 'MISSING_CAPABILITY']
  ];
  for (const [name, update, expected] of cases)
    await t.test(name, async () => {
      assert.equal((await attempt(event(request()), update)).result.reasonCode, expected);
    });
});

test('active ACK is not reconciled; concluded missing result is classified', async () => {
  const ack = { ...identity(), admitted: true };
  assert.deepEqual(observeTerminal({ ack, run: { concluded: false }, comments: [], status: null }),
    { outcome: 'ACTIVE', reportingComplete: false });
  assert.deepEqual(observeTerminal({ ack, run: { concluded: true, conclusion: 'success' },
    comments: [], status: null }),
  { outcome: 'REPORTING_INCOMPLETE', reportingComplete: false });
  assert.deepEqual(observeTerminal({ ack, run: { concluded: true, conclusion: 'cancelled' },
    comments: [], status: null }),
  { outcome: 'INTERRUPTED', reportingComplete: false });
});

test('bounded reconciliation posts one note and does not duplicate it', async () => {
  const api = fakeApi();
  const ack = { ...identity(), admitted: true };
  api.comments = [{ body: fenced(ACK_MARKER, ack, 4096),
    user: { login: 'github-actions[bot]' } }];
  api.run = { concluded: true, conclusion: 'cancelled' };
  const notes = await reconcileRecent({ api, mailboxIssue: 7, since: '2026-09-23T12:00:00Z' });
  assert.equal(notes.length, 1);
  assert.equal(notes[0].outcome, 'INTERRUPTED');
  assert.equal(api.posted.length, 1);
  api.comments.push(api.posted[0]);
  assert.equal((await reconcileRecent({ api, mailboxIssue: 7,
    since: '2026-09-23T12:00:00Z' })).length, 0);
});

test('durable refusal marker makes missing publication REPORTING_INCOMPLETE', async () => {
  const api = fakeApi();
  const ack = { ...identity(), admitted: true };
  api.comments = [{ body: fenced(ACK_MARKER, ack, 4096),
    user: { login: 'github-actions[bot]' } }];
  api.run = { concluded: true, conclusion: 'cancelled' };
  api.artifacts = [`grl-exec-refusal-${ack.run.id}-${ack.run.attempt}`];
  const notes = await reconcileRecent({ api, mailboxIssue: 7, since: '2026-09-23T12:00:00Z' });
  assert.equal(notes.length, 1);
  assert.equal(notes[0].outcome, 'REPORTING_INCOMPLETE');
});

test('complete canonical result is not reconciled as missing reporting', async () => {
  const api = fakeApi();
  const ack = { ...identity(), admitted: true };
  const result = canonicalResult();
  result.report = { attempts: 1, status_posted: true, comment_posted: true };
  api.comments = [
    { body: fenced(ACK_MARKER, ack, 4096), user: { login: 'github-actions[bot]' } },
    { body: fenced(RESULT_MARKER, result, 16 * 1024), user: { login: 'github-actions[bot]' } }
  ];
  api.run = { concluded: true, conclusion: 'success' };
  api.commitStatus = { sha: A, context: 'grl/js-smoke', state: 'success',
    target_url: ack.run.url };
  assert.deepEqual(await reconcileRecent({ api, mailboxIssue: 7,
    since: '2026-09-23T12:00:00Z' }), []);
  assert.equal(api.posted.length, 0);
});

test('canonical PASS with invalid test counts is not observer-complete', () => {
  const ack = { ...identity(), admitted: true };
  const result = canonicalResult();
  result.checks[0].tests.passed = 0;
  result.report = { attempts: 1, status_posted: true, comment_posted: true };
  assert.deepEqual(observeTerminal({ ack, run: { concluded: true, conclusion: 'success' },
    comments: [{ body: fenced(RESULT_MARKER, result, 16 * 1024),
      user: { login: 'github-actions[bot]' } }],
    status: { sha: A, context: 'grl/js-smoke', state: 'success', target_url: ack.run.url } }),
  { outcome: 'REPORTING_INCOMPLETE', reportingComplete: false });
});
