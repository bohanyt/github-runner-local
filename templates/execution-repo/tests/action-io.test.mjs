import test from 'node:test';
import assert from 'node:assert/strict';
import { spawnSync } from 'node:child_process';
import { mkdirSync, mkdtempSync, readFileSync, rmSync, writeFileSync } from 'node:fs';
import { tmpdir } from 'node:os';
import { join, resolve } from 'node:path';
import { decodedInput, input, optionalInput } from '../lib/action-io.mjs';
import { encoded } from '../lib/protocol.mjs';
import { root } from './helpers.mjs';

// Official actions/runner v2.337.0, src/Runner.Worker/Handlers/Handler.cs
// AddInputsToEnvironment: $"INPUT_{pair.Key?.Replace(' ', '_').ToUpperInvariant()}".
// Hyphens are preserved; only ASCII spaces become underscores.
const runnerKey = name => 'INPUT_' + name.replaceAll(' ', '_').toUpperCase();

const actions = ['grl-admit', 'grl-report', 'grl-run-profile', 'grl-verdict'];
const declaredInputs = action => {
  const yml = readFileSync(resolve(root, '.github/actions', action, 'action.yml'), 'utf8');
  const block = /^inputs:\r?\n([\s\S]*?)^runs:/m.exec(yml)?.[1] ?? '';
  return [...block.matchAll(/^  ([A-Za-z0-9_ -]+):\s*$/gm)].map(x => x[1]);
};

function withEnv(values, body) {
  const saved = new Map();
  const touched = Object.keys(values);
  for (const key of touched) saved.set(key, Object.hasOwn(process.env, key) ? process.env[key] : undefined);
  try {
    for (const [key, value] of Object.entries(values)) {
      if (value === undefined) delete process.env[key];
      else process.env[key] = value;
    }
    return body();
  } finally {
    for (const [key, value] of saved) {
      if (value === undefined) delete process.env[key];
      else process.env[key] = value;
    }
  }
}

const code = fn => {
  try { fn(); return null; } catch (error) { return error.code ?? error.message; }
};

test('action metadata declares the hyphenated inputs this boundary must carry', () => {
  assert.deepEqual(declaredInputs('grl-admit'),
    ['token', 'mailbox-issue', 'authorized-actors', 'allowed-branches', 'reserve-gib']);
  assert.deepEqual(declaredInputs('grl-report'),
    ['token', 'mailbox-issue', 'identity-b64', 'result-b64', 'outcome-b64', 'outcome-dir']);
  assert.deepEqual(declaredInputs('grl-run-profile'), ['identity-b64']);
  assert.deepEqual(declaredInputs('grl-verdict'),
    ['admitted', 'execution-status', 'reporting-complete', 'execute-job-result', 'report-job-result']);
  assert.equal(runnerKey('mailbox-issue'), 'INPUT_MAILBOX-ISSUE');
  assert.equal(runnerKey('identity-b64'), 'INPUT_IDENTITY-B64');
});

test('every declared input of all four actions resolves from its runner environment key', () => {
  for (const action of actions) {
    for (const name of declaredInputs(action)) {
      const value = `value-for-${action}-${name}`;
      const decoy = name.includes('-') ? { [runnerKey(name).replaceAll('-', '_')]: 'decoy' } : {};
      withEnv({ ...decoy, [runnerKey(name)]: value }, () => {
        assert.equal(input(name), value, `${action} required ${name}`);
        assert.equal(optionalInput(name), value, `${action} optional ${name}`);
      });
    }
  }
});

test('underscore-only decoys never satisfy hyphenated inputs', () => {
  withEnv({ 'INPUT_MAILBOX-ISSUE': undefined, INPUT_MAILBOX_ISSUE: '1' }, () => {
    assert.equal(code(() => input('mailbox-issue')), 'MISSING_ACTION_INPUT');
    assert.equal(optionalInput('mailbox-issue'), '');
  });
  withEnv({ 'INPUT_MAILBOX-ISSUE': '7', INPUT_MAILBOX_ISSUE: '1' }, () => {
    assert.equal(input('mailbox-issue'), '7');
    assert.equal(optionalInput('mailbox-issue'), '7');
  });
});

test('missing and empty values keep required and optional semantics', () => {
  withEnv({ 'INPUT_RESERVE-GIB': undefined }, () => {
    assert.equal(code(() => input('reserve-gib')), 'MISSING_ACTION_INPUT');
    assert.equal(optionalInput('reserve-gib'), '');
  });
  withEnv({ 'INPUT_RESERVE-GIB': '' }, () => {
    assert.equal(code(() => input('reserve-gib')), 'MISSING_ACTION_INPUT');
    assert.equal(optionalInput('reserve-gib'), '');
  });
  withEnv({ 'INPUT_RESERVE-GIB': ' 10 ' }, () => {
    assert.equal(input('reserve-gib'), ' 10 ');
    assert.equal(optionalInput('reserve-gib'), ' 10 ');
  });
});

test('simple names, space conversion and case follow the runner transformation', () => {
  withEnv({ INPUT_TOKEN: 't' }, () => assert.equal(input('token'), 't'));
  withEnv({ INPUT_ADMITTED: 'true' }, () => assert.equal(optionalInput('admitted'), 'true'));
  withEnv({ 'INPUT_MY_SPACED-NAME': 'x', 'INPUT_MY SPACED-NAME': undefined }, () => {
    assert.equal(input('my spaced-name'), 'x');
    assert.equal(optionalInput('My Spaced-Name'), 'x');
  });
});

