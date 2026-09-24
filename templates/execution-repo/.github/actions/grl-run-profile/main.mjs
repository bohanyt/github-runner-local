import { readFileSync, writeFileSync } from 'node:fs';
import { dirname, resolve } from 'node:path';
import { fileURLToPath } from 'node:url';
import { decodedInput, failAction, output } from '../../../lib/action-io.mjs';
import { gitHead, readTestCounts, runProfile, spawnStep } from '../../../lib/execution.mjs';
import { encoded } from '../../../lib/protocol.mjs';
import { systemAdapter } from '../../../lib/system-adapter.mjs';

try {
  const identity = decodedInput('identity-b64');
  const trustedRoot = resolve(dirname(fileURLToPath(import.meta.url)), '../../..');
  const targetRoot = resolve(process.env.GITHUB_WORKSPACE, 'grl-target');
  const execution = await runProfile({
    identity,
    trustedProfileBytes: readFileSync(resolve(trustedRoot, 'profiles', identity.profile.id + '.json')),
    targetRoot,
    getHead: gitHead,
    runStep: spawnStep,
    readCounts: readTestCounts,
    system: systemAdapter(targetRoot),
    runner: {
      name: process.env.RUNNER_NAME || 'unknown',
      version: process.env.GRL_RUNNER_VERSION || 'unknown',
      identityClass: process.env.GRL_IDENTITY_CLASS || 'portable-user'
    }
  });
  if (execution.kind === 'refusal') {
    output('result_b64', '');
    output('outcome_b64', encoded(execution.outcome));
    output('execution_status', 'BLOCKED');
  } else {
    writeFileSync(resolve(process.env.RUNNER_TEMP, 'result.json'), execution.resultJson, 'utf8');
    output('result_b64', encoded(execution.result));
    output('outcome_b64', '');
    output('execution_status', execution.result.execution_status);
  }
} catch (error) { failAction(error); }
