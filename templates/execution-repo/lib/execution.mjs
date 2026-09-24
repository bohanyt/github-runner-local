import { spawn, execFileSync } from 'node:child_process';
import { readFileSync, lstatSync, realpathSync } from 'node:fs';
import { resolve, sep } from 'node:path';
import { ProtocolError, SHA, gitBlobSha, redact, reject, validateProfile } from './protocol.mjs';

const MAX_OUTPUT_BYTES = 64 * 1024;
const MAX_TEST_FILE_BYTES = 1024 * 1024;
const MAX_RESULT_BYTES = 32 * 1024;

export function parseJUnit(xml) {
  const suite = /<testsuite\b([^>]*)>/.exec(xml);
  if (!suite) reject('INVALID_JUNIT');
  const attr = (name, fallback = null) => {
    const match = new RegExp(`\\b${name}="(\\d+)"`).exec(suite[1]);
    return match ? Number(match[1]) : fallback;
  };
  const total = attr('tests');
  const failed = attr('failures', 0);
  const errored = attr('errors', 0);
  const skipped = attr('skipped', 0);
  if (![total, failed, errored, skipped].every(Number.isSafeInteger) ||
      total < 0 || failed < 0 || errored < 0 || skipped < 0 ||
      failed + errored + skipped > total) reject('INVALID_JUNIT');
  return { passed: total - failed - errored - skipped, failed, skipped, errored, source: 'junit' };
}

export function parseTrx(xml) {
  const counters = /<Counters\b([^>]*)\/?\s*>/.exec(xml);
  if (!counters) reject('INVALID_TRX');
  const attr = name => {
    const match = new RegExp(`\\b${name}="(\\d+)"`).exec(counters[1]);
    return match ? Number(match[1]) : null;
  };
  const passed = attr('passed');
  const failed = attr('failed');
  const errored = attr('error');
  const skipped = attr('notExecuted') ?? 0;
  if (![passed, failed, errored, skipped].every(Number.isSafeInteger))
    reject('INVALID_TRX');
  return { passed, failed, skipped, errored, source: 'trx' };
}

export function readTestCounts(targetRoot, configuration) {
  const root = realpathSync(targetRoot);
  const candidate = resolve(root, configuration.path);
  if (!candidate.startsWith(root + sep)) reject('TEST_PATH_ESCAPE');
  if (lstatSync(candidate).isSymbolicLink()) reject('TEST_PATH_REPARSE');
  const real = realpathSync(candidate);
  if (!real.startsWith(root + sep)) reject('TEST_PATH_ESCAPE');
  const bytes = readFileSync(real);
  if (bytes.length > MAX_TEST_FILE_BYTES) reject('TEST_FILE_TOO_LARGE');
  const xml = bytes.toString('utf8');
  return configuration.kind === 'junit' ? parseJUnit(xml) : parseTrx(xml);
}

export async function defaultKillTree(child) {
  if (process.platform === 'win32') {
    try { execFileSync('taskkill.exe', ['/PID', String(child.pid), '/T', '/F'],
      { stdio: 'ignore', timeout: 5000 }); } catch { child.kill(); }
  } else {
    try { process.kill(-child.pid, 'SIGKILL'); } catch { child.kill('SIGKILL'); }
  }
}

export function spawnStep(executable, argv, { cwd, timeoutMs, killTree = defaultKillTree }) {
  return new Promise((resolvePromise, rejectPromise) => {
    const pinnedExecutable = executable === 'node' ? process.execPath : executable;
    const child = spawn(pinnedExecutable, argv, {
      cwd, shell: false, windowsHide: true, detached: process.platform !== 'win32',
      env: { ...process.env, GITHUB_TOKEN: '', GH_TOKEN: '' }
    });
    let stdout = '';
    let stderr = '';
    let truncated = false;
    let timedOut = false;
    const capture = (field, chunk) => {
      const current = field === 'stdout' ? stdout : stderr;
      const remaining = MAX_OUTPUT_BYTES - Buffer.byteLength(current);
      if (remaining <= 0) { truncated = true; return; }
      const text = Buffer.from(chunk).subarray(0, remaining).toString('utf8');
      if (Buffer.byteLength(chunk) > remaining) truncated = true;
      if (field === 'stdout') stdout += text;
      else stderr += text;
    };
    child.stdout.on('data', x => capture('stdout', x));
    child.stderr.on('data', x => capture('stderr', x));
    const timer = setTimeout(() => {
      timedOut = true;
      Promise.resolve(killTree(child)).catch(() => child.kill());
    }, timeoutMs);
    child.once('error', error => {
      clearTimeout(timer);
      rejectPromise(new ProtocolError(error.code === 'ENOENT' ? 'EXECUTABLE_MISSING' : 'PROCESS_START_FAILED'));
    });
    child.once('close', code => {
      clearTimeout(timer);
      resolvePromise({ exitCode: code ?? -1, stdout: redact(stdout), stderr: redact(stderr),
        truncated, timedOut });
    });
  });
}

