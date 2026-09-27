'use strict';
require('./gate.cjs').started(__dirname).then(pass => {
  process.stdout.write(pass ? 'GRL gate: PASS\n' : 'GRL gate: REFUSED\n');
  process.exitCode = pass ? 0 : 1;
}).catch(() => { process.stderr.write('GRL gate: unavailable; refused\n'); process.exitCode = 1; });
