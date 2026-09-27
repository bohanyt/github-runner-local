'use strict';
require('./gate.cjs').completed(__dirname).catch(() => {
  process.stderr.write('GRL gate: completion unresolved\n'); process.exitCode = 1;
});
