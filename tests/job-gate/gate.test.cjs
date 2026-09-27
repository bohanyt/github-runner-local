'use strict';
const test = require('node:test');
const assert = require('node:assert/strict');
const fs = require('node:fs');
const os = require('node:os');
const path = require('node:path');
const crypto = require('node:crypto');
const { spawn } = require('node:child_process');
const { setTimeout: sleep } = require('node:timers/promises');
const source = path.resolve(__dirname, '../../src/Grl.Integration/JobGate');
const gate = require(path.join(source, 'gate.cjs'));
const config = require(path.join(source, 'configuration.cjs'));
const { OwnershipClient, transition } = require(path.join(source, 'ownership.cjs'));
const sha = '2c8da8834e785ba012001b9427bd68380401bec7';
const identity = { repo: 'owner/exec', runnerId: 42, runnerName: 'fixture', allowedWorkflowShas: [sha] };
async function fixture(t, configuredIdentity = identity) {
  const base = fs.mkdtempSync(path.join(os.tmpdir(), 'grl014-test-'));
  const directory = path.join(base, 'gate'), runner = path.join(base, 'runner');
  fs.mkdirSync(directory); fs.mkdirSync(runner);
  for (const name of fs.readdirSync(source)) fs.copyFileSync(path.join(source, name), path.join(directory, name));
  t.after(() => fs.rmSync(base, { recursive: true, force: true })); // Only this checked disposable fixture.
  await gate.initialize(directory, configuredIdentity);
  return { directory, runner, base };
}
const env = (job = 'admit', overrides = {}) => ({ ...process.env, GITHUB_REPOSITORY: identity.repo,
  RUNNER_NAME: identity.runnerName, GRL_GATE_RUNNER_ID: '42', GITHUB_SHA: sha, GITHUB_WORKFLOW_SHA: sha,
  GITHUB_RUN_ID: '100', GITHUB_RUN_ATTEMPT: '1', GITHUB_JOB: job,
  RUNNER_TRACKING_ID: 'github_' + crypto.randomUUID(), ...overrides });
const active = directory => gate.transition(directory, 'ACTIVE', [{ id: 42, name: 'fixture' }], true);
function child(script, environment = process.env) {
  const process = spawn(require('node:process').execPath, [script], { env: environment, stdio: ['ignore', 'pipe', 'pipe'] });
  let output = ''; process.stdout.on('data', data => { output += data; });
  let error = ''; process.stderr.on('data', data => { error += data; });
  return { process, result: new Promise(resolve => process.on('close', code => resolve({ code, output, error }))) };
}
async function waitFor(file) {
  for (let i = 0; i < 500; i++) { if (fs.existsSync(file)) return; await sleep(10); }
  throw new Error('fixture barrier timeout');
}

test('D3-T actual hook children reject before harmless profile, including stale PASS workspace', async t => {
  const f = await fixture(t); await active(f.directory);
  const old = path.join(f.runner, 'grl-outcome'); fs.mkdirSync(old);
  fs.writeFileSync(path.join(old, 'result.json'), '{"status":"PASS","run":"old"}');
  const cases = [env('execute'), env('admit', { GITHUB_REPOSITORY: 'other/exec' }),
    env('admit', { RUNNER_NAME: 'other' }), env('admit', { GRL_GATE_RUNNER_ID: '43' }),
    env('admit', { GITHUB_WORKFLOW_SHA: '' }), env('admit', { GITHUB_WORKFLOW_SHA: 'a'.repeat(40) }),
    env('admit', { GITHUB_SHA: 'b'.repeat(40) }), env('unknown')];
  for (const context of cases) {
    const result = await child(path.join(f.directory, 'job-started.js'), context).result;
    if (result.code === 0) fs.writeFileSync(path.join(f.runner, 'PROFILE_RAN'), 'unsafe');
    assert.equal(result.code, 1);
  }
  assert.equal(fs.existsSync(path.join(f.runner, 'PROFILE_RAN')), false);
  // The specifically permitted always() upload CAN see old files. It is not a new job result.
  assert.match(fs.readFileSync(path.join(old, 'result.json'), 'utf8'), /old/);
  const newRecords = fs.readdirSync(path.join(f.directory, 'jobs'));
  assert.equal(newRecords.filter(x => x.endsWith('.pass')).length, 0);
  assert.equal(newRecords.filter(x => x.endsWith('.refuse')).length, cases.length);
});

