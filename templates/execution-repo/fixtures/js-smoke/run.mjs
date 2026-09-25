import { readFileSync, writeFileSync } from 'node:fs';
import { dirname, join } from 'node:path';
import { fileURLToPath } from 'node:url';

const here = dirname(fileURLToPath(import.meta.url));
const behavior = readFileSync(join(here, 'behavior.txt'), 'utf8').trim();
if (behavior !== 'PASS' && behavior !== 'FAIL') {
  process.stderr.write('Invalid JS smoke fixture behavior.\n');
  process.exitCode = 2;
} else {
  const failure = behavior === 'FAIL';
  const xml = failure
    ? '<testsuite tests="1" failures="1" errors="0" skipped="0"><testcase name="fixture"><failure message="expected fixture failure"/></testcase></testsuite>'
    : '<testsuite tests="1" failures="0" errors="0" skipped="0"><testcase name="fixture"/></testsuite>';
  writeFileSync(join(here, 'results.xml'), xml + '\n', 'utf8');
  process.stdout.write(failure ? 'JS smoke fixture FAIL\n' : 'JS smoke fixture PASS\n');
  process.exitCode = failure ? 1 : 0;
}
