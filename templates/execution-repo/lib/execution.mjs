import { spawn, execFileSync } from 'node:child_process';
import { readFileSync, lstatSync, realpathSync } from 'node:fs';
import { resolve, sep } from 'node:path';
import { ProtocolError, SHA, gitBlobSha, redact, reject, validateProfile } from './protocol.mjs';

const MAX_OUTPUT_BYTES = 64 * 1024;
const MAX_TEST_FILE_BYTES = 1024 * 1024;
const MAX_RESULT_BYTES = 32 * 1024;

function xmlCounter(attrs, name, { required = false, fallback = null } = {}) {
  const mentioned = new RegExp(`\\b${name}\\s*=`).test(attrs);
  const match = new RegExp(`\\b${name}="(\\d+)"`).exec(attrs);
  if (!match) {
    if (required || mentioned) reject('INVALID_TEST_COUNTER');
    return fallback;
  }
  const value = Number(match[1]);
  if (!Number.isSafeInteger(value) || value < 0) reject('INVALID_TEST_COUNTER');
  return value;
}

function junitCounts(attrs) {
  const total = xmlCounter(attrs, 'tests', { required: true });
  const failed = xmlCounter(attrs, 'failures', { fallback: 0 });
  const errored = xmlCounter(attrs, 'errors', { fallback: 0 });
  const skipped = xmlCounter(attrs, 'skipped', { fallback: 0 });
  if (failed + errored + skipped > total) reject('INVALID_JUNIT');
  return { passed: total - failed - errored - skipped, failed, skipped, errored };
}

export function parseJUnit(xml) {
  const suites = [...xml.matchAll(/<testsuite\b([^>]*)>/g)].map(match => junitCounts(match[1]));
  if (!suites.length) reject('INVALID_JUNIT');
  const aggregate = suites.reduce((sum, value) => ({
    passed: sum.passed + value.passed,
    failed: sum.failed + value.failed,
    skipped: sum.skipped + value.skipped,
    errored: sum.errored + value.errored
  }), { passed: 0, failed: 0, skipped: 0, errored: 0 });

  const root = /<testsuites\b([^>]*)>/.exec(xml);
  if (root && /\b(?:tests|failures|errors|skipped)\s*=/.test(root[1])) {
    const declared = junitCounts(root[1]);
    if (declared.passed !== aggregate.passed ||
        declared.failed !== aggregate.failed ||
        declared.skipped !== aggregate.skipped ||
        declared.errored !== aggregate.errored) reject('INVALID_JUNIT');
  }
  return { ...aggregate, source: 'junit' };
}

export function parseTrx(xml) {
  const counters = /<Counters\b([^>]*)\/?\s*>/.exec(xml);
  if (!counters) reject('INVALID_TRX');
  const allowed = new Set([
    'total', 'executed', 'passed', 'failed', 'error', 'timeout', 'aborted',
    'inconclusive', 'passedButRunAborted', 'notRunnable', 'notExecuted',
    'disconnected', 'warning', 'completed', 'inProgress', 'pending'
  ]);
  const values = {};
  const seen = new Set();
  for (const match of counters[1].matchAll(/\b([A-Za-z][A-Za-z0-9.-]*)="([^"]*)"/g)) {
    const [, name, raw] = match;
    if (!allowed.has(name) || seen.has(name) || !/^\d+$/.test(raw)) reject('INVALID_TRX');
    const value = Number(raw);
    if (!Number.isSafeInteger(value) || value < 0) reject('INVALID_TRX');
    seen.add(name);
    values[name] = value;
  }
  for (const name of counters[1].matchAll(/\b([A-Za-z][A-Za-z0-9.-]*)\s*=/g))
    if (!seen.has(name[1])) reject('INVALID_TRX');

  if (!Object.hasOwn(values, 'total')) reject('INVALID_TRX');
  const value = name => values[name] ?? 0;
  for (const name of ['timeout', 'aborted', 'inconclusive', 'passedButRunAborted',
    'notRunnable', 'disconnected', 'warning', 'inProgress', 'pending'])
    if (value(name) !== 0) reject('INVALID_TRX');

  const passed = value('passed');
  const failed = value('failed');
  const errored = value('error');
  const skipped = value('notExecuted');
  const executed = passed + failed + errored;
  if (executed + skipped !== value('total')) reject('INVALID_TRX');
  if (Object.hasOwn(values, 'executed') && values.executed !== executed) reject('INVALID_TRX');
  if (Object.hasOwn(values, 'completed') && values.completed !== executed) reject('INVALID_TRX');
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
  if (typeof runner?.name !== 'string' || !runner.name ||
      typeof runner.version !== 'string' || !/^\d+(?:\.\d+){1,3}$/.test(runner.version) ||
      !['portable-user', 'service-account'].includes(runner.identityClass))
    reject('RUNNER_METADATA_UNAVAILABLE');
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
      name: redact(runner.name), version: runner.version, os: 'Windows', arch: 'X64',
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