test('D3-T full rerun, downstream-only attempt change, historical SHA and fresh epoch affinity', async t => {
  const f = await fixture(t); await active(f.directory);
  const admit = env(); assert.equal(await gate.started(f.directory, admit), true);
  assert.equal(await gate.started(f.directory, env('execute')), true);
  assert.equal(await gate.started(f.directory, env('execute', { GITHUB_RUN_ATTEMPT: '2' })), false);
  assert.equal(await gate.started(f.directory, env('admit', { GITHUB_RUN_ATTEMPT: '2' })), true);
  assert.equal(await gate.started(f.directory, env('execute', { GITHUB_RUN_ATTEMPT: '2' })), true);
  for (const historical of ['3ca0449', '37b0f13'])
    assert.equal(await gate.started(f.directory, env('admit', { GITHUB_SHA: historical, GITHUB_WORKFLOW_SHA: historical })), false);
  await gate.transition(f.directory, 'DRAINING');
  assert.equal(await gate.started(f.directory, env()), false);
  assert.equal(await gate.started(f.directory, env('report')), true);
  await gate.transition(f.directory, 'INACTIVE');
  assert.equal(await gate.started(f.directory, admit), false); // duplicate old pass cannot reopen
  assert.equal(await gate.started(f.directory, env('verdict')), false);
  await active(f.directory);
  assert.equal(await gate.started(f.directory, admit), false); // old invocation epoch
  assert.equal(await gate.started(f.directory, env('execute')), false);
  assert.equal(await gate.started(f.directory, env()), true);
});

test('D2-T real intent-first process lock serializes closure with delayed decision', async t => {
  const f = await fixture(t); await active(f.directory);
  const ready = path.join(f.base, 'intent-ready'), release = path.join(f.base, 'continue');
  const harness = path.join(f.base, 'delayed.cjs');
  fs.writeFileSync(harness, `const fs=require('node:fs');const g=require(${JSON.stringify(path.join(f.directory, 'gate.cjs'))});
    g.started(${JSON.stringify(f.directory)},process.env,async stage=>{if(stage==='intent'){
      fs.writeFileSync(${JSON.stringify(ready)},'ready');while(!fs.existsSync(${JSON.stringify(release)}))await new Promise(r=>setTimeout(r,10));
    }}).then(pass=>{process.exitCode=pass?0:1});`);
  const hook = child(harness, env()); await waitFor(ready);
  const closing = gate.transition(f.directory, 'INACTIVE');
  await sleep(40); assert.equal(gate.state(f.directory).mode, 'ACTIVE');
  fs.writeFileSync(release, 'go'); assert.equal((await hook.result).code, 0); await closing;
  await assert.rejects(gate.inspect(f.directory, false), /WORKER/);
  assert.equal(await gate.started(f.directory, env()), false);
  const proof = await gate.inspect(f.directory, true);
  assert.equal(proof.anomalies.length, 1); // crashed/passed no done is retained, not deleted
});

test('D2-T completion marker with completed hook still running never proves worker exit', async t => {
  const f = await fixture(t); await active(f.directory);
  const c = env(); assert.equal((await child(path.join(f.directory, 'job-started.js'), c).result).code, 0);
  await gate.transition(f.directory, 'INACTIVE');
  const ready = path.join(f.base, 'done-ready'), release = path.join(f.base, 'complete-exit');
  const harness = path.join(f.base, 'completion.cjs');
  fs.writeFileSync(harness, `const fs=require('node:fs');require(${JSON.stringify(path.join(f.directory, 'gate.cjs'))})
    .completed(${JSON.stringify(f.directory)},process.env,async()=>{fs.writeFileSync(${JSON.stringify(ready)},'done');
    while(!fs.existsSync(${JSON.stringify(release)}))await new Promise(r=>setTimeout(r,10));});`);
  const completion = child(harness, c); await waitFor(ready);
  assert.equal(fs.readdirSync(path.join(f.directory, 'jobs')).filter(x => x.endsWith('.done')).length, 1);
  assert.equal(completion.process.exitCode, null);
  await assert.rejects(gate.inspect(f.directory, false), /LOCK_UNRESOLVED/);
  fs.writeFileSync(release, 'exit'); assert.equal((await completion.result).code, 0);
  await assert.rejects(gate.inspect(f.directory, false), /WORKER/); // Worker may outlive the hook too.
  assert.equal((await gate.inspect(f.directory, true)).anomalies.length, 0);
});

