import test from 'node:test';
import assert from 'node:assert/strict';
import { execFileSync } from 'node:child_process';
import { existsSync, mkdirSync, mkdtempSync, readFileSync, renameSync, rmSync, writeFileSync } from 'node:fs';
import { tmpdir } from 'node:os';
import { dirname, join, resolve } from 'node:path';
import { fileURLToPath } from 'node:url';
import { lintSource } from '../../templates/execution-repo/tools/lint.mjs';
import {
  BASELINE_WORKFLOW_BLOB, CANONICAL_REMOTE_URLS, WORKFLOW_PATH, applyOverlay, gitBlobSha, main,
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


// ---- Disposable private-repo shaped clone with a local bare "origin" ----
// allowedUrls is the in-process test seam; the CLI itself only accepts the canonical remote.
function fixture() {
  const base = mkdtempSync(join(tmpdir(), 'grl-g1-witness-'));
  const bare = join(base, 'remote.git').replaceAll('\\', '/');
  const dir = join(base, 'work');
  git(base, ['init', '-q', '--bare', '-b', 'main', bare]);
  mkdirSync(dir);
  const run = args => git(dir, args);
  run(['init', '-q', '-b', 'main']);
  run(['config', 'core.autocrlf', 'false']);
  run(['config', 'user.name', 'witness-test']);
  run(['config', 'user.email', 'witness-test@example.invalid']);
  writeFileSync(join(dir, 'README.md'), 'fixture\n');
  run(['add', '-A']); run(['commit', '-q', '-m', 'initial']);
  mkdirSync(join(dir, '.github/workflows'), { recursive: true });
  writeFileSync(join(dir, WORKFLOW_PATH), baseline);
  run(['add', '-A']); run(['commit', '-q', '-m', 'bootstrap X']);
  const observed = run(['rev-parse', 'HEAD']);
  run(['commit', '-q', '--allow-empty', '-m', 'witness requested anchor']);
  const requested = run(['rev-parse', 'HEAD']);
  run(['remote', 'add', 'origin', bare]);
  run(['push', '-q', 'origin', 'main']);
  const parameters = { requestedSha: requested, observedSha: observed, requestId: U };
  const opts = { allowedUrls: [bare] };
  const cli = (command, p = parameters) => main([command, dir, '--requested', p.requestedSha,
    '--observed', p.observedSha, '--request-id', p.requestId], opts);
  const remote = () => git(base, ['--git-dir', bare, 'rev-parse', 'refs/heads/main']);
  const remoteWorkflow = () => git(base, ['--git-dir', bare, 'rev-parse', `refs/heads/main:${WORKFLOW_PATH}`]);
  const P = run(['rev-parse', 'HEAD']);
  const overlay = applyOverlay(baseline, parameters);
  const overlayBlob = gitBlobSha(overlay);
  const commits = () => Number(run(['rev-list', '--count', 'HEAD']));
  const cleanup = () => rmSync(base, { recursive: true, force: true });
  return { base, bare, dir, run, parameters, opts, cli, remote, remoteWorkflow, P, overlay,
    overlayBlob, commits, cleanup };
}
const workflowBytes = fx => readFileSync(join(fx.dir, WORKFLOW_PATH));
const rejectPush = fx => writeFileSync(join(fx.bare, 'hooks', 'pre-receive'), '#!/bin/sh\nexit 1\n');
const allowPush = fx => rmSync(join(fx.bare, 'hooks', 'pre-receive'), { force: true });

test('canonical destination is exactly the private execution repository', () => {
  assert.deepEqual([...CANONICAL_REMOTE_URLS], [
    'https://github.com/bohanyt/github-runner-local-exec.git',
    'https://github.com/bohanyt/github-runner-local-exec']);
});

test('happy path: plan, apply, verify-overlay, restore and verify-normal against the actual remote', () => {
  const fx = fixture();
  try {
    const planned = fx.cli('plan');
    assert.equal(planned.plan.P, fx.P);
    assert.equal(planned.plan.overlayBlob, fx.overlayBlob);
    assert.equal(fx.run(['status', '--porcelain']), '', 'plan leaves the tree untouched');
    assert.equal(fx.cli('plan').reused, true, 'plan is idempotent');
    const applied = fx.cli('apply');
    assert.equal(applied.state, 'OVERLAY_ACTIVE_CONFIRMED');
    assert.equal(fx.remote(), applied.Y);
    assert.equal(fx.remoteWorkflow(), fx.overlayBlob);
    assert.deepEqual(fx.run(['diff', '--name-only', fx.P, applied.Y]).split('\n'), [WORKFLOW_PATH]);
    assert.equal(fx.cli('verify-overlay').state, 'OVERLAY_ACTIVE_CONFIRMED');
    assert.equal(fx.cli('apply').pushed, false, 'apply re-run is a no-op');
    assert.equal(refusal(() => fx.cli('verify-normal')), 'LOCAL_NOT_NORMAL');
    const restored = fx.cli('restore');
    assert.equal(restored.state, 'NORMAL_CONFIRMED');
    assert.equal(restored.remoteSha, fx.remote());
    assert.equal(fx.remoteWorkflow(), BASELINE_WORKFLOW_BLOB);
    assert.equal(fx.run(['diff', fx.P, 'HEAD']), '', 'tree identical to P');
    const count = fx.commits();
    assert.equal(fx.cli('restore').state, 'NORMAL_CONFIRMED');
    assert.equal(fx.commits(), count, 'restore re-run creates no commit');
    assert.equal(fx.cli('verify-normal').state, 'NORMAL_CONFIRMED');
    assert.equal(refusal(() => fx.cli('apply')), 'ALREADY_RESTORED');
    assert.ok(workflowBytes(fx).equals(baseline));
  } finally { fx.cleanup(); }
});

test('W-1 point A: interrupted apply (overlay written, unstaged or staged) resumes or cancels', () => {
  for (const staged of [false, true]) {
    // Resume forward with apply.
    let fx = fixture();
    try {
      fx.cli('plan');
      writeFileSync(join(fx.dir, WORKFLOW_PATH), fx.overlay);
      if (staged) fx.run(['add', '--', WORKFLOW_PATH]);
      assert.equal(refusal(() => fx.cli('verify-normal')), 'LOCAL_NOT_NORMAL', 'dirty overlay is not normal');
      const applied = fx.cli('apply');
      assert.equal(fx.remote(), applied.Y);
      assert.equal(fx.commits(), 4);
    } finally { fx.cleanup(); }
    // Or cancel locally: nothing was committed or pushed, so no restore commit is invented.
    fx = fixture();
    try {
      fx.cli('plan');
      writeFileSync(join(fx.dir, WORKFLOW_PATH), fx.overlay);
      if (staged) fx.run(['add', '--', WORKFLOW_PATH]);
      const cancelled = fx.cli('restore');
      assert.equal(cancelled.state, 'NORMAL_CONFIRMED');
      assert.equal(cancelled.reason, 'CANCELLED_BEFORE_COMMIT');
      assert.equal(fx.run(['rev-parse', 'HEAD']), fx.P);
      assert.equal(fx.remote(), fx.P);
      assert.equal(fx.run(['status', '--porcelain']), '');
      assert.equal(fx.cli('restore').reason, 'CANCELLED_BEFORE_COMMIT', 'repeatable');
    } finally { fx.cleanup(); }
  }
});

test('W-1 point B: interrupted restore (baseline written, unstaged or staged) converges to one restore commit', () => {
  for (const staged of [false, true]) {
    const fx = fixture();
    try {
      fx.cli('plan'); const { Y } = fx.cli('apply');
      writeFileSync(join(fx.dir, WORKFLOW_PATH), baseline);
      if (staged) fx.run(['add', '--', WORKFLOW_PATH]);
      assert.equal(refusal(() => fx.cli('verify-normal')), 'LOCAL_NOT_NORMAL', 'HEAD still overlay');
      assert.equal(refusal(() => fx.cli('apply')), 'RESTORE_IN_PROGRESS');
      const restored = fx.cli('restore');
      assert.equal(restored.state, 'NORMAL_CONFIRMED');
      assert.deepEqual(fx.run(['rev-list', '--parents', '-n', '1', 'HEAD']).split(' ').slice(1), [Y]);
      assert.equal(fx.commits(), 5);
      fx.cli('restore');
      assert.equal(fx.commits(), 5, 'no duplicate restore commit');
    } finally { fx.cleanup(); }
  }
});

test('overlay committed but not pushed: apply publishes it, or restore publishes Y+Z with no active override', () => {
  for (const next of ['apply', 'restore']) {
    const fx = fixture();
    try {
      fx.cli('plan');
      rejectPush(fx);
      assert.equal(refusal(() => fx.cli('apply')), 'APPLY_PUSH_PENDING');
      assert.equal(fx.remote(), fx.P, 'remote untouched');
      assert.equal(refusal(() => fx.cli('verify-overlay')), 'OVERLAY_NOT_ON_REMOTE');
      allowPush(fx);
      if (next === 'apply') assert.equal(fx.cli('apply').state, 'OVERLAY_ACTIVE_CONFIRMED');
      else {
        assert.equal(fx.cli('restore').state, 'NORMAL_CONFIRMED');
        assert.equal(fx.remoteWorkflow(), BASELINE_WORKFLOW_BLOB);
      }
    } finally { fx.cleanup(); }
  }
});

test('restore committed but not pushed / failed push: local baseline is never an overall normal', () => {
  const fx = fixture();
  try {
    fx.cli('plan'); const { Y } = fx.cli('apply');
    rejectPush(fx);
    assert.equal(refusal(() => fx.cli('restore')), 'RESTORE_PUSH_PENDING');
    assert.equal(fx.remote(), Y, 'remote still overlaid');
    assert.equal(fx.run(['rev-parse', `HEAD:${WORKFLOW_PATH}`]), BASELINE_WORKFLOW_BLOB);
    assert.equal(fx.run(['status', '--porcelain']), '', 'clean local baseline');
    assert.equal(refusal(() => fx.cli('verify-normal')), 'REMOTE_STILL_OVERLAID');
    const Z = fx.run(['rev-parse', 'HEAD']);
    allowPush(fx);
    assert.equal(fx.cli('restore').state, 'NORMAL_CONFIRMED');
    assert.equal(fx.remote(), Z, 'same restore commit published, none duplicated');
  } finally { fx.cleanup(); }
});

test('lost push acknowledgement: remote already restored is confirmed without a new push or commit', () => {
  const fx = fixture();
  try {
    fx.cli('plan'); fx.cli('apply');
    rejectPush(fx);
    assert.equal(refusal(() => fx.cli('restore')), 'RESTORE_PUSH_PENDING');
    allowPush(fx);
    fx.run(['push', '-q', 'origin', 'HEAD:main']); // the push that "succeeded" out of band
    const count = fx.commits();
    const restored = fx.cli('restore');
    assert.equal(restored.state, 'NORMAL_CONFIRMED');
    assert.equal(fx.commits(), count);
    // Same for apply: overlay push reached the remote but the tool never saw it.
    const fy = fixture();
    try {
      fy.cli('plan'); rejectPush(fy);
      assert.equal(refusal(() => fy.cli('apply')), 'APPLY_PUSH_PENDING');
      allowPush(fy); fy.run(['push', '-q', 'origin', 'HEAD:main']);
      const applied = fy.cli('apply');
      assert.equal(applied.pushed, false);
      assert.equal(applied.state, 'OVERLAY_ACTIVE_CONFIRMED');
    } finally { fy.cleanup(); }
  } finally { fx.cleanup(); }
});

test('unreachable remote is pending, never normal', () => {
  const fx = fixture();
  try {
    fx.cli('plan'); fx.cli('apply'); fx.cli('restore');
    renameSync(fx.bare, fx.bare + '.offline');
    assert.equal(refusal(() => fx.cli('verify-normal')), 'REMOTE_UNAVAILABLE');
    renameSync(fx.bare + '.offline', fx.bare);
    assert.equal(fx.cli('verify-normal').state, 'NORMAL_CONFIRMED');
  } finally { fx.cleanup(); }
});

test('wrong fetch/push destination, branch or remote default branch are refused before any mutation', () => {
  const fx = fixture();
  try {
    const other = join(fx.base, 'other.git').replaceAll('\\', '/');
    git(fx.base, ['init', '-q', '--bare', '-b', 'main', other]);
    const snapshot = () => [fx.run(['rev-parse', 'HEAD']), fx.run(['status', '--porcelain']), fx.remote()];
    const before = snapshot();
    assert.equal(refusal(() => main(['plan', fx.dir, '--requested', fx.parameters.requestedSha,
      '--observed', fx.parameters.observedSha, '--request-id', U])), 'WRONG_DESTINATION', 'CLI default is canonical only');
    fx.run(['remote', 'set-url', '--push', 'origin', other]);
    assert.equal(refusal(() => fx.cli('plan')), 'WRONG_DESTINATION');
    fx.run(['config', '--unset', 'remote.origin.pushurl']);
    fx.run(['checkout', '-q', '-b', 'side']);
    assert.equal(refusal(() => fx.cli('plan')), 'WRONG_BRANCH');
    fx.run(['checkout', '-q', 'main']);
    git(fx.base, ['--git-dir', fx.bare, 'branch', 'trunk', 'main']);
    git(fx.base, ['--git-dir', fx.bare, 'symbolic-ref', 'HEAD', 'refs/heads/trunk']);
    assert.equal(refusal(() => fx.cli('plan')), 'WRONG_DEFAULT_BRANCH');
    git(fx.base, ['--git-dir', fx.bare, 'symbolic-ref', 'HEAD', 'refs/heads/main']);
    fx.run(['config', 'core.autocrlf', 'true']);
    assert.equal(refusal(() => fx.cli('plan')), 'LINE_ENDING_CONVERSION');
    fx.run(['config', 'core.autocrlf', 'false']);
    assert.deepEqual(snapshot(), before);
    assert.equal(existsSync(join(fx.dir, '.git', 'grl-g1-witness-plan.json')), false);
  } finally { fx.cleanup(); }
});

test('unexpected remote advance is refused and never overwritten', () => {
  const fx = fixture();
  try {
    fx.cli('plan'); fx.cli('apply');
    // Someone else advances remote main from another clone.
    const other = join(fx.base, 'other');
    git(fx.base, ['clone', '-q', fx.bare, other]);
    git(other, ['-c', 'user.name=x', '-c', 'user.email=x@example.invalid', 'commit', '-q', '--allow-empty', '-m', 'foreign']);
    git(other, ['push', '-q', 'origin', 'main']);
    const foreign = fx.remote();
    assert.equal(refusal(() => fx.cli('restore')), 'REMOTE_DRIFT');
    assert.equal(fx.remote(), foreign, 'remote not overwritten');
    assert.equal(refusal(() => fx.cli('verify-normal')), 'REMOTE_NOT_RESTORED');
    // Before apply as well: plan requires local HEAD == actual remote main.
    const fy = fixture();
    try {
      const o2 = join(fy.base, 'other');
      git(fy.base, ['clone', '-q', fy.bare, o2]);
      git(o2, ['-c', 'user.name=x', '-c', 'user.email=x@example.invalid', 'commit', '-q', '--allow-empty', '-m', 'foreign']);
      git(o2, ['push', '-q', 'origin', 'main']);
      assert.equal(refusal(() => fy.cli('plan')), 'LOCAL_NOT_REMOTE_MAIN');
    } finally { fy.cleanup(); }
  } finally { fx.cleanup(); }
});

test('unrelated changes and unknown workflow bytes are preserved and refused', () => {
  const fx = fixture();
  try {
    fx.cli('plan'); fx.cli('apply');
    writeFileSync(join(fx.dir, WORKFLOW_PATH), baseline);
    writeFileSync(join(fx.dir, 'README.md'), 'operator edit\n');
    writeFileSync(join(fx.dir, 'notes.txt'), 'untracked\n');
    const head = fx.run(['rev-parse', 'HEAD']);
    for (const command of ['restore', 'apply', 'verify-normal', 'verify-overlay'])
      assert.equal(refusal(() => fx.cli(command)), 'UNRELATED_CHANGES', command);
    assert.equal(fx.run(['rev-parse', 'HEAD']), head, 'no commit made');
    assert.equal(readFileSync(join(fx.dir, 'README.md'), 'utf8'), 'operator edit\n');
    assert.equal(readFileSync(join(fx.dir, 'notes.txt'), 'utf8'), 'untracked\n');
    assert.ok(workflowBytes(fx).equals(baseline));
    rmSync(join(fx.dir, 'notes.txt')); fx.run(['checkout', '-q', '--', 'README.md']);
    const partial = baseline.subarray(0, 100);
    writeFileSync(join(fx.dir, WORKFLOW_PATH), partial);
    for (const command of ['restore', 'apply', 'verify-normal'])
      assert.equal(refusal(() => fx.cli(command)), 'UNKNOWN_WORKFLOW_BYTES', command);
    assert.ok(workflowBytes(fx).equals(partial), 'unknown bytes preserved');
    assert.equal(fx.remote(), head, 'override stays visible as pending, not reported normal');
    writeFileSync(join(fx.dir, WORKFLOW_PATH), baseline);
    assert.equal(fx.cli('restore').state, 'NORMAL_CONFIRMED');
  } finally { fx.cleanup(); }
});

test('missing, corrupt or mismatched checkpoints are refused without mutation', () => {
  const fx = fixture();
  try {
    assert.equal(refusal(() => fx.cli('apply')), 'PLAN_MISSING');
    assert.equal(refusal(() => fx.cli('restore')), 'PLAN_MISSING');
    fx.cli('plan');
    const planFile = join(fx.dir, '.git', 'grl-g1-witness-plan.json');
    const saved = readFileSync(planFile);
    assert.equal(refusal(() => fx.cli('apply', { ...fx.parameters, requestId: 'a1b2c3d4-0000-4000-8000-000000000000' })),
      'PLAN_MISMATCH');
    writeFileSync(planFile, '{not json');
    assert.equal(refusal(() => fx.cli('apply')), 'PLAN_CORRUPT');
    const tampered = JSON.parse(saved); tampered.treeP = "0".repeat(40);
    writeFileSync(planFile, JSON.stringify(tampered));
    assert.equal(refusal(() => fx.cli('apply')), 'PLAN_CORRUPT');
    assert.equal(fx.run(['rev-parse', 'HEAD']), fx.P);
    assert.equal(fx.run(['status', '--porcelain']), '');
    assert.equal(fx.remote(), fx.P);
    writeFileSync(planFile, saved);
    assert.equal(fx.cli('apply').state, 'OVERLAY_ACTIVE_CONFIRMED');
  } finally { fx.cleanup(); }
});

test('plan preconditions: dirty tree, foreign or non-commit SHAs, drifted baseline', () => {
  const fx = fixture();
  try {
    writeFileSync(join(fx.dir, 'stray.txt'), 'x');
    assert.equal(refusal(() => fx.cli('plan')), 'DIRTY_TREE');
    rmSync(join(fx.dir, 'stray.txt'));
    assert.equal(refusal(() => fx.cli('plan', { ...fx.parameters, observedSha: 'e'.repeat(40) })),
      'OBSERVED_NOT_IN_HISTORY');
    const tree = fx.run(['rev-parse', 'HEAD^{tree}']);
    assert.equal(refusal(() => fx.cli('plan', { ...fx.parameters, requestedSha: tree })), 'REQUESTED_NOT_IN_HISTORY');
    fx.run(['checkout', '-q', '-b', 'side']);
    fx.run(['commit', '-q', '--allow-empty', '-m', 'not on main']);
    const side = fx.run(['rev-parse', 'HEAD']);
    fx.run(['checkout', '-q', 'main']);
    assert.equal(refusal(() => fx.cli('plan', { ...fx.parameters, requestedSha: side })), 'REQUESTED_NOT_IN_HISTORY');
    writeFileSync(join(fx.dir, WORKFLOW_PATH), baseline.toString('utf8').replace('timeout-minutes: 10', 'timeout-minutes: 9'));
    fx.run(['commit', '-q', '-am', 'drift']); fx.run(['push', '-q', 'origin', 'main']);
    assert.equal(refusal(() => fx.cli('plan')), 'BASELINE_DRIFT');
  } finally { fx.cleanup(); }
});

test('a HEAD that is not the recognized overlay/restore lineage is refused', () => {
  const fx = fixture();
  try {
    fx.cli('plan');
    writeFileSync(join(fx.dir, WORKFLOW_PATH), fx.overlay);
    writeFileSync(join(fx.dir, 'README.md'), 'changed\n');
    fx.run(['commit', '-q', '-am', 'overlay plus unrelated change']);
    const head = fx.run(['rev-parse', 'HEAD']);
    for (const command of ['apply', 'restore', 'verify-overlay', 'verify-normal'])
      assert.equal(refusal(() => fx.cli(command)), 'UNEXPECTED_HEAD', command);
    assert.equal(fx.run(['rev-parse', 'HEAD']), head);
    assert.equal(fx.remote(), fx.P);
  } finally { fx.cleanup(); }
});

test('status reports state without mutating anything', () => {
  const fx = fixture();
  try {
    fx.cli('plan'); fx.cli('apply');
    writeFileSync(join(fx.dir, WORKFLOW_PATH), baseline);
    const report = fx.cli('status');
    assert.equal(report.destination, 'OK');
    assert.equal(report.local.head, 'Y');
    assert.equal(report.local.worktree, 'baseline');
    assert.equal(report.remote.kind, 'Y');
    assert.ok(workflowBytes(fx).equals(baseline));
    assert.equal(fx.run(['diff', '--cached', '--name-only']), '');
  } finally { fx.cleanup(); }
});

test('unchanged run-profile refuses a genuinely different checkout HEAD with zero processes', () => {
  const fx = fixture(); const { dir, run, parameters } = fx;
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
    fx.cleanup();
    rmSync(work, { recursive: true, force: true });
  }
});
