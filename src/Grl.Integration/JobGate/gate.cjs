'use strict';
const fs = require('node:fs');
const path = require('node:path');
const crypto = require('node:crypto');
const { setTimeout: sleep } = require('node:timers/promises');
const digest = value => crypto.createHash('sha256').update(value).digest('hex');
const json = file => JSON.parse(fs.readFileSync(file, 'utf8'));
const fail = code => { throw new Error(code); };

function ordinary(file, missing = false) {
  const absolute = path.resolve(file);
  for (let cursor = absolute; ; cursor = path.dirname(cursor)) {
    try { if (fs.lstatSync(cursor).isSymbolicLink()) fail('REPARSE_PATH'); }
    catch (e) { if (!(missing && e.code === 'ENOENT')) throw e; }
    if (cursor === path.dirname(cursor)) break;
  }
  return absolute;
}
function root(directory) {
  if (!path.isAbsolute(directory) || path.parse(directory).root === directory ||
      directory.startsWith('\\\\') || directory.includes('..')) fail('UNSAFE_ROOT');
  ordinary(directory);
  if (!fs.statSync(directory).isDirectory()) fail('UNSAFE_ROOT');
  return path.resolve(directory);
}
function fileAt(directory, name) {
  const file = path.resolve(directory, name);
  if (!file.startsWith(directory + path.sep)) fail('OUTSIDE_ROOT');
  return ordinary(file, true);
}
function durable(file, value, exclusive = false) {
  ordinary(file, true);
  const bytes = Buffer.isBuffer(value) ? value : Buffer.from(JSON.stringify(value) + '\n');
  if (exclusive) {
    const fd = fs.openSync(file, 'wx');
    try { fs.writeFileSync(fd, bytes); fs.fsyncSync(fd); } finally { fs.closeSync(fd); }
    return;
  }
  const temporary = file + '.' + crypto.randomUUID() + '.tmp';
  durable(temporary, bytes, true);
  // Same-volume replace. Interrupted temporary files are retained, never evidence of success.
  fs.renameSync(temporary, file);
}
async function locked(directory, action, timeout = 3000) {
  directory = root(directory);
  const lock = fileAt(directory, 'gate.lock');
  const deadline = Date.now() + timeout;
  while (true) {
    try { fs.mkdirSync(lock); break; }
    catch (e) {
      if (e.code !== 'EEXIST') throw e;
      if (Date.now() >= deadline) fail('LOCK_UNRESOLVED');
      await sleep(10);
    }
  }
  try { return await action(); }
  finally { fs.rmdirSync(lock); }
}
function validState(s) {
  if (s.v !== 1 || !/^[A-Za-z0-9_.-]+\/[A-Za-z0-9_.-]+$/.test(s.repo) ||
      !Number.isSafeInteger(s.runnerId) || s.runnerId <= 0 ||
      !/^[A-Za-z0-9_.-]+$/.test(s.runnerName) ||
      !/^[a-f0-9-]{36}$/.test(s.gateEpoch) || !Number.isSafeInteger(s.ownershipEpoch) || s.ownershipEpoch < 0 ||
      !['ACTIVE', 'DRAINING', 'INACTIVE'].includes(s.mode) ||
      !Array.isArray(s.allowedWorkflowShas) || s.allowedWorkflowShas.length === 0 ||
      !s.allowedWorkflowShas.every(x => /^[a-f0-9]{40}$/.test(x))) fail('INVALID_STATE');
  return s;
}
function requireEmergencyReconciled(directory) {
  if (fs.existsSync(fileAt(directory, 'emergency-recovery-required.json'))) fail('RECOVERY_REQUIRED');
}
function state(directory) { requireEmergencyReconciled(directory); return validState(json(fileAt(directory, 'state.json'))); }
function context(env) {
  const c = { repo: env.GITHUB_REPOSITORY, runnerName: env.RUNNER_NAME,
    runnerId: Number(env.GRL_GATE_RUNNER_ID), run: env.GITHUB_RUN_ID,
    attempt: env.GITHUB_RUN_ATTEMPT, job: env.GITHUB_JOB,
    sha: env.GITHUB_SHA, workflowSha: env.GITHUB_WORKFLOW_SHA,
    tracking: env.RUNNER_TRACKING_ID };
  if (!c.repo || !c.runnerName || !Number.isSafeInteger(c.runnerId) || c.runnerId <= 0 ||
      !/^[1-9][0-9]*$/.test(c.run) || !/^[1-9][0-9]*$/.test(c.attempt) ||
      !/^[A-Za-z0-9_-]+$/.test(c.job) || !/^github_[a-f0-9-]{36}$/.test(c.tracking)) fail('MISSING_CONTEXT');
  // RUNNER_TRACKING_ID is the pinned Worker's per-job GUID, inherited by child processes.
  // Do not use a reusable PID or a mutable workspace outcome as an invocation identity.
  c.invocation = digest(JSON.stringify(c));
  return c;
}
function recordFile(directory, c, suffix) { return fileAt(directory, 'jobs/' + c.invocation + '.' + suffix); }
function ledgerFile(directory, s, c) {
  return fileAt(directory, 'ledger/' + digest(JSON.stringify([s.repo, s.runnerId, s.runnerName,
    s.gateEpoch, c.run, c.attempt, c.workflowSha])) + '.json');
}
function decision(directory, s, c) {
  if (c.repo !== s.repo || c.runnerId !== s.runnerId || c.runnerName !== s.runnerName) return 'IDENTITY';
  if (!c.workflowSha || !s.allowedWorkflowShas.includes(c.workflowSha) || c.sha !== c.workflowSha) return 'WORKFLOW_SHA';
  if (s.mode === 'INACTIVE' || s.recoveryRequired) return 'INACTIVE';
  if (c.job === 'admit') return s.mode === 'ACTIVE' ? 'PASS' : 'DRAINING';
  if (!['execute', 'report', 'verdict'].includes(c.job)) return 'UNKNOWN_JOB';
  try {
    const entry = json(ledgerFile(directory, s, c));
    const intent = json(fileAt(directory, 'jobs/' + entry.invocation + '.intent'));
    const pass = json(fileAt(directory, 'jobs/' + entry.invocation + '.pass'));
    if (entry.gateEpoch !== s.gateEpoch || intent.gateEpoch !== s.gateEpoch ||
        intent.context.job !== 'admit' || pass.invocation !== entry.invocation ||
        JSON.stringify(entry.affinity) !== JSON.stringify([c.repo, c.runnerId, c.runnerName,
          c.run, c.attempt, c.workflowSha])) return 'PROVENANCE';
    return 'PASS';
  } catch { return 'PROVENANCE'; }
}
async function started(directory, env = process.env, checkpoint = async () => {}) {
  directory = root(directory);
  const c = context(env);
  return locked(directory, async () => {
    // Intent is durable before any read of gate state, under the shared process lock.
    const intentPath = recordFile(directory, c, 'intent');
    let intent;
    if (fs.existsSync(intentPath)) {
      intent = json(intentPath);
      if (JSON.stringify(intent.context) !== JSON.stringify(c)) fail('AMBIGUOUS_INVOCATION');
    } else {
      intent = { v: 1, context: c, invocation: c.invocation, hookPid: process.pid, hookParentPid: process.ppid };
      durable(intentPath, intent, true);
    }
    await checkpoint('intent');
    let s;
    try { s = state(directory); } catch {
      const refuse = recordFile(directory, c, 'refuse');
      if (!fs.existsSync(refuse)) durable(refuse, { invocation: c.invocation, reason: 'STATE_UNAVAILABLE' }, true);
      return false;
    }
    await checkpoint('state');
    if (intent.gateEpoch && intent.gateEpoch !== s.gateEpoch) return false;
    intent.gateEpoch = s.gateEpoch;
    durable(intentPath, intent);
    const reason = decision(directory, s, c);
    const passPath = recordFile(directory, c, 'pass');
    const refusePath = recordFile(directory, c, 'refuse');
    if (fs.existsSync(refusePath)) return false;
    if (reason !== 'PASS') {
      // A delayed duplicate never inherits an old PASS after closure/replacement.
      if (!fs.existsSync(passPath)) durable(refusePath, { invocation: c.invocation, reason }, true);
      return false;
    }
    if (c.job === 'admit') {
      const entry = ledgerFile(directory, s, c);
      if (!fs.existsSync(entry)) durable(entry, { gateEpoch: s.gateEpoch, invocation: c.invocation,
        affinity: [c.repo, c.runnerId, c.runnerName, c.run, c.attempt, c.workflowSha] }, true);
    }
    await checkpoint('ledger');
    if (!fs.existsSync(passPath)) durable(passPath, { invocation: c.invocation, gateEpoch: s.gateEpoch }, true);
    await checkpoint('pass');
    return true;
  });
}
async function completed(directory, env = process.env, checkpoint = async () => {}) {
  const c = context(env);
  return locked(directory, async () => {
    const intent = json(recordFile(directory, c, 'intent'));
    if (JSON.stringify(intent.context) !== JSON.stringify(c)) fail('COMPLETION_MISMATCH');
    if (fs.existsSync(recordFile(directory, c, 'pass'))) {
      const done = recordFile(directory, c, 'done');
      if (!fs.existsSync(done)) durable(done, { invocation: c.invocation, gateEpoch: intent.gateEpoch }, true);
    }
    await checkpoint('done'); // Marker can exist while this child AND its Worker are alive.
  });
}
async function initialize(directory, identity) {
  requireEmergencyReconciled(root(directory));
  return locked(directory, () => {
    for (const name of ['jobs', 'ledger']) {
      const dir = fileAt(directory, name);
      if (!fs.existsSync(dir)) fs.mkdirSync(dir);
      ordinary(dir);
    }
    let prior;
    if (fs.existsSync(fileAt(directory, 'state.json'))) {
      prior = state(directory);
      if (prior.repo !== identity.repo || prior.runnerId !== identity.runnerId || prior.runnerName !== identity.runnerName)
        fail('IDENTITY');
    }
    const s = validState({ ...identity, v: 1, ownershipEpoch: prior?.ownershipEpoch ?? 0,
      gateEpoch: crypto.randomUUID(), mode: 'INACTIVE',
      recoveryRequired: (prior?.recoveryRequired ?? false) || (prior && prior.mode !== 'INACTIVE') || false });
    durable(fileAt(directory, 'state.json'), s);
    return s;
  });
}
async function transition(directory, mode, registrations, noOwnedWorker) {
  return locked(directory, () => {
    const s = state(directory);
    if (mode === 'ACTIVE') {
      if (s.recoveryRequired || fs.existsSync(fileAt(directory, 'ownership-pending.json'))) fail('RECOVERY_REQUIRED');
      if (!Array.isArray(registrations) || registrations.length !== 1 ||
          registrations[0].id !== s.runnerId || registrations[0].name !== s.runnerName) fail('SOLE_REGISTRATION_REQUIRED');
      if (s.mode !== 'INACTIVE') fail('INVALID_TRANSITION');
      inspectUnlocked(directory, noOwnedWorker);
      // Caller must prove no old Worker and no unresolved records before reactivation.
      if (fs.existsSync(fileAt(directory, 'reactivation-blocked.json'))) fail('RECOVERY_REQUIRED');
      s.gateEpoch = crypto.randomUUID();
    } else if (mode === 'DRAINING') {
      if (s.mode !== 'ACTIVE') fail('INVALID_TRANSITION');
    } else if (mode !== 'INACTIVE') fail('INVALID_TRANSITION');
    s.mode = mode;
    durable(fileAt(directory, 'state.json'), s);
    return s;
  });
}
function inspectUnlocked(directory, noOwnedWorker) {
    const s = state(directory);
    if (s.mode !== 'INACTIVE') fail('GATE_NOT_INACTIVE');
    if (noOwnedWorker !== true) fail('WORKER_LIFETIME_UNCONFIRMED');
    const anomalies = [];
    for (const name of fs.readdirSync(fileAt(directory, 'jobs'))) {
      if (name.endsWith('.tmp')) fail('PARTIAL_RECORD');
      const record = json(fileAt(directory, 'jobs/' + name));
      if (!/^[a-f0-9]{64}\.(intent|pass|refuse|done)$/.test(name) ||
          record.invocation !== name.split('.')[0]) fail('INVALID_RECORD');
      if (!name.endsWith('.intent')) {
        const intent = json(fileAt(directory, 'jobs/' + record.invocation + '.intent'));
        if (name.endsWith('.pass') || name.endsWith('.done')) {
          if (record.gateEpoch !== intent.gateEpoch) fail('RECORD_EPOCH');
        }
        if (name.endsWith('.done') && !fs.existsSync(fileAt(directory, 'jobs/' + record.invocation + '.pass')))
          fail('ORPHAN_COMPLETION');
      }
      if (name.endsWith('.intent')) {
        const stem = 'jobs/' + record.invocation;
        const pass = fs.existsSync(fileAt(directory, stem + '.pass'));
        const refuse = fs.existsSync(fileAt(directory, stem + '.refuse'));
        if (pass && refuse) fail('CONFLICTING_RECORD');
        if ((!pass && !refuse) || (pass && !fs.existsSync(fileAt(directory, stem + '.done'))))
          anomalies.push(record.invocation);
      }
    }
    // Exact owned-tree absence can resolve a crashed hook, without deleting its evidence.
    if (anomalies.length) durable(fileAt(directory, 'anomaly-' + digest(JSON.stringify(anomalies)) + '.json'),
      { v: 1, kind: 'GATE_ANOMALY', gateEpoch: s.gateEpoch, invocations: anomalies });
    return { gateEpoch: s.gateEpoch, anomalies };
}
async function inspect(directory, noOwnedWorker) {
  return locked(directory, () => inspectUnlocked(directory, noOwnedWorker));
}
async function emergency(directory) {
  return locked(directory, () => {
    const s = state(directory); s.mode = 'INACTIVE'; s.recoveryRequired = true;
    durable(fileAt(directory, 'state.json'), s);
  });
}
module.exports = { ordinary, root, fileAt, durable, locked, state, initialize, transition,
  started, completed, inspect, emergency, digest };