test('duplicates, missing state, partial writes, replacement and corrupt provenance fail closed', async t => {
  const f = await fixture(t); await active(f.directory);
  const c = env(); assert.equal(await gate.started(f.directory, c), true);
  assert.equal(await gate.started(f.directory, c), true);
  await gate.completed(f.directory, c); await gate.completed(f.directory, c);
  assert.equal(fs.readdirSync(path.join(f.directory, 'jobs')).filter(x => x.endsWith('.pass')).length, 1);
  const ledger = path.join(f.directory, 'ledger', fs.readdirSync(path.join(f.directory, 'ledger'))[0]);
  fs.writeFileSync(ledger, '{partial'); assert.equal(await gate.started(f.directory, env('execute')), false);
  fs.writeFileSync(path.join(f.directory, 'state.json'), '{partial');
  assert.equal((await child(path.join(f.directory, 'job-started.js'), env()).result).code, 1);
  fs.unlinkSync(path.join(f.directory, 'state.json'));
  assert.equal((await child(path.join(f.directory, 'job-started.js'), env()).result).code, 1);
  await gate.initialize(f.directory, identity);
  fs.writeFileSync(path.join(f.directory, 'jobs', 'partial.tmp'), '{');
  await assert.rejects(gate.inspect(f.directory, true), /PARTIAL/);
});

test('crash after intent/ledger/pass retains evidence and unresolved lock fences recovery', async t => {
  for (const crash of ['intent', 'state', 'ledger', 'pass']) {
    const f = await fixture(t); await active(f.directory);
    await assert.rejects(gate.started(f.directory, env(), async stage => { if (stage === crash) throw new Error('crash'); }));
    await gate.transition(f.directory, 'INACTIVE');
    assert.equal((await gate.inspect(f.directory, true)).anomalies.length, 1);
    assert.ok(fs.readdirSync(f.directory).some(x => x.startsWith('anomaly-')));
  }
  const f = await fixture(t); fs.mkdirSync(path.join(f.directory, 'gate.lock'));
  await assert.rejects(gate.locked(f.directory, () => {}, 30), /LOCK_UNRESOLVED/);
  assert.ok(fs.existsSync(path.join(f.directory, 'gate.lock')));
});

test('synthetic configuration checkpoint, crash points, idempotence and exact restoration', async t => {
  for (const crash of ['checkpoint', 'installed']) {
    const f = await fixture(t); const original = Buffer.from('# keep comments\r\nOTHER=value\r\n');
    fs.writeFileSync(path.join(f.runner, '.env'), original);
    await assert.rejects(config.install(f.runner, f.directory, 42, stage => { if (stage === crash) throw new Error('interrupt'); }));
    await config.install(f.runner, f.directory, 42); await config.install(f.runner, f.directory, 42);
    config.verify(f.runner, f.directory, 42);
    assert.ok(fs.readFileSync(path.join(f.runner, '.env')).subarray(0, original.length).equals(original));
    await config.restore(f.runner, f.directory); await config.restore(f.runner, f.directory);
    assert.ok(fs.readFileSync(path.join(f.runner, '.env')).equals(original));
    await config.install(f.runner, f.directory, 42); await config.restore(f.runner, f.directory);
    assert.ok(fs.readFileSync(path.join(f.runner, '.env')).equals(original));
    assert.ok(fs.readdirSync(f.directory).some(name => name.startsWith('configuration-restored-')));
  }
});

