// Acceptance-only G1 checkout-SHA-mismatch witness overlay (Issue #15 packets 5841236994, 5841514277).
// NOT part of the execution-repository template. It rewrites exactly two places of the
// reviewed grl-dispatch.yml in a private execution-repository clone:
//   1. appends ONE extra admission fence for a predeclared one-run request nonce;
//   2. pins ONLY the execute job's grl-target checkout ref to a fixed observed commit.
// Everything else (guards, pins, permissions, trusted checkout, action code) is byte-identical.
// apply/restore are checkpointed and idempotent: re-running converges from any recognized
// interrupted state; unknown bytes, unrelated changes or remote drift are refused untouched.
import { createHash } from 'node:crypto';
import { spawnSync } from 'node:child_process';
import { existsSync, readFileSync, renameSync, writeFileSync } from 'node:fs';
import { isAbsolute, resolve } from 'node:path';
import { fileURLToPath } from 'node:url';

// Git blob SHA-1 of the reviewed templates/execution-repo/.github/workflows/grl-dispatch.yml.
export const BASELINE_WORKFLOW_BLOB = '71e0271ee9c1146ebcdc2ac9d982b32c1a773b5d';
export const WORKFLOW_PATH = '.github/workflows/grl-dispatch.yml';

const SHA = /^[0-9a-f]{40}$/;
const UUID4 = /^[0-9a-f]{8}-[0-9a-f]{4}-4[0-9a-f]{3}-[89ab][0-9a-f]{3}-[0-9a-f]{12}$/;
const FENCE_ANCHOR = "      startsWith(github.event.comment.body, '<!-- grl-request v1 -->')\n";
const TARGET_ANCHOR = '          ref: ${{ needs.admit.outputs.target_sha }}\n          path: grl-target\n';

export class WitnessError extends Error {
  constructor(code, detail) { super(detail ? `${code}: ${detail}` : code); this.code = code; }
}
const fail = (code, detail) => { throw new WitnessError(code, detail); };

export const gitBlobSha = bytes =>
  createHash('sha1').update(`blob ${bytes.length}\0`).update(bytes).digest('hex');

export function validateParameters({ requestedSha, observedSha, requestId }) {
  if (typeof requestedSha !== 'string' || !SHA.test(requestedSha)) fail('INVALID_REQUESTED_SHA');
  if (typeof observedSha !== 'string' || !SHA.test(observedSha)) fail('INVALID_OBSERVED_SHA');
  if (requestedSha === observedSha) fail('EQUAL_SHAS');
  if (typeof requestId !== 'string' || !UUID4.test(requestId)) fail('INVALID_REQUEST_ID');
}

const fenceLine = requestId =>
  `      contains(github.event.comment.body, '"request_id":"${requestId}"')\n`;
const once = (text, needle, code) => {
  const first = text.indexOf(needle);
  if (first < 0 || text.indexOf(needle, first + 1) >= 0) fail(code);
  return first;
};

export function applyOverlay(baselineBytes, parameters) {
  validateParameters(parameters);
  const bytes = Buffer.from(baselineBytes);
  if (gitBlobSha(bytes) !== BASELINE_WORKFLOW_BLOB) fail('BASELINE_DRIFT');
  let text = bytes.toString('utf8');
  once(text, FENCE_ANCHOR, 'FENCE_ANCHOR');
  once(text, TARGET_ANCHOR, 'TARGET_ANCHOR');
  text = text.replace(FENCE_ANCHOR,
    FENCE_ANCHOR.replace(/\n$/, ' &&\n') + fenceLine(parameters.requestId));
  text = text.replace(TARGET_ANCHOR,
    `          ref: ${parameters.observedSha}\n          path: grl-target\n`);
  return Buffer.from(text, 'utf8');
}

// Exact inverse; refuses anything that is not the overlay generated from these parameters.
export function restoreOverlay(overlayBytes, parameters, baselineBytes) {
  if (gitBlobSha(Buffer.from(baselineBytes)) !== BASELINE_WORKFLOW_BLOB) fail('BASELINE_DRIFT');
  const expected = applyOverlay(baselineBytes, parameters);
  if (!Buffer.from(overlayBytes).equals(expected)) fail('NOT_EXPECTED_OVERLAY');
  return Buffer.from(baselineBytes);
}

