import test from 'node:test';
import assert from 'node:assert/strict';
import { gitBlobSha, parseRequestEnvelope, parseStrictJson, validateProfile } from '../lib/protocol.mjs';
import { A, body, bodyDigest, createdAt, definitionSha, now, profileBytes,
  profilePolicies, request } from './helpers.mjs';

const parse = value => parseRequestEnvelope(value, {
  createdAt, now, repository: 'owner/execution', profilePolicies: profilePolicies()
});

test('valid envelope computes exact fenced-byte digest', () => {
  const expected = request();
  const result = parse(body(expected));
  assert.deepEqual(result.request, expected);
  assert.equal(result.bodySha256, bodyDigest(expected));
  assert.equal(gitBlobSha(profileBytes), definitionSha);
});

test('request parser rejects envelope, schema and identity negatives', async t => {
  const cases = [
    ['BOM', Buffer.concat([Buffer.from([0xef, 0xbb, 0xbf]), Buffer.from(body(request()))])],
    ['invalid UTF-8', Buffer.concat([Buffer.from(body(request())), Buffer.from([0xff])])],
    ['extra text', body(request()) + '\nother'],
    ['two fences', body(request()) + '\n```json\n{}\n```'],
    ['oversize', Buffer.from(body(request()) + ' '.repeat(4096))],
    ['unknown field', body(request({ extra: true }))],
    ['example_only', body(request({ example_only: true }))],
    ['machine_selector', body(request({ machine_selector: 'laptop' }))],
    ['arbitrary command', body(request({ command: 'echo hi' }))],
    ['short SHA', body(request({ target: { repository: 'owner/execution', sha: 'abc' } }))],
    ['uppercase SHA', body(request({ target: { repository: 'owner/execution', sha: A.toUpperCase() } }))],
    ['bad UUID', body(request({ request_id: '123e4567-e89b-12d3-a456-426614174000' }))],
    ['bad version', body(request({ schema_version: 'grl.request.v2' }))],
    ['homoglyph repo', body(request({ target: { repository: 'owner/executiоn', sha: A } }))],
    ['double-dot repo', body(request({ target: { repository: 'owner/a..b', sha: A } }))],
    ['expired', body(request({ expires_at: '2026-09-24T12:00:00Z' }))],
    ['too soon', body(request({ expires_at: '2026-09-24T12:00:30Z' }))],
    ['too far', body(request({ expires_at: '2026-09-25T12:00:01Z' }))],
    ['naive expiry', body(request({ expires_at: '2026-09-24T12:10:00' }))],
    ['timeout zero', body(request({ timeout_minutes: 0 }))],
    ['timeout high', body(request({ timeout_minutes: 6 }))],
    ['unknown profile', body(request({ profile: { id: 'other', definition_sha: definitionSha } }))],
    ['profile SHA mismatch', body(request({ profile: { id: 'js-smoke', definition_sha: 'd'.repeat(40) } }))]
  ];
  for (const [name, value] of cases)
    await t.test(name, () => assert.throws(() => parse(value)));
});

test('strict parser rejects duplicate keys at every relevant nesting level', async t => {
  const raw = JSON.stringify(request());
  const duplicates = [
    raw.replace('"request_id":', '"request_id":"other","request_id":'),
    raw.replace('"repository":', '"repository":"other","repository":'),
    raw.replace('"definition_sha":', '"definition_sha":"other","definition_sha":'),
    raw.replace('"expires_at":', '"expires_at":"other","expires_at":')
  ];
  for (const [i, json] of duplicates.entries())
    await t.test(String(i), () => {
      assert.throws(() => parse('<!-- grl-request v1 -->\n```json\n' + json + '\n```'),
        { code: 'DUPLICATE_KEY' });
    });
  assert.throws(() => parseStrictJson('{"a":[{"x":1,"x":2}]}'), { code: 'DUPLICATE_KEY' });
});

test('profile is strict data-only JSON', () => {
  assert.equal(validateProfile(profileBytes).profile.id, 'js-smoke');
  const profile = JSON.parse(profileBytes.toString('utf8'));
  profile.steps[0].shell = 'powershell';
  assert.throws(() => validateProfile(Buffer.from(JSON.stringify(profile))), { code: 'UNKNOWN_FIELD' });
  delete profile.steps[0].shell;
  profile.steps[0].argv = 'node fixtures/js-smoke/run.mjs';
  assert.throws(() => validateProfile(Buffer.from(JSON.stringify(profile))), { code: 'INVALID_STEP' });
});