test('foreign hooks, env drift, unsafe placement, symlink descendants and emergency recovery refuse', async t => {
  const f = await fixture(t);
  fs.writeFileSync(path.join(f.runner, '.env'), 'ACTIONS_RUNNER_HOOK_JOB_STARTED=foreign.js\n');
  await assert.rejects(config.install(f.runner, f.directory, 42), /FOREIGN/);
  assert.match(fs.readFileSync(path.join(f.runner, '.env'), 'utf8'), /foreign/);
  assert.throws(() => config.placement(f.runner, f.runner), /PLACEMENT/);
  const nested = path.join(f.runner, 'nested'); fs.mkdirSync(nested);
  assert.throws(() => config.placement(f.runner, nested), /PLACEMENT/);
  const f2 = await fixture(t); await config.install(f2.runner, f2.directory, 42);
  fs.appendFileSync(path.join(f2.runner, '.env'), 'NEW=entry\n');
  await assert.rejects(config.restore(f2.runner, f2.directory), /DRIFT/);
  fs.renameSync(path.join(f2.directory, 'jobs'), path.join(f2.base, 'saved-jobs'));
  fs.symlinkSync(path.join(f2.base, 'saved-jobs'), path.join(f2.directory, 'jobs'), 'junction');
  await assert.rejects(gate.started(f2.directory, env()), /REPARSE/);
  await gate.emergency(f.directory); await assert.rejects(active(f.directory), /RECOVERY/);
});

class FakeRef {
  offline = true; queue = []; commits = new Map(); failRead = false; lostResponse = false; delayed = false;
  constructor(holder) {
    this.head = { sha: 'initial', parents: [], record: { v: 1, epoch: 0, holder, state: 'RELEASED', operation: crypto.randomUUID() } };
    this.commits.set(this.head.sha, this.head);
  }
  async readHead() { if (this.failRead) throw new Error('offline'); return structuredClone(this.head); }
  async createCommit(value) {
    assert.equal(value.emptyTree, true);
    const commit = { sha: gate.digest(JSON.stringify(value)), ...value }; this.commits.set(commit.sha, commit); return commit;
  }
  deliver(sha) { const candidate = this.commits.get(sha); transition(this.head, candidate); this.head = candidate; }
  async updateRef(sha, options) {
    assert.deepEqual(options, { force: false });
    if (this.delayed) this.queue.push(sha); else this.deliver(sha);
    if (this.lostResponse || this.delayed) throw new Error('lost response');
  }
}
const holder = name => ({ repo: identity.repo, runnerId: name === 'X' ? 42 : 43,
  name: name === 'X' ? 'fixture' : 'fixture-y' });

test('D1-T two exact-parent acquires in both delivery orders, duplicate delivery and no rebase', async t => {
  for (const order of [[0, 1], [1, 0]]) {
    const fixtures = [await fixture(t), await fixture(t, { ...identity, runnerId: 43, runnerName: 'fixture-y' })];
    const ref = new FakeRef(holder('X')); ref.delayed = true;
    const clients = fixtures.map((f, i) => new OwnershipClient(f.directory, holder(i ? 'Y' : 'X'), ref, 'synthetic'));
    await Promise.all(clients.map(c => assert.rejects(c.publish('ACQUIRED'), /UNRESOLVED/)));
    assert.equal(ref.queue.length, 2);
    ref.deliver(ref.queue[order[0]]);
    assert.throws(() => ref.deliver(ref.queue[order[1]]), /EXACT_PARENT/);
    assert.throws(() => ref.deliver(ref.queue[order[0]]), /EXACT_PARENT/);
    await clients[order[0]].reconcile(); await clients[order[0]].activate(true);
    await assert.rejects(clients[order[1]].activate(true), /UNRESOLVED/);
    await assert.rejects(clients[order[1]].publish('ACQUIRED'), /UNRESOLVED/);
    assert.equal(fixtures.filter(f => gate.state(f.directory).mode === 'ACTIVE').length, 1);
  }
});

