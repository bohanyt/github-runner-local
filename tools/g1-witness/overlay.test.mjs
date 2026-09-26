import test from 'node:test';
import assert from 'node:assert/strict';
import { execFileSync } from 'node:child_process';
import { mkdirSync, mkdtempSync, readFileSync, rmSync, writeFileSync } from 'node:fs';
import { tmpdir } from 'node:os';
import { dirname, join, resolve } from 'node:path';
import { fileURLToPath } from 'node:url';
import { lintSource } from '../../templates/execution-repo/tools/lint.mjs';
import {
  BASELINE_WORKFLOW_BLOB, WORKFLOW_PATH, applyOverlay, gitBlobSha, main,
  overlayDelta, restoreOverlay, validateParameters
} from './overlay.mjs';

const repoRoot = resolve(dirname(fileURLToPath(import.meta.url)), '../..');
const template = resolve(repoRoot, 'templates/execution-repo');
const git = (cwd, args) => execFileSync('git', args, { cwd, encoding: 'utf8',
  stdio: ['ignore', 'pipe', 'pipe'] }).trim();
// Exact committed bytes (independent of the checkout's line-ending conversion).
const baseline = Buffer.from(execFileSync('git',
  ['cat-file', 'blob', `HEAD:templates/execution-repo/${WORKFLOW_PATH}`], { cwd: repoRoot }));
const R = '1'.repeat(40);
const O = '2'.repeat(40);
const U = '5f0c2a9e-3b1d-4c6e-8a7f-0123456789ab';
const params = { requestedSha: R, observedSha: O, requestId: U };
const refusal = fn => { try { fn(); return null; } catch (error) { return error.code ?? error.message; } };

test('pinned baseline is the reviewed template workflow blob', () => {
  assert.equal(gitBlobSha(baseline), BASELINE_WORKFLOW_BLOB);
  assert.equal(git(repoRoot, ['rev-parse', `HEAD:templates/execution-repo/${WORKFLOW_PATH}`]),
    BASELINE_WORKFLOW_BLOB);
});

test('overlay changes only the admission fence and the grl-target checkout ref', () => {
  const overlay = applyOverlay(baseline, params);
  const { removed, added } = overlayDelta(baseline, overlay);
  assert.deepEqual(removed.map(x => x[1]), [
    "      startsWith(github.event.comment.body, '<!-- grl-request v1 -->')",
    '          ref: ${{ needs.admit.outputs.target_sha }}'
  ]);
  assert.deepEqual(added.map(x => x[1]), [
    "      startsWith(github.event.comment.body, '<!-- grl-request v1 -->') &&",
    `      contains(github.event.comment.body, '"request_id":"${U}"')`,
    `          ref: ${O}`
  ]);
  const text = overlay.toString('utf8');
  const execute = /\n  execute:\s*\n([\s\S]*?)(?=\n  report:)/.exec(text)[1];
  assert.match(execute, new RegExp(`ref: ${O}\\n          path: grl-target\\n`));
  assert.match(execute, /ref: \$\{\{ github\.sha \}\}\n          path: grl-trusted\n/);
});

test('overlay keeps every existing guard, pin, permission and runner label (template lint)', () => {
  const read = path => readFileSync(resolve(template, path), 'utf8');
  const source = workflow => ({
    workflow,
    profiles: [readFileSync(resolve(template, 'profiles/js-smoke.json'))],
    runAction: read('.github/actions/grl-run-profile/main.mjs'),
    reportLibrary: read('lib/reporting.mjs'),
    verdictAction: read('.github/actions/grl-verdict/main.mjs'),
    runtimeSources: [], changedPaths: [], drift: []
  });
  const overlay = applyOverlay(baseline, params).toString('utf8');
  assert.deepEqual(lintSource(source(baseline.toString('utf8'))), []);
  assert.deepEqual(lintSource(source(overlay)), []);
  const lines = (text, pattern) => text.split('\n').filter(x => pattern.test(x));
  const base = baseline.toString('utf8');
  for (const pattern of [/uses:/, /permissions|contents:|issues:|actions:|statuses:/, /runs-on:/,
    /persist-credentials/, /timeout-minutes/, /^on:|issue_comment|types:/, /needs:|if: /])
    assert.deepEqual(lines(overlay, pattern).filter(x => !x.includes('request_id')),
      lines(base, pattern), String(pattern));
  for (const guard of ['github.event.issue.number == fromJSON(vars.GRL_MAILBOX_ISSUE)',
    'github.event.issue.pull_request == null',
    'contains(fromJSON(vars.GRL_AUTHORIZED_ACTORS), github.event.comment.user.login)',
    "github.event.comment.author_association == 'OWNER'"])
    assert.ok(overlay.includes(guard), guard);
});