// Minimal LCS line diff for review output: { removed: [[line, text]], added: [[line, text]] }.
export function overlayDelta(beforeBytes, afterBytes) {
  const a = Buffer.from(beforeBytes).toString('utf8').split('\n');
  const b = Buffer.from(afterBytes).toString('utf8').split('\n');
  const lcs = Array.from({ length: a.length + 1 }, () => new Int32Array(b.length + 1));
  for (let i = a.length - 1; i >= 0; i--)
    for (let j = b.length - 1; j >= 0; j--)
      lcs[i][j] = a[i] === b[j] ? lcs[i + 1][j + 1] + 1 : Math.max(lcs[i + 1][j], lcs[i][j + 1]);
  const removed = []; const added = [];
  let i = 0; let j = 0;
  while (i < a.length || j < b.length) {
    if (i < a.length && j < b.length && a[i] === b[j]) { i++; j++; }
    else if (j < b.length && (i === a.length || lcs[i][j + 1] >= lcs[i + 1][j])) { added.push([j + 1, b[j]]); j++; }
    else { removed.push([i + 1, a[i]]); i++; }
  }
  return { removed, added };
}

// ---- Private-repository destination and checkpointed Git state ----

export const EXPECTED_REPOSITORY = 'bohanyt/github-runner-local-exec';
export const EXPECTED_BRANCH = 'main';
export const REMOTE = 'origin';
export const CANONICAL_REMOTE_URLS = Object.freeze([
  `https://github.com/${EXPECTED_REPOSITORY}.git`, `https://github.com/${EXPECTED_REPOSITORY}`]);
const PLAN_FILE = 'grl-g1-witness-plan.json';
const PLAN_SCHEMA = 'grl.g1-witness-plan.v1';
const BRANCH_REF = `refs/heads/${EXPECTED_BRANCH}`;

function gitRaw(repo, args, { buffer = false } = {}) {
  return spawnSync('git', args, { cwd: repo, encoding: buffer ? 'buffer' : 'utf8',
    stdio: ['ignore', 'pipe', 'pipe'], timeout: 120_000, windowsHide: true });
}
function git(repo, args, code = 'GIT_FAILED') {
  const run = gitRaw(repo, args);
  if (run.error || run.status !== 0) fail(code, args.join(' '));
  return run.stdout.trim();
}
const gitOk = (repo, args) => { const run = gitRaw(repo, args); return !run.error && run.status === 0; };
const blobBytes = (repo, spec) => {
  const run = gitRaw(repo, ['cat-file', 'blob', spec], { buffer: true });
  if (run.error || run.status !== 0) fail('GIT_FAILED', `cat-file ${spec}`);
  return run.stdout;
};
const gitPath = (repo, name) => {
  const path = git(repo, ['rev-parse', '--git-path', name]);
  return isAbsolute(path) ? path : resolve(repo, path);
};

// Replace-safe write: bytes land in the Git directory first, then one rename replaces the file.
function atomicWrite(repo, target, bytes) {
  const temp = gitPath(repo, 'grl-g1-witness.tmp');
  writeFileSync(temp, bytes);
  renameSync(temp, target);
}

// Fetch and push URLs (insteadOf/pushInsteadOf expanded by Git), current branch,
// line-ending conversion and the remote's default branch. Network failure is pending.
export function checkDestination(repo, allowedUrls = CANONICAL_REMOTE_URLS) {
  if (git(repo, ['symbolic-ref', '-q', 'HEAD'], 'WRONG_BRANCH') !== BRANCH_REF) fail('WRONG_BRANCH');
  const fetchUrls = git(repo, ['remote', 'get-url', '--all', REMOTE], 'WRONG_DESTINATION').split(/\r?\n/);
  const pushUrls = git(repo, ['remote', 'get-url', '--push', '--all', REMOTE], 'WRONG_DESTINATION').split(/\r?\n/);
  for (const url of [...fetchUrls, ...pushUrls])
    if (!allowedUrls.includes(url)) fail('WRONG_DESTINATION', url);
  const autocrlf = gitRaw(repo, ['config', '--get', 'core.autocrlf']).stdout.trim().toLowerCase();
  if (autocrlf === 'true' || autocrlf === 'input') fail('LINE_ENDING_CONVERSION', 'set core.autocrlf false in this clone');
  const symref = gitRaw(repo, ['ls-remote', '--symref', REMOTE, 'HEAD']);
  if (symref.error || symref.status !== 0) fail('REMOTE_UNAVAILABLE', 'ls-remote HEAD');
  if (!new RegExp(`^ref: ${BRANCH_REF}\\tHEAD$`, 'm').test(symref.stdout)) fail('WRONG_DEFAULT_BRANCH');
  return { fetchUrls, pushUrls };
}