export async function runProfile({
  identity, trustedProfileBytes, targetRoot, getHead, runStep, readCounts, system,
  runner, now = () => Date.now()
}) {
  const requested = identity.target.requested_sha;
  if (!SHA.test(requested)) reject('INVALID_SHA');
  const observed = (await getHead(targetRoot)).trim();
  if (!SHA.test(observed)) reject('INVALID_CHECKOUT_SHA');
  if (observed !== requested) {
    return {
      kind: 'refusal',
      outcome: {
        status: 'BLOCKED',
        reason_code: 'CHECKOUT_SHA_MISMATCH',
        profile_executed: false,
        requested_sha: requested,
        observed_checkout_sha: observed,
        canonical_result_present: false
      }
    };
  }
  const { profile, definitionSha } = validateProfile(trustedProfileBytes);
  if (profile.id !== identity.profile.id || definitionSha !== identity.profile.definition_sha ||
      gitBlobSha(trustedProfileBytes) !== identity.profile.definition_sha)
    reject('PROFILE_DEFINITION_MISMATCH');
  if (identity.timeout_minutes > profile.max_timeout_minutes) reject('TIMEOUT_OVER_MAX');
  if (await system.isElevated()) reject('ELEVATED_RUNNER');
  const started = now();
  const deadline = started + identity.timeout_minutes * 60_000;
  const checks = [];
  let timeout = false;
  let outputTruncated = false;
  for (const step of profile.steps) {
    const before = now();
    const remaining = deadline - before;
    if (remaining <= 0) { timeout = true; break; }
    const execution = await runStep(step.executable, step.argv, {
      cwd: targetRoot,
      timeoutMs: Math.min(step.timeout_seconds * 1000, remaining),
      shell: false
    });
    timeout ||= execution.timedOut;
    outputTruncated ||= execution.truncated;
    let counts = { passed: 0, failed: 0, skipped: 0, errored: 0, source: 'none' };
    if (step.test_result && !execution.timedOut) {
      try { counts = await readCounts(targetRoot, step.test_result); }
      catch { counts = { passed: 0, failed: 0, skipped: 0, errored: 1,
        source: 'unavailable' }; }
    }
    checks.push({
      name: step.name,
      exit_code: execution.exitCode,
      duration_s: Math.max(0, (now() - before) / 1000),
      tests: counts
    });
    if (timeout) break;
  }
  const finished = now();
  const passed = checks.reduce((sum, x) => sum + x.tests.passed, 0);
  const status = timeout ? 'TIMED_OUT' :
    !outputTruncated && checks.length === profile.steps.length &&
    checks.every(x => x.exit_code === 0 && x.tests.failed === 0 && x.tests.errored === 0) &&
    passed >= profile.min_tests ? 'PASS' : 'FAIL';
  const afterDisk = await system.freeDiskGiB();
  const result = {
    schema_version: 'grl.result.v1',
    request_id: identity.request_id,
    request_comment_id: identity.request_comment_id,
    request_body_sha256: identity.request_body_sha256,
    execution_repo: identity.execution_repo,
    run: identity.run,
    workflow_sha: identity.workflow_sha,
    profile: identity.profile,
    target: { ...identity.target, tested_sha: requested },
    runner: {
      name: runner.name, version: runner.version, os: 'Windows', arch: 'X64',
      identity_class: runner.identityClass, elevated: false
    },
    timing: {
      admitted_at: identity.admitted_at,
      started_at: new Date(started).toISOString(),
      finished_at: new Date(finished).toISOString(),
      phases: [{ name: 'execute', duration_s: Math.max(0, (finished - started) / 1000) }]
    },
    checks,
    execution_status: status,
    disk: {
      free_before_gib: identity.disk_before_gib,
      free_after_gib: afterDisk,
      reserve_gib: identity.reserve_gib
    },
    artifacts: [],
    logs: { url: identity.run.url },
    report: { attempts: 1, status_posted: false, comment_posted: false }
  };
  const json = JSON.stringify(result);
  if (Buffer.byteLength(json, 'utf8') > MAX_RESULT_BYTES) reject('RESULT_TOO_LARGE');
  return { kind: 'result', result, resultJson: json, profile_executed: true };
}

export async function gitHead(root) {
  return execFileSync('git', ['rev-parse', 'HEAD'], {
    cwd: root, encoding: 'utf8', timeout: 5000, stdio: ['ignore', 'pipe', 'ignore']
  });
}
