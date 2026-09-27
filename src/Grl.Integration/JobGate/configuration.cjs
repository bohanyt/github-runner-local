'use strict';
const fs = require('node:fs');
const path = require('node:path');
const gate = require('./gate.cjs');
const keys = ['ACTIONS_RUNNER_HOOK_JOB_STARTED', 'ACTIONS_RUNNER_HOOK_JOB_COMPLETED', 'GRL_GATE_RUNNER_ID'];
function placement(runner, directory) {
  runner = gate.root(runner); directory = gate.root(directory);
  const contains = (a, b) => a.toLowerCase() === b.toLowerCase() || b.toLowerCase().startsWith(a.toLowerCase() + path.sep);
  if (contains(runner, directory) || contains(directory, runner) ||
      directory.split(path.sep).some(x => x.toLowerCase() === '_work')) throw new Error('HOOK_PLACEMENT');
  return [runner, directory];
}
function desired(original, directory, runnerId) {
  const text = original.toString('utf8');
  if (!Buffer.from(text).equals(original) || keys.some(k => new RegExp('^\\s*' + k + '\\s*=', 'mi').test(text)))
    throw new Error('FOREIGN_HOOK_OR_ENCODING');
  const newline = text.includes('\r\n') ? '\r\n' : '\n';
  return Buffer.from(text + (text && !text.endsWith('\n') ? newline : '') +
    keys.map((k, i) => k + '=' + [path.join(directory, 'job-started.js'),
      path.join(directory, 'job-completed.js'), String(runnerId)][i]).join(newline) + newline);
}
async function install(runner, directory, runnerId, checkpoint = () => {}) {
  [runner, directory] = placement(runner, directory);
  if (!Number.isSafeInteger(runnerId) || runnerId <= 0) throw new Error('IDENTITY');
  return gate.locked(directory, async () => {
    const env = gate.fileAt(runner, '.env');
    const journal = gate.fileAt(directory, 'configuration.json');
    let saved;
    const current = fs.existsSync(env) ? fs.readFileSync(env) : Buffer.alloc(0);
    if (fs.existsSync(journal)) {
      saved = JSON.parse(fs.readFileSync(journal));
      if (saved.runnerHash !== gate.digest(runner.toLowerCase()) || saved.runnerId !== runnerId)
        throw new Error('CHECKPOINT_MISMATCH');
      if (!saved.restored) {
        if (current.equals(Buffer.from(saved.installed, 'base64'))) return;
        if (!current.equals(Buffer.from(saved.original, 'base64'))) throw new Error('ENV_DRIFT');
      } else {
        const archive = gate.fileAt(directory, 'configuration-restored-' + gate.digest(JSON.stringify(saved)) + '.json');
        if (!fs.existsSync(archive)) gate.durable(archive, saved, true);
        saved = null;
      }
    }
    if (!saved) {
      const installed = desired(current, directory, runnerId);
      saved = { v: 1, runnerHash: gate.digest(runner.toLowerCase()), runnerId,
        existed: fs.existsSync(env), original: current.toString('base64'), installed: installed.toString('base64') };
      // Contains configuration bytes locally; never print or publish this checkpoint.
      gate.durable(journal, saved, !fs.existsSync(journal));
    }
    await checkpoint('checkpoint');
    gate.durable(env, Buffer.from(saved.installed, 'base64'));
    await checkpoint('installed');
  });
}
async function restore(runner, directory) {
  [runner, directory] = placement(runner, directory);
  return gate.locked(directory, () => {
    const env = gate.fileAt(runner, '.env');
    const journal = gate.fileAt(directory, 'configuration.json');
    const saved = JSON.parse(fs.readFileSync(journal));
    if (saved.runnerHash !== gate.digest(runner.toLowerCase())) throw new Error('CHECKPOINT_MISMATCH');
    const original = Buffer.from(saved.original, 'base64');
    const current = fs.existsSync(env) ? fs.readFileSync(env) : Buffer.alloc(0);
    if (!current.equals(original) && !current.equals(Buffer.from(saved.installed, 'base64'))) throw new Error('ENV_DRIFT');
    if (saved.existed) gate.durable(env, original);
    else if (fs.existsSync(env)) fs.unlinkSync(env);
    saved.restored = true; gate.durable(journal, saved);
  });
}
function verify(runner, directory, runnerId) {
  [runner, directory] = placement(runner, directory);
  const saved = JSON.parse(fs.readFileSync(gate.fileAt(directory, 'configuration.json')));
  if (saved.restored || saved.runnerId !== runnerId || saved.runnerHash !== gate.digest(runner.toLowerCase()) ||
      !fs.readFileSync(gate.fileAt(runner, '.env')).equals(Buffer.from(saved.installed, 'base64')))
    throw new Error('HOOKS_UNVERIFIED');
  for (const name of ['job-started.js', 'job-completed.js', 'gate.cjs']) gate.ordinary(gate.fileAt(directory, name));
}
module.exports = { placement, install, restore, verify };