test('decoded hyphenated input reads the runner key and keeps strict decoding', () => {
  const identity = { request_id: 'r', target: { requested_sha: 'a'.repeat(40) } };
  withEnv({ 'INPUT_IDENTITY-B64': encoded(identity), INPUT_IDENTITY_B64: encoded({ decoy: true }) }, () =>
    assert.deepEqual(decodedInput('identity-b64'), identity));
  withEnv({ 'INPUT_IDENTITY-B64': undefined, INPUT_IDENTITY_B64: encoded(identity) }, () =>
    assert.equal(code(() => decodedInput('identity-b64')), 'MISSING_ACTION_INPUT'));
  withEnv({ 'INPUT_IDENTITY-B64': Buffer.from('{"a":1,"a":2}').toString('base64') }, () =>
    assert.equal(code(() => decodedInput('identity-b64')), 'DUPLICATE_KEY'));
});

// Runs a real action entrypoint in a child process whose environment contains
// only the keys the runner itself would set. No network is reached.
function runAction(action, inputs, extra = {}) {
  const work = mkdtempSync(join(tmpdir(), 'grl-action-io-'));
  try {
    const outputFile = join(work, 'github-output.txt');
    writeFileSync(outputFile, '');
    const env = {
      SystemRoot: process.env.SystemRoot ?? '', PATH: process.env.PATH ?? '',
      GITHUB_OUTPUT: outputFile, GITHUB_WORKSPACE: work, ...extra(work)
    };
    for (const [name, value] of Object.entries(inputs)) env[runnerKey(name)] = value;
    const run = spawnSync(process.execPath,
      [resolve(root, '.github/actions', action, 'main.mjs')],
      { cwd: work, env, encoding: 'utf8', timeout: 30_000 });
    return { status: run.status, stderr: run.stderr, outputs: readFileSync(outputFile, 'utf8') };
  } finally { rmSync(work, { recursive: true, force: true }); }
}

test('real admit entrypoint reads all policy inputs from runner keys', () => {
  const inputs = { token: 'not-a-token', 'mailbox-issue': '1', 'authorized-actors': '["owner"]',
    'allowed-branches': '["main"]', 'reserve-gib': '10' };
  // An invalid repository makes the first post-input step fail closed offline.
  const repo = () => ({ GITHUB_REPOSITORY: 'INVALID' });
  const run = runAction('grl-admit', inputs, repo);
  assert.equal(run.status, 1);
  assert.match(run.stderr, /GRL action failed: INVALID_API_CONFIGURATION/);
  const decoy = runAction('grl-admit', { ...inputs, 'mailbox-issue': '' },
    () => ({ ...repo(), INPUT_MAILBOX_ISSUE: '1' }));
  assert.match(decoy.stderr, /GRL action failed: MISSING_ACTION_INPUT/);
});

test('real run-profile entrypoint decodes identity from its runner key', () => {
  const identity = { profile: { id: 'js-smoke' }, target: { requested_sha: 'not-a-sha' } };
  const run = runAction('grl-run-profile', { 'identity-b64': encoded(identity) }, () => ({}));
  assert.equal(run.status, 1);
  assert.match(run.stderr, /GRL action failed: INVALID_SHA/);
});

test('real report entrypoint honors identity and outcome-dir runner keys', () => {
  const identity = { execution_repo: 'owner/execution' };
  const direct = runAction('grl-report', { token: 't', 'mailbox-issue': '1',
    'identity-b64': encoded(identity) }, () => ({}));
  assert.equal(direct.status, 0, direct.stderr);
  assert.match(direct.outputs, /^reporting_complete=false$/m);
  assert.match(direct.outputs, /^execution_status=NO_EXECUTION$/m);
  // Identity exists only in the directory named by the hyphenated outcome-dir input.
  const fromDir = runAction('grl-report', { token: 't', 'mailbox-issue': '1', 'outcome-dir': 'custom-outcome' },
    work => {
      mkdirSync(join(work, 'custom-outcome'));
      writeFileSync(join(work, 'custom-outcome', 'identity.json'), JSON.stringify(identity));
      return {};
    });
  assert.equal(fromDir.status, 0, fromDir.stderr);
  assert.match(fromDir.outputs, /^execution_status=NO_EXECUTION$/m);
  const decoyDir = runAction('grl-report', { token: 't', 'mailbox-issue': '1' },
    work => {
      mkdirSync(join(work, 'custom-outcome'));
      writeFileSync(join(work, 'custom-outcome', 'identity.json'), JSON.stringify(identity));
      return { INPUT_OUTCOME_DIR: 'custom-outcome' };
    });
  assert.equal(decoyDir.status, 1);
  assert.match(decoyDir.stderr, /GRL action failed: INTERNAL_ERROR/);
});

test('real verdict entrypoint passes only with genuine runner keys', () => {
  const passing = { admitted: 'true', 'execution-status': 'PASS', 'reporting-complete': 'true',
    'execute-job-result': 'success', 'report-job-result': 'success' };
  const pass = runAction('grl-verdict', passing, () => ({}));
  assert.equal(pass.status, 0, pass.stderr);
  const decoys = () => ({ INPUT_EXECUTION_STATUS: 'PASS', INPUT_REPORTING_COMPLETE: 'true',
    INPUT_EXECUTE_JOB_RESULT: 'success', INPUT_REPORT_JOB_RESULT: 'success' });
  const decoyOnly = runAction('grl-verdict', { admitted: 'true' }, decoys);
  assert.equal(decoyOnly.status, 1);
  assert.match(decoyOnly.stderr, /GRL action failed: /);
});
