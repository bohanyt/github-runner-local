import test from 'node:test';
import assert from 'node:assert/strict';
import { ACK_MARKER, REFUSAL_MARKER, RESULT_MARKER, fenced } from '../lib/protocol.mjs';
import { observeTerminal, parseMarkedComment, validateRefusal } from '../lib/admission.mjs';
import { evaluateVerdict, makeRefusal, publishReport, retryPublication,
  validateCanonicalResult } from '../lib/reporting.mjs';
import { A, B, canonicalResult, fakeApi, identity } from './helpers.mjs';

const outcome = () => ({ status: 'BLOCKED', reason_code: 'CHECKOUT_SHA_MISMATCH',
  profile_executed: false, requested_sha: A, observed_checkout_sha: B,
  canonical_result_present: false });
const ack = () => ({ ...identity(), admitted: true });
const botComment = (marker, payload) => ({
  user: { login: 'github-actions[bot]' }, body: fenced(marker, payload, 16 * 1024)
});
const status = (state = 'failure') => ({ state, sha: A, context: 'grl/js-smoke',
  target_url: identity().run.url });

test('canonical PASS/FAIL results retain exact tested SHA and bounded result contract', async () => {
  for (const executionStatus of ['PASS', 'FAIL']) {
    const result = canonicalResult(executionStatus);
    assert.equal(validateCanonicalResult(result, identity()), result);
    const api = fakeApi();
    const published = await publishReport({ identity: identity(), result, api,
      issueNumber: 7, wait: async () => {} });
    assert.equal(published.executionStatus, executionStatus);
    assert.equal(published.reportingComplete, true);
    assert.equal(api.statuses[0].sha, A);
    assert.equal(api.statuses[0].state, executionStatus === 'PASS' ? 'success' : 'failure');
    const comment = parseMarkedComment(api.posted[0].body, RESULT_MARKER);
    assert.equal(comment.target.tested_sha, A);
    assert.equal(comment.report.status_posted, true);
    assert.equal(comment.report.comment_posted, true);
    assert.ok(Buffer.byteLength(api.posted[0].body) <= 16 * 1024);
    assert.equal(evaluateVerdict({ admitted: true, executionStatus,
      reportingComplete: true }).pass, executionStatus === 'PASS');
  }
  assert.throws(() => validateCanonicalResult({ ...canonicalResult(),
    target: { ...canonicalResult().target, tested_sha: B } }, identity()),
  { code: 'INVALID_CANONICAL_RESULT' });
  assert.throws(() => validateCanonicalResult({ ...canonicalResult(),
    checks: [{ ...canonicalResult().checks[0], tests: {
      ...canonicalResult().checks[0].tests, passed: 0 } }] }, identity()),
  { code: 'INVALID_PASS' });
});

test('canonical validator rejects nested schema and observer identity violations', async t => {
  const base = canonicalResult();
  const cases = [
    ['unknown nested field', { ...base, runner: { ...base.runner, token: 'x' } }],
    ['invalid run URL', { ...base, run: { ...base.run, url: 'https://example.invalid/' } }],
    ['elevated runner', { ...base, runner: { ...base.runner, elevated: true } }],
    ['out-of-order timing', { ...base, timing: { ...base.timing,
      started_at: '2026-09-24T12:02:00Z', finished_at: '2026-09-24T12:01:00Z' } }],
    ['invalid test count', { ...base, checks: [{ ...base.checks[0], tests: {
      ...base.checks[0].tests, failed: -1 } }] }]
  ];
  for (const [name, value] of cases)
    await t.test(name, () => assert.throws(() => validateCanonicalResult(value, identity())));
});

test('E-B1 refusal has the exact negative identity, no tested SHA or canonical result', async () => {
  const api = fakeApi();
  const published = await publishReport({ identity: identity(), refusal: outcome(),
    api, issueNumber: 7, wait: async () => {} });
  assert.equal(published.executionStatus, 'BLOCKED');
  assert.equal(published.reportingComplete, true);
  assert.equal(published.marker, REFUSAL_MARKER);
  assert.equal(api.statuses.length, 1);
  assert.equal(api.statuses[0].sha, A);
  assert.equal(api.statuses[0].state, 'failure');
  assert.equal(api.statuses[0].context, 'grl/js-smoke');
  const payload = parseMarkedComment(api.posted[0].body, REFUSAL_MARKER);
  assert.equal(payload.target.requested_sha, A);
  assert.equal(payload.target.observed_checkout_sha, B);
  assert.equal(payload.profile_executed, false);
  assert.equal(payload.reason_code, 'CHECKOUT_SHA_MISMATCH');
  assert.equal(Object.hasOwn(payload.target, 'tested_sha'), false);
  assert.equal(Object.hasOwn(payload, 'checks'), false);
  assert.equal(parseMarkedComment(api.posted[0].body, RESULT_MARKER), null);
  assert.ok(Buffer.byteLength(api.posted[0].body) <= 8 * 1024);
  assert.equal(evaluateVerdict({ admitted: true, executionStatus: 'BLOCKED',
    reportingComplete: true }).pass, false);
});

test('complete E-B1 refusal observes BLOCKED rather than INTERRUPTED', () => {
  const refusal = makeRefusal(identity(), outcome(), {
    attempts: 1, status_posted: true, comment_posted: true });
  assert.equal(validateRefusal(refusal, ack(), status()), true);
  assert.deepEqual(observeTerminal({ ack: ack(), run: {
    concluded: true, conclusion: 'cancelled' },
  comments: [botComment(REFUSAL_MARKER, refusal)], status: status() }),
  { outcome: 'BLOCKED', reportingComplete: true });
});