test('parameters must be canonical, distinct SHAs and a UUIDv4 nonce', () => {
  const bad = [
    [{ ...params, requestedSha: R.toUpperCase().replace(/1/g, 'A') }, 'INVALID_REQUESTED_SHA'],
    [{ ...params, requestedSha: R.slice(1) }, 'INVALID_REQUESTED_SHA'],
    [{ ...params, observedSha: 'g'.repeat(40) }, 'INVALID_OBSERVED_SHA'],
    [{ ...params, observedSha: '${{ github.sha }}' }, 'INVALID_OBSERVED_SHA'],
    [{ ...params, observedSha: R }, 'EQUAL_SHAS'],
    [{ ...params, requestId: U.toUpperCase() }, 'INVALID_REQUEST_ID'],
    [{ ...params, requestId: "x') || true || ('" }, 'INVALID_REQUEST_ID'],
    [{ ...params, requestId: U.replace('-4c6e-', '-1c6e-') }, 'INVALID_REQUEST_ID']
  ];
  for (const [value, code] of bad) {
    assert.equal(refusal(() => validateParameters(value)), code);
    assert.equal(refusal(() => applyOverlay(baseline, value)), code);
  }
});

test('drifted or line-ending-converted baselines are refused', () => {
  const drift = Buffer.from(baseline.toString('utf8').replace('timeout-minutes: 10', 'timeout-minutes: 11'));
  assert.equal(refusal(() => applyOverlay(drift, params)), 'BASELINE_DRIFT');
  const crlf = Buffer.from(baseline.toString('utf8').replaceAll('\n', '\r\n'));
  assert.equal(refusal(() => applyOverlay(crlf, params)), 'BASELINE_DRIFT');
  const twice = applyOverlay(baseline, params);
  assert.equal(refusal(() => applyOverlay(twice, params)), 'BASELINE_DRIFT');
});

test('restore returns the exact reviewed bytes and refuses anything else', () => {
  const overlay = applyOverlay(baseline, params);
  const restored = restoreOverlay(overlay, params, baseline);
  assert.ok(restored.equals(baseline));
  assert.equal(gitBlobSha(restored), BASELINE_WORKFLOW_BLOB);
  assert.equal(refusal(() => restoreOverlay(overlay, { ...params, observedSha: '3'.repeat(40) }, baseline)),
    'NOT_EXPECTED_OVERLAY');
  const tampered = Buffer.from(overlay.toString('utf8').replace('timeout-minutes: 5', 'timeout-minutes: 6'));
  assert.equal(refusal(() => restoreOverlay(tampered, params, baseline)), 'NOT_EXPECTED_OVERLAY');
  assert.equal(refusal(() => restoreOverlay(baseline, params, baseline)), 'NOT_EXPECTED_OVERLAY');
});

// End-to-end CLI round trip in a disposable Git repository shaped like the private repo.
function fixtureRepo() {
  const dir = mkdtempSync(join(tmpdir(), 'grl-g1-witness-'));
  const run = args => git(dir, args);
  run(['init', '-q', '-b', 'main']);
  run(['config', 'core.autocrlf', 'false']);
  run(['config', 'user.name', 'witness-test']);
  run(['config', 'user.email', 'witness-test@example.invalid']);
  writeFileSync(join(dir, 'README.md'), 'fixture\n');
  run(['add', '-A']); run(['commit', '-q', '-m', 'initial']);
  mkdirSync(join(dir, '.github/workflows'), { recursive: true });
  writeFileSync(join(dir, WORKFLOW_PATH), baseline);
  run(['add', '-A']); run(['commit', '-q', '-m', 'bootstrap']);
  const observed = run(['rev-parse', 'HEAD']);
  run(['commit', '-q', '--allow-empty', '-m', 'witness requested anchor']);
  const requested = run(['rev-parse', 'HEAD']);
  return { dir, run, parameters: { requestedSha: requested, observedSha: observed, requestId: U } };
}
const cli = (command, dir, p) => main([command, dir, '--requested', p.requestedSha,
  '--observed', p.observedSha, '--request-id', p.requestId]);

test('CLI plan/apply/verify/restore round-trips to the exact normal workflow', () => {
  const { dir, run, parameters } = fixtureRepo();
  try {
    const pre = run(['rev-parse', 'HEAD']);
    const plan = cli('plan', dir, parameters);
    assert.equal(plan.preOverlayCommit, pre);
    assert.equal(plan.expectedRefusal.reason_code, 'CHECKOUT_SHA_MISMATCH');
    assert.equal(run(['status', '--porcelain']), '', 'plan writes nothing');
    const applied = cli('apply', dir, parameters);
    assert.equal(applied.overlayBlob, plan.overlayBlob);
    assert.equal(run(['diff', '--name-only']), WORKFLOW_PATH);
    run(['commit', '-q', '-am', 'TEMPORARY G1 witness overlay']);
    assert.equal(run(['rev-parse', `HEAD:${WORKFLOW_PATH}`]), plan.overlayBlob);
    assert.equal(cli('verify-overlay', dir, parameters).overlayBlob, plan.overlayBlob);
    assert.equal(refusal(() => main(['verify-normal', dir])), 'OVERRIDE_PRESENT');
    cli('restore', dir, parameters);
    run(['commit', '-q', '-am', 'Restore reviewed G1 workflow']);
    assert.equal(run(['rev-parse', `HEAD:${WORKFLOW_PATH}`]), BASELINE_WORKFLOW_BLOB);
    assert.equal(run(['diff', pre, 'HEAD']), '', 'tree identical to pre-overlay commit');
    assert.equal(main(['verify-normal', dir]).workflowBlob, BASELINE_WORKFLOW_BLOB);
  } finally { rmSync(dir, { recursive: true, force: true }); }
});

