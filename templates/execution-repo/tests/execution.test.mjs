import test from 'node:test';
import assert from 'node:assert/strict';
import { parseJUnit, parseTrx, runProfile, spawnStep } from '../lib/execution.mjs';
import { proveFixture } from '../tools/fixture-proof.mjs';
import { A, B, identity, profileBytes } from './helpers.mjs';
import { elevatedFromGroups } from '../lib/system-adapter.mjs';

function harness({ head = A, exitCode = 0, counts = { passed: 1, failed: 0,
  skipped: 0, errored: 0, source: 'junit' }, timedOut = false,
  runnerName = 'runner' } = {}) {
  const calls = [];
  const system = { isElevated: async () => false, freeDiskGiB: async () => 39 };
  const run = () => runProfile({
    identity: identity(), trustedProfileBytes: profileBytes, targetRoot: '<target>',
    getHead: async () => head,
    runStep: async (executable, argv, options) => {
      calls.push({ executable, argv, options });
      return { exitCode, stdout: '', stderr: '', truncated: false, timedOut };
    },
    readCounts: async () => counts, system,
    runner: { name: runnerName, version: '2.337.0', identityClass: 'portable-user' },
    now: () => Date.parse('2026-09-24T12:02:00Z')
  });
  return { run, calls };
}

test('exact-SHA mismatch refuses before any profile process or canonical result', async () => {
  const { run, calls } = harness({ head: B });
  const execution = await run();
  assert.equal(execution.kind, 'refusal');
  assert.equal(calls.length, 0);
  assert.equal(execution.outcome.status, 'BLOCKED');
  assert.equal(execution.outcome.reason_code, 'CHECKOUT_SHA_MISMATCH');
  assert.equal(execution.outcome.profile_executed, false);
  assert.equal(execution.outcome.requested_sha, A);
  assert.equal(execution.outcome.observed_checkout_sha, B);
  assert.equal(execution.outcome.canonical_result_present, false);
  assert.ok(!Object.hasOwn(execution, 'resultJson'));
  assert.ok(!JSON.stringify(execution).includes('tested_sha'));
});

test('reviewed profile executes argv array with shell:false and exact tested SHA', async () => {
  const { run, calls } = harness();
  const execution = await run();
  assert.equal(execution.result.execution_status, 'PASS');
  assert.equal(execution.result.target.tested_sha, A);
  assert.equal(execution.result.checks[0].tests.passed, 1);
  assert.equal(calls.length, 1);
  assert.equal(calls[0].executable, 'node');
  assert.deepEqual(calls[0].argv, ['fixtures/js-smoke/run.mjs']);
  assert.equal(calls[0].options.shell, false);
  assert.ok(Buffer.byteLength(execution.resultJson) <= 32 * 1024);
});

test('test failure, error, zero pass, nonzero exit and timeout prevent PASS', async t => {
  const cases = [
    ['failed', { counts: { passed: 1, failed: 1, skipped: 0, errored: 0, source: 'junit' } }, 'FAIL'],
    ['errored', { counts: { passed: 1, failed: 0, skipped: 0, errored: 1, source: 'junit' } }, 'FAIL'],
    ['zero passed', { counts: { passed: 0, failed: 0, skipped: 0, errored: 0, source: 'junit' } }, 'FAIL'],
    ['exit code', { exitCode: 1 }, 'FAIL'],
    ['timeout', { timedOut: true, exitCode: -1 }, 'TIMED_OUT']
  ];
  for (const [name, options, expected] of cases)
    await t.test(name, async () => {
      assert.equal((await harness(options).run()).result.execution_status, expected);
    });
});

test('missing or invalid test report after process execution remains FAIL data', async () => {
  const executed = await runProfile({
    identity: identity(), trustedProfileBytes: profileBytes, targetRoot: '<target>',
    getHead: async () => A,
    runStep: async () => ({ exitCode: 1, stdout: '', stderr: '',
      truncated: false, timedOut: false }),
    readCounts: async () => { throw Error('JUnit file missing'); },
    system: { isElevated: async () => false, freeDiskGiB: async () => 39 },
    runner: { name: 'runner', version: '2.337.0', identityClass: 'portable-user' }
  });
  assert.equal(executed.kind, 'result');
  assert.equal(executed.result.execution_status, 'FAIL');
  assert.deepEqual(executed.result.checks[0].tests,
    { passed: 0, failed: 0, skipped: 0, errored: 1, source: 'unavailable' });
});

test('JUnit and TRX counters are parsed without external packages', () => {
  assert.deepEqual(parseJUnit('<testsuite tests="4" failures="1" errors="1" skipped="1"></testsuite>'),
    { passed: 1, failed: 1, skipped: 1, errored: 1, source: 'junit' });
  assert.deepEqual(parseTrx('<Counters total="4" passed="1" failed="1" error="1" notExecuted="1" />'),
    { passed: 1, failed: 1, skipped: 1, errored: 1, source: 'trx' });
  assert.throws(() => parseJUnit('<testsuite tests="1" failures="2"></testsuite>'));
});

test('result size is bounded to 32 KiB', async () => {
  await assert.rejects(harness({ runnerName: 'x'.repeat(40_000) }).run(), { code: 'RESULT_TOO_LARGE' });
});

test('spawn output is bounded and redacts token-shaped strings', async () => {
  const processResult = await spawnStep(process.execPath,
    ['-e', "process.stdout.write('ghu_secretvalue ' + 'x'.repeat(100000))"],
    { cwd: process.cwd(), timeoutMs: 5000 });
  assert.equal(processResult.exitCode, 0);
  assert.equal(processResult.truncated, true);
  assert.ok(Buffer.byteLength(processResult.stdout) <= 64 * 1024);
  assert.ok(!processResult.stdout.includes('ghu_secretvalue'));
});

test('step timeout invokes process-tree kill adapter', async () => {
  let killed = 0;
  const processResult = await spawnStep(process.execPath,
    ['-e', 'setInterval(() => {}, 1000)'],
    { cwd: process.cwd(), timeoutMs: 200,
      killTree: child => { killed++; child.kill(); } });
  assert.equal(killed, 1);
  assert.equal(processResult.timedOut, true);
});

test('PASS and FAIL fixture proofs are real local process executions', async () => {
  const passing = await proveFixture('pass');
  const failing = await proveFixture('fail');
  assert.equal(passing.execution.result.execution_status, 'PASS');
  assert.equal(failing.execution.result.execution_status, 'FAIL');
  assert.equal(failing.execution.result.checks[0].tests.failed, 1);
  assert.equal(passing.processCalls, 1);
  assert.equal(failing.processCalls, 1);
});

test('runner metadata is measured rather than invented', async () => {
  const id = identity();
  await assert.rejects(runProfile({
    identity: id, trustedProfileBytes: profileBytes, targetRoot: '<target>',
    getHead: async () => A, runStep: async () => { throw Error('must not execute'); },
    readCounts: async () => ({}),
    system: { isElevated: async () => false },
    runner: { name: 'runner', version: 'unknown', identityClass: 'portable-user' }
  }), { code: 'RUNNER_METADATA_UNAVAILABLE' });
  assert.equal(elevatedFromGroups('S-1-16-8192'), false);
  assert.equal(elevatedFromGroups('S-1-16-12288'), true);
  assert.equal(elevatedFromGroups('S-1-16-20480'), true);
  assert.throws(() => elevatedFromGroups('no integrity SID'), /ELEVATION_MEASUREMENT_UNAVAILABLE/);
  const masked = await harness({ runnerName: 'ghp_secretvalue' }).run();
  assert.equal(masked.result.runner.name, '[REDACTED]');
});