// Fresh read of the actual remote branch (never a tracking ref).
export function remoteMain(repo) {
  const run = gitRaw(repo, ['ls-remote', '--exit-code', REMOTE, BRANCH_REF]);
  if (run.error || run.status !== 0) fail('REMOTE_UNAVAILABLE', `ls-remote ${BRANCH_REF}`);
  const match = new RegExp(`^([0-9a-f]{40})\\t${BRANCH_REF}$`, 'm').exec(run.stdout);
  if (!match) fail('REMOTE_UNAVAILABLE', 'unparseable ls-remote');
  return match[1];
}

// Porcelain status split into the owned workflow and everything else.
function dirtyPaths(repo) {
  const out = gitRaw(repo, ['status', '--porcelain=v1', '-z', '--untracked-files=all']);
  if (out.error || out.status !== 0) fail('GIT_FAILED', 'status');
  const records = out.stdout.split('\0').filter(Boolean);
  const entries = [];
  for (let i = 0; i < records.length; i++) {
    const status = records[i].slice(0, 2);
    entries.push({ status, path: records[i].slice(3) });
    if (/[RC]/.test(status[0])) i++;
  }
  return entries;
}

function planPath(repo) { return gitPath(repo, PLAN_FILE); }

function readPlan(repo, parameters) {
  const file = planPath(repo);
  if (!existsSync(file)) fail('PLAN_MISSING', 'run plan first');
  let plan;
  try { plan = JSON.parse(readFileSync(file, 'utf8')); } catch { fail('PLAN_CORRUPT'); }
  const baseline = blobBytes(repo, BASELINE_WORKFLOW_BLOB);
  if (plan?.schema !== PLAN_SCHEMA || plan.repository !== EXPECTED_REPOSITORY ||
      plan.branch !== EXPECTED_BRANCH || plan.remote !== REMOTE ||
      plan.baselineBlob !== BASELINE_WORKFLOW_BLOB || !SHA.test(plan.P ?? '') || !SHA.test(plan.treeP ?? ''))
    fail('PLAN_CORRUPT');
  if (plan.requestedSha !== parameters.requestedSha || plan.observedSha !== parameters.observedSha ||
      plan.requestId !== parameters.requestId) fail('PLAN_MISMATCH', 'parameters differ from the checkpoint');
  if (plan.overlayBlob !== gitBlobSha(applyOverlay(baseline, parameters))) fail('PLAN_CORRUPT');
  if (git(repo, ['rev-parse', `${plan.P}^{tree}`], 'PLAN_CORRUPT') !== plan.treeP ||
      git(repo, ['rev-parse', `${plan.P}:${WORKFLOW_PATH}`], 'PLAN_CORRUPT') !== BASELINE_WORKFLOW_BLOB)
    fail('PLAN_CORRUPT');
  return plan;
}
function writePlan(repo, plan) {
  const temp = gitPath(repo, `${PLAN_FILE}.tmp`);
  writeFileSync(temp, JSON.stringify(plan, null, 2) + '\n');
  renameSync(temp, planPath(repo));
}

const parentsOf = (repo, commit) => git(repo, ['rev-list', '--parents', '-n', '1', commit]).split(' ').slice(1);
const treeOf = (repo, commit) => git(repo, ['rev-parse', `${commit}^{tree}`]);
const changedFrom = (repo, a, b) => git(repo, ['diff', '--name-only', a, b]).split(/\r?\n/).filter(Boolean);

// Y = one-parent child of P changing only the workflow to the expected overlay.
function isOverlayCommit(repo, plan, commit) {
  const parents = parentsOf(repo, commit);
  const changed = changedFrom(repo, plan.P, commit);
  return parents.length === 1 && parents[0] === plan.P && changed.length === 1 &&
    changed[0] === WORKFLOW_PATH && git(repo, ['rev-parse', `${commit}:${WORKFLOW_PATH}`]) === plan.overlayBlob;
}
// Z = one-parent child of such a Y whose tree equals P's tree.
function isRestoreCommit(repo, plan, commit) {
  const parents = parentsOf(repo, commit);
  return parents.length === 1 && isOverlayCommit(repo, plan, parents[0]) && treeOf(repo, commit) === plan.treeP;
}
const knownCommit = (repo, sha) => gitOk(repo, ['cat-file', '-e', `${sha}^{commit}`]);
function classifyCommit(repo, plan, commit) {
  if (commit === plan.P) return 'P';
  if (!knownCommit(repo, commit)) return 'UNKNOWN';
  if (isOverlayCommit(repo, plan, commit)) return 'Y';
  if (isRestoreCommit(repo, plan, commit)) return 'Z';
  return 'UNKNOWN';
}