test('D1-T delayed RELEASED, lost response, restart and Abort remain quarantined at old ACQUIRED', async t => {
  const f = await fixture(t); const ref = new FakeRef(holder('X'));
  let client = new OwnershipClient(f.directory, holder('X'), ref, 'synthetic');
  await client.publish('ACQUIRED'); await client.activate(true);
  await gate.transition(f.directory, 'DRAINING'); await gate.transition(f.directory, 'INACTIVE');
  await assert.rejects(client.publish('RELEASED'), /DRAINED/);
  await client.acknowledgeDrain(true); ref.delayed = true;
  await assert.rejects(client.publish('RELEASED'), /UNRESOLVED/);
  client = new OwnershipClient(f.directory, holder('X'), ref, 'synthetic'); // restart preserves intent
  await assert.rejects(client.abort(), /UNRESOLVED/);
  await assert.rejects(client.activate(true), /UNRESOLVED/);
  await assert.rejects(active(f.directory), /RECOVERY/);
  await assert.rejects(client.reconcile(), /UNRESOLVED/); // old self-owned read still unsafe
  ref.deliver(ref.queue[0]); await client.reconcile();
  await assert.rejects(client.abort(), /NOT_HOLDER/);
  await assert.rejects(client.activate(true), /NOT_HOLDER/);
  ref.delayed = false; ref.lostResponse = true;
  await client.publish('ACQUIRED'); await client.activate(true); // read reconciles actually landed operation
  assert.equal(gate.state(f.directory).ownershipEpoch, 2);
});

test('ownership rejects holder/epoch/merge parents, unreadable refs and disabled capability', async t => {
  const f = await fixture(t); const ref = new FakeRef(holder('X'));
  assert.throws(() => new OwnershipClient(f.directory, holder('X'), ref), /DISABLED/);
  assert.throws(() => new OwnershipClient(f.directory, holder('X'), { offline: false }, 'synthetic'), /DISABLED/);
  const client = new OwnershipClient(f.directory, holder('X'), ref, 'synthetic'); ref.failRead = true;
  await assert.rejects(client.activate(true)); assert.equal(gate.state(f.directory).mode, 'INACTIVE');
  ref.failRead = false;
  const candidate = { parents: ['initial'], record: { ...ref.head.record, state: 'ACQUIRED', epoch: 1 } };
  transition(ref.head, candidate);
  assert.throws(() => transition(ref.head, { ...candidate, parents: ['initial', 'other'] }), /EXACT_PARENT/);
  assert.throws(() => transition(ref.head, { ...candidate, record: { ...candidate.record, epoch: 4 } }), /ACQUIRE/);
  const acquired = { ...candidate, sha: 'acquired' };
  assert.throws(() => transition(acquired, { parents: ['acquired'], record: { ...candidate.record,
    state: 'RELEASED', holder: holder('Y') } }), /RELEASE/);
});

test('D1/D2 negative controls detect old label, quiet snapshot and an actual read-first child violation', async t => {
  const labels = new Set(); const xRead = labels.size, yRead = labels.size;
  if (!xRead) labels.add('X'); if (!yRead) labels.add('Y'); assert.equal(labels.size, 2);
  const server = { assigned: ['J'], busy: false }, worker = { running: false, killed: false };
  const quiet = () => !server.busy;
  const oldStopAllowed = quiet() && quiet();
  worker.running = server.assigned.shift() === 'J'; // delayed pickup after both snapshots
  if (oldStopAllowed) worker.killed = worker.running;
  assert.equal(worker.killed, true);
  const f = await fixture(t); await active(f.directory);
  const ready = path.join(f.base, 'read-first-ready'), proceed = path.join(f.base, 'late-decision');
  const profile = path.join(f.base, 'UNSAFE_PROFILE_EXECUTED'), script = path.join(f.base, 'bad-hook.cjs');
  fs.writeFileSync(script, `const fs=require('node:fs');
    const wasActive=JSON.parse(fs.readFileSync(${JSON.stringify(path.join(f.directory, 'state.json'))})).mode==='ACTIVE';
    fs.writeFileSync(${JSON.stringify(ready)},'read without intent');
    const timer=setInterval(()=>{if(fs.existsSync(${JSON.stringify(proceed)})){clearInterval(timer);
      if(wasActive)fs.writeFileSync(${JSON.stringify(profile)},'negative-control profile');}},10);`);
  const badHook = child(script); await waitFor(ready);
  await gate.transition(f.directory, 'INACTIVE');
  await gate.inspect(f.directory, true); // old read-first implementation has no durable intent to scan
  fs.writeFileSync(proceed, 'deliver decision after proof'); assert.equal((await badHook.result).code, 0);
  assert.equal(fs.existsSync(profile), true); // harness detected exactly the rejected ordering violation
});