test('CLI refuses dirty trees, foreign or non-commit SHAs and drifted HEAD', () => {
  const { dir, run, parameters } = fixtureRepo();
  try {
    writeFileSync(join(dir, 'stray.txt'), 'x');
    assert.equal(refusal(() => cli('apply', dir, parameters)), 'DIRTY_TREE');
    rmSync(join(dir, 'stray.txt'));
    assert.equal(refusal(() => cli('plan', dir, { ...parameters, observedSha: 'e'.repeat(40) })),
      'OBSERVED_NOT_IN_HISTORY');
    const tree = run(['rev-parse', 'HEAD^{tree}']);
    assert.equal(refusal(() => cli('plan', dir, { ...parameters, requestedSha: tree })),
      'REQUESTED_NOT_IN_HISTORY');
    run(['checkout', '-q', '-b', 'side']);
    run(['commit', '-q', '--allow-empty', '-m', 'not on main']);
    const side = run(['rev-parse', 'HEAD']);
    run(['checkout', '-q', 'main']);
    assert.equal(refusal(() => cli('plan', dir, { ...parameters, requestedSha: side })),
      'REQUESTED_NOT_IN_HISTORY');
    writeFileSync(join(dir, WORKFLOW_PATH), baseline.toString('utf8').replace('timeout-minutes: 10', 'timeout-minutes: 9'));
    run(['commit', '-q', '-am', 'drift']);
    assert.equal(refusal(() => cli('apply', dir, parameters)), 'BASELINE_DRIFT');
  } finally { rmSync(dir, { recursive: true, force: true }); }
});

test('unchanged run-profile refuses a genuinely different checkout HEAD with zero processes', () => {
  const { dir, run, parameters } = fixtureRepo();
  const work = mkdtempSync(join(tmpdir(), 'grl-g1-witness-ws-'));
  try {
    // What the overlaid execute job produces: grl-target really checked out at the observed commit.
    execFileSync('git', ['clone', '-q', '--no-checkout', dir, join(work, 'grl-target')]);
    git(join(work, 'grl-target'), ['checkout', '-q', '--detach', parameters.observedSha]);
    // A stale positive result left in the workspace must not be mistaken for fresh proof.
    mkdirSync(join(work, 'grl-outcome'));
    writeFileSync(join(work, 'grl-outcome', 'result.json'), '{"stale":true}');
    const output = join(work, 'github-output.txt');
    writeFileSync(output, '');
    const identity = { request_id: U, profile: { id: 'js-smoke' },
      target: { requested_sha: parameters.requestedSha } };
    const child = execFileSync(process.execPath,
      [resolve(template, '.github/actions/grl-run-profile/main.mjs')], {
        cwd: work, encoding: 'utf8',
        env: { SystemRoot: process.env.SystemRoot ?? '', PATH: process.env.PATH ?? '',
          GITHUB_WORKSPACE: work, GITHUB_OUTPUT: output,
          'INPUT_IDENTITY-B64': Buffer.from(JSON.stringify(identity)).toString('base64') }
      });
    assert.equal(child, '');
    const outputs = readFileSync(output, 'utf8');
    assert.match(outputs, /^execution_status=BLOCKED$/m);
    assert.match(outputs, /^result_b64=$/m);
    const outcome = JSON.parse(readFileSync(join(work, 'grl-outcome', 'outcome.json'), 'utf8'));
    assert.deepEqual(outcome, { status: 'BLOCKED', reason_code: 'CHECKOUT_SHA_MISMATCH',
      profile_executed: false, requested_sha: parameters.requestedSha,
      observed_checkout_sha: parameters.observedSha, canonical_result_present: false });
    assert.equal(readFileSync(join(work, 'grl-outcome', 'refusal.marker'), 'utf8'), 'CHECKOUT_SHA_MISMATCH\n');
    // No profile process ran: the fixture never wrote its JUnit file.
    assert.throws(() => readFileSync(join(work, 'grl-target', 'fixtures/js-smoke/results.xml')));
    // run-profile does not clear old files; the runbook's workspace isolation is required.
    assert.equal(readFileSync(join(work, 'grl-outcome', 'result.json'), 'utf8'), '{"stale":true}');
    assert.equal(run(['status', '--porcelain']), '');
  } finally {
    rmSync(dir, { recursive: true, force: true });
    rmSync(work, { recursive: true, force: true });
  }
});

test('CLI refuses an overlay commit that touches anything but the workflow', () => {
  const { dir, run, parameters } = fixtureRepo();
  try {
    cli('apply', dir, parameters);
    writeFileSync(join(dir, 'README.md'), 'changed\n');
    run(['commit', '-q', '-am', 'overlay plus unrelated change']);
    assert.equal(refusal(() => cli('verify-overlay', dir, parameters)), 'OVERLAY_COMMIT_SCOPE');
  } finally { rmSync(dir, { recursive: true, force: true }); }
});