// Recognized states only: the owned workflow at baseline/overlay bytes in HEAD, index and
// worktree. Anything else is preserved and refused.
export function localState(repo, plan) {
  const entries = dirtyPaths(repo);
  const unrelated = entries.filter(x => x.path !== WORKFLOW_PATH).map(x => x.path);
  if (unrelated.length) fail('UNRELATED_CHANGES', unrelated.join(', '));
  if (entries.some(x => /U|AA|DD/.test(x.status))) fail('UNMERGED_WORKFLOW');
  const kind = blob => blob === BASELINE_WORKFLOW_BLOB ? 'baseline' : blob === plan.overlayBlob ? 'overlay' : 'unknown';
  const file = resolve(repo, WORKFLOW_PATH);
  const index = gitRaw(repo, ['rev-parse', `:${WORKFLOW_PATH}`]);
  const state = {
    head: classifyCommit(repo, plan, git(repo, ['rev-parse', 'HEAD'])),
    headSha: git(repo, ['rev-parse', 'HEAD']),
    headWorkflow: kind(gitRaw(repo, ['rev-parse', `HEAD:${WORKFLOW_PATH}`]).stdout.trim()),
    index: index.status === 0 ? kind(index.stdout.trim()) : 'unknown',
    worktree: existsSync(file) ? kind(gitBlobSha(readFileSync(file))) : 'unknown'
  };
  if (state.head === 'UNKNOWN') fail('UNEXPECTED_HEAD', state.headSha);
  if ([state.headWorkflow, state.index, state.worktree].includes('unknown'))
    fail('UNKNOWN_WORKFLOW_BYTES', 'workflow bytes preserved; resolve manually');
  return state;
}

// Bring index+worktree to `want` using only the two known byte sequences.
function converge(repo, plan, state, want, baseline) {
  const bytes = want === 'baseline' ? baseline : applyOverlay(baseline, plan);
  if (state.worktree !== want) atomicWrite(repo, resolve(repo, WORKFLOW_PATH), bytes);
  if (state.index !== want || state.worktree !== want) git(repo, ['add', '--', WORKFLOW_PATH]);
}
function commitOwned(repo, message) {
  git(repo, ['commit', '-q', '-m', message, '--', WORKFLOW_PATH]);
  return git(repo, ['rev-parse', 'HEAD']);
}

// Normal fast-forward publication with a fresh remote read before and after the push.
function publish(repo, target, acceptableBefore, pendingCode) {
  const before = remoteMain(repo);
  if (before === target) return { remoteSha: before, pushed: false };
  if (!acceptableBefore.includes(before)) fail('REMOTE_DRIFT', `remote main ${before}`);
  const push = gitRaw(repo, ['push', '--porcelain', REMOTE, `${target}:${BRANCH_REF}`]);
  let after;
  try { after = remoteMain(repo); } catch { fail('PUSH_UNCERTAIN', `push exit ${push.status}; rerun to re-read remote`); }
  if (after === target) return { remoteSha: after, pushed: true, pushExit: push.status };
  if (acceptableBefore.includes(after)) fail(pendingCode, `push exit ${push.status}; remote main ${after}`);
  fail('REMOTE_DRIFT', `remote main ${after}`);
}

