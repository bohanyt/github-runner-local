import { cpSync, mkdtempSync, readFileSync, rmSync, writeFileSync } from 'node:fs';
import { tmpdir } from 'node:os';
import { join, resolve } from 'node:path';
import { dirname } from 'node:path';
import { fileURLToPath } from 'node:url';
import { A, B, identity, profileBytes } from '../tests/helpers.mjs';
import { readTestCounts, runProfile, spawnStep } from '../lib/execution.mjs';

export async function proveFixture(kind) {
  if (!['pass', 'fail', 'refusal'].includes(kind)) throw new Error('Unknown proof case.');
  const root = resolve(dirname(fileURLToPath(import.meta.url)), '..');
  const owned = mkdtempSync(join(tmpdir(), 'grl-e-fixture-'));
  let processCalls = 0;
  try {
    const target = join(owned, 'target');
    cpSync(join(root, 'fixtures'), join(target, 'fixtures'), { recursive: true });
    if (kind === 'fail')
      writeFileSync(join(target, 'fixtures/js-smoke/behavior.txt'), 'FAIL\n', 'utf8');
    const run = async (executable, argv, options) => {
      processCalls++;
      return spawnStep(executable, argv, options);
    };
    const execution = await runProfile({
      identity: identity(),
      trustedProfileBytes: profileBytes,
      targetRoot: target,
      getHead: async () => kind === 'refusal' ? B : A,
      runStep: run,
      readCounts: readTestCounts,
      system: { isElevated: async () => false, freeDiskGiB: async () => 39 },
      runner: { name: 'fixture-runner', version: 'source', identityClass: 'portable-user' }
    });
    if (kind === 'refusal') {
      if (execution.kind !== 'refusal' || processCalls !== 0 ||
          execution.outcome.profile_executed !== false ||
          execution.outcome.requested_sha !== A ||
          execution.outcome.observed_checkout_sha !== B ||
          Object.hasOwn(execution, 'resultJson') ||
          JSON.stringify(execution).includes('tested_sha'))
        throw new Error('Exact-SHA refusal proof failed.');
    } else if (execution.kind !== 'result' ||
        execution.result.execution_status !== kind.toUpperCase() ||
        processCalls !== 1 ||
        execution.result.checks[0].tests[kind === 'pass' ? 'passed' : 'failed'] !== 1)
      throw new Error('Fixture execution proof failed.');
    return { kind, execution, processCalls };
  } finally {
    rmSync(owned, { recursive: true, force: false });
  }
}

if (process.argv[1] && resolve(process.argv[1]) === fileURLToPath(import.meta.url)) {
  const kind = process.argv[2];
  try {
    const proof = await proveFixture(kind);
    process.stdout.write(`fixture=${kind} proof=PASS processes=${proof.processCalls} outcome=${
      proof.execution.kind === 'result' ? proof.execution.result.execution_status : 'BLOCKED'}\n`);
  } catch (error) {
    process.stderr.write('Fixture proof failed.\n');
    process.exitCode = 1;
  }
}
