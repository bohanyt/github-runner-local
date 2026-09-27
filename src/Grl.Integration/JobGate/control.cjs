'use strict';
const gate = require('./gate.cjs');
const config = require('./configuration.cjs');
async function main() {
  const [command, directory, argument] = process.argv.slice(2);
  const data = argument ? JSON.parse(argument) : {};
  let result;
  switch (command) {
    case 'initialize': result = await gate.initialize(directory, data); break;
    case 'transition': result = await gate.transition(directory, data.mode, data.registrations, data.noOwnedWorker); break;
    case 'inspect': result = await gate.inspect(directory, data.noOwnedWorker); break;
    case 'emergency': await gate.emergency(directory); break;
    case 'install': await config.install(data.runner, directory, data.runnerId); break;
    case 'restore': await config.restore(data.runner, directory); break;
    case 'verify': config.verify(data.runner, directory, data.runnerId); break;
    default: throw new Error('UNKNOWN_COMMAND');
  }
  process.stdout.write(JSON.stringify(result ?? { ok: true }) + '\n');
}
main().catch(() => { process.stderr.write('GRL gate operation refused or unresolved\n'); process.exitCode = 1; });