export function plan(repo, parameters, allowedUrls) {
  validateParameters(parameters);
  checkDestination(repo, allowedUrls);
  const baseline = blobBytes(repo, BASELINE_WORKFLOW_BLOB);
  if (existsSync(planPath(repo))) return { command: 'plan', reused: true, plan: readPlan(repo, parameters) };
  const dirty = dirtyPaths(repo).map(x => x.path);
  if (dirty.length) fail('DIRTY_TREE', dirty.join(', '));
  const head = git(repo, ['rev-parse', 'HEAD']);
  if (git(repo, ['rev-parse', `HEAD:${WORKFLOW_PATH}`], 'BASELINE_DRIFT') !== BASELINE_WORKFLOW_BLOB) fail('BASELINE_DRIFT');
  for (const [sha, code] of [[parameters.requestedSha, 'REQUESTED_NOT_IN_HISTORY'],
    [parameters.observedSha, 'OBSERVED_NOT_IN_HISTORY']]) {
    if (!knownCommit(repo, sha) || git(repo, ['cat-file', '-t', sha], code) !== 'commit' ||
        !gitOk(repo, ['merge-base', '--is-ancestor', sha, 'HEAD'])) fail(code);
  }
  const remoteSha = remoteMain(repo);
  if (remoteSha !== head) fail('LOCAL_NOT_REMOTE_MAIN', `local ${head}, remote ${remoteSha}`);
  const overlay = applyOverlay(baseline, parameters);
  const record = { schema: PLAN_SCHEMA, repository: EXPECTED_REPOSITORY, branch: EXPECTED_BRANCH,
    remote: REMOTE, P: head, treeP: treeOf(repo, head), baselineBlob: BASELINE_WORKFLOW_BLOB,
    overlayBlob: gitBlobSha(overlay), ...parameters, createdAt: new Date().toISOString() };
  writePlan(repo, record);
  return { command: 'plan', reused: false, plan: record,
    expectedRefusal: { reason_code: 'CHECKOUT_SHA_MISMATCH', requested_sha: parameters.requestedSha,
      observed_checkout_sha: parameters.observedSha, profile_executed: false },
    delta: overlayDelta(baseline, overlay) };
}

export function apply(repo, parameters, allowedUrls) {
  checkDestination(repo, allowedUrls);
  const record = readPlan(repo, parameters);
  const baseline = blobBytes(repo, BASELINE_WORKFLOW_BLOB);
  let state = localState(repo, record);
  if (state.head === 'Z') fail('ALREADY_RESTORED', 'overlay was already restored; do not reapply');
  if (state.head === 'Y' && (state.index !== 'overlay' || state.worktree !== 'overlay'))
    fail('RESTORE_IN_PROGRESS', 'run restore');
  if (state.head === 'P') {
    const remote = remoteMain(repo);
    if (remote !== record.P) fail('REMOTE_DRIFT', `remote main ${remote}`);
    converge(repo, record, state, 'overlay', baseline);
    commitOwned(repo, `TEMPORARY G1 witness overlay (FAULT_INJECTED, acceptance-only)\n\n` +
      `requested=${record.requestedSha} observed=${record.observedSha} nonce=${record.requestId}`);
    state = localState(repo, record);
    if (state.head !== 'Y') fail('UNEXPECTED_HEAD', 'overlay commit did not match the checkpoint');
  }
  const Y = state.headSha;
  const published = publish(repo, Y, [record.P], 'APPLY_PUSH_PENDING');
  writePlan(repo, { ...record, Y, overlayConfirmed: { remoteSha: published.remoteSha, at: new Date().toISOString() } });
  return { command: 'apply', state: 'OVERLAY_ACTIVE_CONFIRMED', Y, ...published };
}

export function restore(repo, parameters, allowedUrls) {
  checkDestination(repo, allowedUrls);
  const record = readPlan(repo, parameters);
  const baseline = blobBytes(repo, BASELINE_WORKFLOW_BLOB);
  let state = localState(repo, record);
  if (state.head === 'P') {
    // Overlay never committed here: local cancellation only if the remote never saw it either.
    const remote = remoteMain(repo);
    if (remote !== record.P) fail('LINEAGE_MISMATCH',
      `remote main ${remote} but local HEAD is P; fetch and fast-forward, then rerun restore`);
    converge(repo, record, state, 'baseline', baseline);
    return verifyNormal(repo, parameters, allowedUrls, 'CANCELLED_BEFORE_COMMIT');
  }
  if (state.head === 'Y') {
    converge(repo, record, state, 'baseline', baseline);
    commitOwned(repo, `Restore reviewed G1 dispatch workflow\n\nreverts TEMPORARY overlay ${state.headSha}`);
    state = localState(repo, record);
    if (state.head !== 'Z') fail('UNEXPECTED_HEAD', 'restore commit did not match the checkpoint');
  } else if (state.index !== 'baseline' || state.worktree !== 'baseline') {
    converge(repo, record, state, 'baseline', baseline);
  }
  const Z = state.headSha;
  const Y = parentsOf(repo, Z)[0];
  publish(repo, Z, [Y, record.P], 'RESTORE_PUSH_PENDING');
  writePlan(repo, { ...readPlan(repo, parameters), Y, Z });
  return verifyNormal(repo, parameters, allowedUrls, 'RESTORED');
}