test('missing or partial E-B1 publication is REPORTING_INCOMPLETE', async t => {
  const refusal = makeRefusal(identity(), outcome(), {
    attempts: 1, status_posted: true, comment_posted: true });
  const run = { concluded: true, conclusion: 'cancelled' };
  for (const [name, comments, commitStatus] of [
    ['missing comment', [], status()],
    ['missing status', [botComment(REFUSAL_MARKER, refusal)], null],
    ['wrong status', [botComment(REFUSAL_MARKER, refusal)], status('success')],
    ['wrong status run', [botComment(REFUSAL_MARKER, refusal)],
      { ...status(), target_url: 'https://example.invalid/run' }],
    ['untrusted comment', [{ body: botComment(REFUSAL_MARKER, refusal).body,
      user: { login: 'attacker' } }], status()]
  ]) await t.test(name, () => {
    assert.deepEqual(observeTerminal({ ack: ack(), run, comments, status: commitStatus }),
      { outcome: 'REPORTING_INCOMPLETE', reportingComplete: false });
  });
  const api = fakeApi();
  api.createStatus = async () => { throw Error('offline'); };
  const published = await publishReport({ identity: identity(), refusal: outcome(),
    api, issueNumber: 7, wait: async () => {} });
  assert.equal(published.statusPosted, false);
  assert.equal(published.commentPosted, true);
  assert.equal(published.reportingComplete, false);
  assert.equal(evaluateVerdict({ admitted: true, executionStatus: 'BLOCKED',
    reportingComplete: published.reportingComplete }).pass, false);
});

test('E-B1 observer rejects mismatched identity and other refusal reasons', async t => {
  const base = makeRefusal(identity(), outcome(), {
    attempts: 1, status_posted: true, comment_posted: true });
  const run = { concluded: true, conclusion: 'success' };
  const variants = [
    ['request', { ...base, request_id: 'other' }],
    ['comment', { ...base, request_comment_id: 999 }],
    ['body', { ...base, request_body_sha256: '0'.repeat(64) }],
    ['run', { ...base, run: { ...base.run, attempt: 2 } }],
    ['profile', { ...base, profile: { ...base.profile, definition_sha: '0'.repeat(40) } }],
    ['reason', { ...base, reason_code: 'OTHER' }],
    ['executed', { ...base, profile_executed: true }],
    ['tested SHA', { ...base, target: { ...base.target, tested_sha: A } }]
  ];
  for (const [name, payload] of variants) await t.test(name, () => {
    assert.equal(validateRefusal(payload, ack(), status()), false);
    assert.deepEqual(observeTerminal({ ack: ack(), run,
      comments: [botComment(REFUSAL_MARKER, payload)], status: status() }),
    { outcome: 'REPORTING_INCOMPLETE', reportingComplete: false });
  });
  assert.throws(() => makeRefusal(identity(), { ...outcome(), reason_code: 'OTHER' },
    { attempts: 1, status_posted: true, comment_posted: true }),
  { code: 'INVALID_REFUSAL' });
});

test('publication retries independently with cap three and partial failure fails verdict', async () => {
  let calls = 0;
  const retry = await retryPublication(async () => { calls++; throw Error('offline'); },
    async () => {});
  assert.deepEqual(retry, { posted: false, attempts: 3 });
  assert.equal(calls, 3);
  const api = fakeApi();
  let commentCalls = 0;
  api.postIssueComment = async () => { commentCalls++; throw Error('offline'); };
  const published = await publishReport({ identity: identity(), result: canonicalResult(),
    api, issueNumber: 7, wait: async () => {} });
  assert.equal(api.statuses.length, 1);
  assert.equal(commentCalls, 3);
  assert.equal(published.statusPosted, true);
  assert.equal(published.commentPosted, false);
  assert.equal(published.reportingComplete, false);
  assert.equal(evaluateVerdict({ admitted: true, executionStatus: 'PASS',
    reportingComplete: false }).pass, false);
});

test('comment retry recognizes prior bot publication after uncertain response', async () => {
  const api = fakeApi();
  let calls = 0;
  api.postIssueComment = async (issue, body) => {
    calls++;
    api.comments.push({ body, user: { login: 'github-actions[bot]' } });
    throw Error('response lost');
  };
  const published = await publishReport({ identity: identity(), result: canonicalResult(),
    api, issueNumber: 7, wait: async () => {} });
  assert.equal(calls, 1);
  assert.equal(published.commentPosted, true);
  assert.equal(published.commentAttempts, 2);
});

test('verdict requires admission, PASS, and complete reporting', () => {
  for (const admitted of [false, true])
    for (const executionStatus of ['PASS', 'FAIL', 'BLOCKED', 'TIMED_OUT'])
      for (const reportingComplete of [false, true])
        assert.equal(evaluateVerdict({ admitted, executionStatus, reportingComplete }).pass,
          admitted && executionStatus === 'PASS' && reportingComplete);
});

test('verdict stays red when execution or report job fails', () => {
  assert.equal(evaluateVerdict({ admitted: true, executionStatus: 'PASS',
    reportingComplete: true, executeJobResult: 'failure' }).pass, false);
  assert.equal(evaluateVerdict({ admitted: true, executionStatus: 'PASS',
    reportingComplete: true, reportJobResult: 'failure' }).pass, false);
});

test('report comments redact token-shaped strings', async () => {
  const api = fakeApi();
  const result = canonicalResult();
  result.runner.name = 'ghp_abcdefghijklmnopqrstuvwxyz1234567890AB';
  await publishReport({ identity: identity(), result, api,
    issueNumber: 7, wait: async () => {} });
  assert.equal(api.posted[0].body.includes('ghp_'), false);
  assert.equal(api.posted[0].body.includes('[REDACTED]'), true);
});
