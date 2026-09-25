import { readFileSync } from 'node:fs';
import { dirname, resolve } from 'node:path';
import { fileURLToPath } from 'node:url';

export function schemaDrift(templateRoot) {
  const repositoryRoot = resolve(templateRoot, '../..');
  const names = ['grl.request.v1.schema.json', 'grl.result.v1.schema.json'];
  return names.filter(name => !readFileSync(resolve(templateRoot, 'schemas', name))
    .equals(readFileSync(resolve(repositoryRoot, 'schemas', name))));
}

if (process.argv[1] && resolve(process.argv[1]) === fileURLToPath(import.meta.url)) {
  const root = resolve(dirname(fileURLToPath(import.meta.url)), '..');
  const drift = schemaDrift(root);
  if (drift.length) {
    process.stderr.write('Schema mirror drift: ' + drift.join(', ') + '\n');
    process.exitCode = 1;
  } else process.stdout.write('Schema mirrors: byte-for-byte PASS\n');
}
