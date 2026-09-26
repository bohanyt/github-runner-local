// Acceptance-only G1 checkout-SHA-mismatch witness overlay (Issue #15 packet 5841236994).
// NOT part of the execution-repository template. It rewrites exactly two places of the
// reviewed grl-dispatch.yml in a private execution-repository working tree:
//   1. appends ONE extra admission fence for a predeclared one-run request nonce;
//   2. pins ONLY the execute job's grl-target checkout ref to a fixed observed commit.
// Everything else (guards, pins, permissions, trusted checkout, action code) is byte-identical.
import { createHash } from 'node:crypto';
import { execFileSync } from 'node:child_process';
import { readFileSync, writeFileSync } from 'node:fs';
import { resolve } from 'node:path';
import { fileURLToPath } from 'node:url';

// Git blob SHA-1 of the reviewed templates/execution-repo/.github/workflows/grl-dispatch.yml.
export const BASELINE_WORKFLOW_BLOB = '71e0271ee9c1146ebcdc2ac9d982b32c1a773b5d';
export const WORKFLOW_PATH = '.github/workflows/grl-dispatch.yml';

const SHA = /^[0-9a-f]{40}$/;
const UUID4 = /^[0-9a-f]{8}-[0-9a-f]{4}-4[0-9a-f]{3}-[89ab][0-9a-f]{3}-[0-9a-f]{12}$/;
const FENCE_ANCHOR = "      startsWith(github.event.comment.body, '<!-- grl-request v1 -->')\n";
const TARGET_ANCHOR = '          ref: ${{ needs.admit.outputs.target_sha }}\n          path: grl-target\n';

export class WitnessError extends Error {
  constructor(code) { super(code); this.code = code; }
}
const fail = code => { throw new WitnessError(code); };

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

const git = (repo, args) => execFileSync('git', args, { cwd: repo, encoding: 'utf8',
  stdio: ['ignore', 'pipe', 'pipe'] }).trim();

// Repository preconditions: clean tree, HEAD workflow is the reviewed baseline, and both
// requested and observed commits are real commits contained in HEAD's history.
export function checkRepository(repo, parameters, { expectOverlay = false } = {}) {
  validateParameters(parameters);
  if (git(repo, ['status', '--porcelain', '--untracked-files=all']) !== '') fail('DIRTY_TREE');
  const headBlob = git(repo, ['rev-parse', `HEAD:${WORKFLOW_PATH}`]);
  if (!expectOverlay && headBlob !== BASELINE_WORKFLOW_BLOB) fail('BASELINE_DRIFT');
  for (const [sha, code] of [[parameters.requestedSha, 'REQUESTED_NOT_IN_HISTORY'],
    [parameters.observedSha, 'OBSERVED_NOT_IN_HISTORY']]) {
    let type = '';
    try { type = git(repo, ['cat-file', '-t', sha]); } catch { fail(code); }
    if (type !== 'commit') fail(code);
    try { git(repo, ['merge-base', '--is-ancestor', sha, 'HEAD']); } catch { fail(code); }
  }
  return headBlob;
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

export function main(argv) {
  const { command, repo, parameters } = parseArgs(argv);
  if (!repo || !['plan', 'apply', 'restore', 'verify-overlay', 'verify-normal'].includes(command)) fail('USAGE');
  const file = resolve(repo, WORKFLOW_PATH);
  const baseline = () => Buffer.from(execFileSync('git',
    ['cat-file', 'blob', BASELINE_WORKFLOW_BLOB], { cwd: repo }));
  if (command === 'plan') {
    checkRepository(repo, parameters);
    const overlay = applyOverlay(baseline(), parameters);
    return { command, preOverlayCommit: git(repo, ['rev-parse', 'HEAD']),
      baselineBlob: BASELINE_WORKFLOW_BLOB, overlayBlob: gitBlobSha(overlay), ...parameters,
      expectedRefusal: { reason_code: 'CHECKOUT_SHA_MISMATCH', requested_sha: parameters.requestedSha,
        observed_checkout_sha: parameters.observedSha, profile_executed: false },
      delta: overlayDelta(baseline(), overlay) };
  }
  if (command === 'apply') {
    checkRepository(repo, parameters);
    const current = readFileSync(file);
    if (gitBlobSha(current) !== BASELINE_WORKFLOW_BLOB) fail('WORKTREE_BYTES_DRIFT');
    const overlay = applyOverlay(current, parameters);
    writeFileSync(file, overlay);
    return { command, baselineBlob: BASELINE_WORKFLOW_BLOB, overlayBlob: gitBlobSha(overlay),
      delta: overlayDelta(current, overlay) };
  }
  if (command === 'verify-overlay') {
    checkRepository(repo, parameters, { expectOverlay: true });
    const expected = applyOverlay(baseline(), parameters);
    const committed = Buffer.from(execFileSync('git', ['cat-file', 'blob', `HEAD:${WORKFLOW_PATH}`], { cwd: repo }));
    if (!committed.equals(expected)) fail('NOT_EXPECTED_OVERLAY');
    const changed = git(repo, ['diff', '--name-only', 'HEAD~1', 'HEAD']).split('\n').filter(Boolean);
    if (changed.length !== 1 || changed[0] !== WORKFLOW_PATH) fail('OVERLAY_COMMIT_SCOPE');
    if (git(repo, ['rev-parse', `HEAD~1:${WORKFLOW_PATH}`]) !== BASELINE_WORKFLOW_BLOB) fail('BASELINE_DRIFT');
    return { command, overlayBlob: gitBlobSha(expected) };
  }
  if (command === 'restore') {
    if (git(repo, ['status', '--porcelain', '--untracked-files=all']) !== '') fail('DIRTY_TREE');
    const restored = restoreOverlay(readFileSync(file), parameters, baseline());
    writeFileSync(file, restored);
    return { command, restoredBlob: gitBlobSha(restored) };
  }
  // verify-normal: committed HEAD workflow is exactly the reviewed baseline and no fence/pin remains.
  const committed = Buffer.from(execFileSync('git', ['cat-file', 'blob', `HEAD:${WORKFLOW_PATH}`], { cwd: repo }));
  const text = committed.toString('utf8');
  if (gitBlobSha(committed) !== BASELINE_WORKFLOW_BLOB || /request_id/.test(text) ||
      !text.includes(TARGET_ANCHOR)) fail('OVERRIDE_PRESENT');
  return { command, workflowBlob: BASELINE_WORKFLOW_BLOB };
}

if (process.argv[1] && resolve(process.argv[1]) === fileURLToPath(import.meta.url)) {
  try {
    process.stdout.write(JSON.stringify(main(process.argv.slice(2)), null, 2) + '\n');
  } catch (error) {
    process.stderr.write('G1 witness overlay refused: ' +
      (error instanceof WitnessError ? error.code : 'INTERNAL_ERROR') + '\n');
    process.exitCode = 1;
  }
}