test('D2-T every two-job start/completion/closure/proof order calls the filesystem implementation', async t => {
  const schedules = [];
  const visit = sequence => {
    if (sequence.length === 6) { schedules.push(sequence); return; }
    for (const event of ['s0', 'd0', 's1', 'd1', 'close', 'proof']) {
      if (sequence.includes(event) || (event[0] === 'd' && !sequence.includes('s' + event[1])) ||
          (event === 'proof' && !sequence.includes('close'))) continue;
      visit([...sequence, event]);
    }
  };
  visit([]); assert.equal(schedules.length, 90);
  for (const schedule of schedules) {
    const f = await fixture(t); await active(f.directory);
    const contexts = [env(), env('admit', { GITHUB_RUN_ID: '101' })]; const alive = new Set();
    let proved = false;
    for (const event of schedule) {
      if (event[0] === 's') {
        const i = Number(event[1]);
        const pass = await gate.started(f.directory, contexts[i]);
        if (proved) assert.equal(pass, false);
        if (pass) alive.add(i);
      } else if (event[0] === 'd') {
        const i = Number(event[1]); await gate.completed(f.directory, contexts[i]); alive.delete(i);
      } else if (event === 'close') await gate.transition(f.directory, 'INACTIVE');
      else if (alive.size) await assert.rejects(gate.inspect(f.directory, false), /WORKER/);
      else { await gate.inspect(f.directory, true); proved = true; }
    }
  }
});

test('three concurrent actual hooks race closure and a real crash keeps the lock/evidence quarantined', async t => {
  const f = await fixture(t); await active(f.directory);
  const hooks = [0, 1, 2].map(i => child(path.join(f.directory, 'job-started.js'), env('admit', { GITHUB_RUN_ID: String(200 + i) })));
  await gate.transition(f.directory, 'INACTIVE');
  const results = await Promise.all(hooks.map(c => c.result));
  for (const result of results) assert.ok([0, 1].includes(result.code));
  assert.equal(fs.readdirSync(path.join(f.directory, 'jobs')).filter(x => x.endsWith('.intent')).length, 3);
  assert.equal(await gate.started(f.directory, env()), false);
  const f2 = await fixture(t); await active(f2.directory);
  const crash = path.join(f2.base, 'crash.cjs');
  fs.writeFileSync(crash, `require(${JSON.stringify(path.join(f2.directory, 'gate.cjs'))}).started(${JSON.stringify(f2.directory)},process.env,
    async stage=>{if(stage==='intent')process.exit(23);});`);
  assert.equal((await child(crash, env()).result).code, 23);
  assert.equal(fs.readdirSync(path.join(f2.directory, 'jobs')).filter(x => x.endsWith('.intent')).length, 1);
  await assert.rejects(gate.locked(f2.directory, () => {}, 30), /UNRESOLVED/);
  assert.ok(fs.existsSync(path.join(f2.directory, 'gate.lock')));
});

test('S3 offline fake HTTP uses exact commit parents/empty tree/create-if-absent/force=false only', async t => {
  const { OfflineGitOwnershipTransport } = require(path.join(source, 'offline-git-transport.cjs'));
  const calls = [], commits = new Map(); let head;
  const request = async (method, route, body) => {
    calls.push({ method, route, body });
    const endpoint = route.replace('/repos/owner/control/git', '');
    if (method === 'POST' && endpoint === '/trees') return { status: 201, body: { sha: '4b825dc642cb6eb9a060e54bf8d69288fbee4904' } };
    if (method === 'POST' && endpoint === '/commits') {
      const sha = gate.digest(JSON.stringify(body)).slice(0, 40);
      commits.set(sha, { sha, tree: { sha: body.tree }, parents: body.parents.map(sha => ({ sha })), message: body.message });
      return { status: 201, body: { sha } };
    }
    if (method === 'GET' && endpoint.startsWith('/commits/')) return { status: 200, body: commits.get(endpoint.slice(9)) };
    if (method === 'POST' && endpoint === '/refs') {
      if (head) return { status: 422 }; head = body.sha; return { status: 201, body: {} };
    }
    if (method === 'GET' && endpoint === '/ref/heads/grl-control')
      return { status: 200, body: { ref: 'refs/heads/grl-control', object: { type: 'commit', sha: head } } };
    if (method === 'PATCH' && endpoint === '/refs/heads/grl-control') {
      assert.equal(body.force, false);
      const candidate = commits.get(body.sha);
      if (candidate.parents.length !== 1 || candidate.parents[0].sha !== head) return { status: 409 };
      head = body.sha; return { status: 200, body: {} };
    }
    throw new Error('Unexpected fake HTTP request');
  };
  request.offline = true;
  assert.throws(() => new OfflineGitOwnershipTransport('owner/control', request), /DISABLED/);
  const transport = new OfflineGitOwnershipTransport('owner/control', request, 'synthetic');
  await transport.bootstrapReleased({ v: 1, epoch: 0, state: 'RELEASED', holder: holder('X'), operation: crypto.randomUUID() });
  const f = await fixture(t); const client = new OwnershipClient(f.directory, holder('X'), transport, 'synthetic');
  await client.publish('ACQUIRED'); await client.activate(true);
  await assert.rejects(transport.bootstrapReleased({ v: 1, epoch: 0, state: 'RELEASED', holder: holder('Y'), operation: crypto.randomUUID() }));
  await assert.rejects(transport.updateRef(head, { force: true }), /FORCE/);
  assert.equal(calls.filter(c => c.method === 'PATCH').length, 1);
  assert.equal(calls.some(c => c.route.includes('/actions/') || c.method === 'DELETE'), false);
});