export function verifyOverlay(repo, parameters, allowedUrls) {
  checkDestination(repo, allowedUrls);
  const record = readPlan(repo, parameters);
  const state = localState(repo, record);
  if (state.head !== 'Y' || state.index !== 'overlay' || state.worktree !== 'overlay')
    fail('OVERLAY_NOT_ACTIVE_LOCALLY');
  const remoteSha = remoteMain(repo);
  if (remoteSha !== state.headSha) fail('OVERLAY_NOT_ON_REMOTE', `remote main ${remoteSha}`);
  return { command: 'verify-overlay', state: 'OVERLAY_ACTIVE_CONFIRMED', Y: remoteSha, overlayBlob: record.overlayBlob };
}

// Overall normal = baseline in HEAD/index/worktree, nothing else dirty, HEAD tree == P's tree,
// and a FRESH read of the actual remote main equal to this restored HEAD.
export function verifyNormal(repo, parameters, allowedUrls, reason = 'VERIFIED') {
  checkDestination(repo, allowedUrls);
  const record = readPlan(repo, parameters);
  const state = localState(repo, record);
  if (state.headWorkflow !== 'baseline' || state.index !== 'baseline' || state.worktree !== 'baseline')
    fail('LOCAL_NOT_NORMAL', `head=${state.headWorkflow} index=${state.index} worktree=${state.worktree}`);
  if (treeOf(repo, state.headSha) !== record.treeP) fail('LOCAL_NOT_NORMAL', 'tree differs from P');
  const remoteSha = remoteMain(repo);
  if (remoteSha !== state.headSha) {
    const remoteKind = classifyCommit(repo, record, remoteSha);
    fail(remoteKind === 'Y' ? 'REMOTE_STILL_OVERLAID' : 'REMOTE_NOT_RESTORED', `remote main ${remoteSha}`);
  }
  const confirmed = { remoteSha, treeP: record.treeP, workflowBlob: BASELINE_WORKFLOW_BLOB, at: new Date().toISOString() };
  writePlan(repo, { ...record, normalConfirmed: confirmed });
  return { command: 'verify-normal', state: 'NORMAL_CONFIRMED', reason, ...confirmed };
}

// Read-only report; never mutates or throws on recognized refusals.
export function status(repo, parameters, allowedUrls) {
  const report = { command: 'status' };
  const attempt = (key, fn) => { try { report[key] = fn(); } catch (e) { report[key] = e.code ?? 'ERROR'; } };
  attempt('destination', () => { checkDestination(repo, allowedUrls); return 'OK'; });
  let record = null;
  attempt('plan', () => { record = readPlan(repo, parameters); return 'OK'; });
  if (record) {
    attempt('local', () => localState(repo, record));
    attempt('remote', () => { const sha = remoteMain(repo); return { sha, kind: classifyCommit(repo, record, sha) }; });
  }
  return report;
}

function parseArgs(argv) {
  const [command, repo, ...rest] = argv;
  const options = {};
  for (let i = 0; i < rest.length; i += 2) {
    const key = rest[i];
    if (!['--requested', '--observed', '--request-id'].includes(key) || rest[i + 1] === undefined)
      fail('USAGE');
    options[key] = rest[i + 1];
  }
  return { command, repo: repo && resolve(repo), parameters: {
    requestedSha: options['--requested'], observedSha: options['--observed'],
    requestId: options['--request-id'] } };
}

const COMMANDS = { plan, apply, restore, 'verify-overlay': verifyOverlay, 'verify-normal': verifyNormal, status };

// allowedUrls is an in-process test seam only; the CLI always uses the canonical private remote.
export function main(argv, { allowedUrls = CANONICAL_REMOTE_URLS } = {}) {
  const { command, repo, parameters } = parseArgs(argv);
  if (!repo || !Object.hasOwn(COMMANDS, command)) fail('USAGE');
  validateParameters(parameters);
  return COMMANDS[command](repo, parameters, allowedUrls);
}

if (process.argv[1] && resolve(process.argv[1]) === fileURLToPath(import.meta.url)) {
  try {
    process.stdout.write(JSON.stringify(main(process.argv.slice(2)), null, 2) + '\n');
  } catch (error) {
    process.stderr.write('G1 witness overlay refused: ' +
      (error instanceof WitnessError ? error.message : 'INTERNAL_ERROR') + '\n');
    process.exitCode = 1;
  }
}