test('D1-T all twenty send/deliver/activate interleavings preserve a single pass-capable gate', async t => {
  const schedules = [];
  function visit(sequence) {
    if (sequence.length === 6) { schedules.push(sequence); return; }
    for (const event of ['s0', 'l0', 'a0', 's1', 'l1', 'a1']) {
      if (sequence.includes(event) || (event[0] === 'l' && !sequence.includes('s' + event[1])) ||
          (event[0] === 'a' && !sequence.includes('l' + event[1]))) continue;
      visit([...sequence, event]);
    }
  }
  visit([]); assert.equal(schedules.length, 20);
  for (const schedule of schedules) {
    const f = [await fixture(t), await fixture(t, { ...identity, runnerId: 43, runnerName: 'fixture-y' })];
    const ref = new FakeRef(holder('X')); ref.delayed = true;
    const clients = f.map((value, i) => new OwnershipClient(value.directory, holder(i ? 'Y' : 'X'), ref, 'synthetic'));
    const candidate = [];
    for (const event of schedule) {
      const i = Number(event[1]);
      if (event[0] === 's') {
        try { await clients[i].publish('ACQUIRED'); } catch { /* denied or durable unresolved publication */ }
        const pending = path.join(f[i].directory, 'ownership-pending.json');
        if (fs.existsSync(pending)) candidate[i] = JSON.parse(fs.readFileSync(pending)).candidate;
      } else if (event[0] === 'l' && candidate[i]) {
        try { ref.deliver(candidate[i]); } catch { /* exact-parent losing delivery */ }
      } else if (event[0] === 'a') {
        try { await clients[i].reconcile(); await clients[i].activate(true); } catch { /* remains INACTIVE */ }
      }
      assert.ok(f.filter(value => ['ACTIVE', 'DRAINING'].includes(gate.state(value.directory).mode)).length <= 1);
    }
  }
});


test('lock-independent emergency evidence fences hooks, startup and activation without fabricating state', async t => {
  const f = await fixture(t); await active(f.directory);
  const original = fs.readFileSync(path.join(f.directory, 'state.json'));
  // A partially written fallback is also uncertainty, never ignored on restart.
  fs.writeFileSync(path.join(f.directory, 'emergency-recovery-required.json'), '{');
  assert.equal(await gate.started(f.directory, env()), false);
  assert.equal((await child(path.join(f.directory, 'job-started.js'), env()).result).code, 1);
  await assert.rejects(active(f.directory), /RECOVERY_REQUIRED/);
  fs.mkdirSync(path.join(f.directory, 'gate.lock'));
  await assert.rejects(gate.initialize(f.directory, identity), /RECOVERY_REQUIRED/);
  assert.deepEqual(fs.readFileSync(path.join(f.directory, 'state.json')), original);
  assert.equal(fs.existsSync(path.join(f.directory, 'gate.lock')), true);
});
