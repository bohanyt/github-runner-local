import { readFileSync } from 'node:fs';
import { createHash } from 'node:crypto';
import { fileURLToPath } from 'node:url';
const root = new URL('../', import.meta.url);
const manifest = JSON.parse(readFileSync(new URL('action-metadata/manifest.json', import.meta.url)));
const normalize = text => text.replaceAll('\r\n', '\n');
export function loadActionMetadata() {
  const metadata = {};
  for (const ref of Object.keys(manifest)) {
    const file = ref.replace('actions/', '').replace('@', '-') + '.json';
    const snapshot = JSON.parse(readFileSync(new URL('action-metadata/' + file, import.meta.url), 'utf8'));
    if (snapshot.source !== 'https://raw.githubusercontent.com/' + ref.replace('@', '/') + '/action.yml')
      throw new Error('GATE_METADATA_SOURCE');
    metadata[ref] = snapshot.yaml;
  }
  for (const name of ['grl-admit', 'grl-run-profile', 'grl-report', 'grl-verdict'])
    metadata['./.github/actions/' + name] = readFileSync(new URL('.github/actions/' + name + '/action.yml', root), 'utf8');
  return metadata;
}
const allowedArtifact = `      - uses: actions/upload-artifact@ea165f8d65b6e75b540449e92b4886f43607fa02
        if: always()
        with:
          name: grl-execution-outcome-${'${{ github.run_id }}'}
          path: grl-outcome
          retention-days: 3
          if-no-files-found: warn
          overwrite: true`;

// Deliberately a narrow grammar for this fixed template, not a permissive general YAML interpreter.
// Unsupported conditions/layouts/action metadata fail closed and require a new inventory decision.
export function gateInventory(workflow, metadata = loadActionMetadata()) {
  const errors = [];
  const lines = normalize(workflow).split('\n');
  let job, inSteps = false, steps = [], current;
  const flush = () => { if (current) steps.push({ job, text: current.join('\n').trimEnd() }); current = null; };
  for (const line of lines) {
    const match = /^  ([a-z]+):$/.exec(line);
    if (match) { flush(); job = match[1]; inSteps = false; }
    if (line === '    steps:') { inSteps = true; continue; }
    if (!inSteps || !line.trim()) continue;
    if (/^      - /.test(line)) { flush(); current = [line]; }
    else if (/^        /.test(line) && current) current.push(line);
    else errors.push('GATE_STEP_GRAMMAR');
  }
  flush();
  let exceptions = 0;
  for (const step of steps) {
    const condition = /^        if:\s*(.*)$/m.exec(step.text)?.[1];
    if (condition && (/^[*!&]/.test(condition) || /\bsuccess\s*\(/i.test(condition) &&
        !/^(?:\$\{\{\s*)?success\(\)(?:\s*\}\})?$/.test(condition))) errors.push('GATE_CONDITION_UNKNOWN');
    if (condition && (/[|>]/.test(condition) || /(?:always|failure|cancelled)\s*\(/i.test(condition))) {
      if (step.job !== 'execute' || step.text !== allowedArtifact) errors.push('GATE_UNCONDITIONAL_STEP');
      else exceptions++;
    }
    const ref = /\buses:\s*([^\s]+)/.exec(step.text)?.[1];
    if (!ref) { errors.push('GATE_ACTION_UNKNOWN'); continue; }
    const key = ref.replace('./grl-trusted/', './');
    const action = metadata[key];
    if (!action) { errors.push('GATE_ACTION_UNKNOWN'); continue; }
    const text = normalize(action);
    if (!/^runs:\n/m.test(text) || !/^  using:\s*['"]?node20['"]?\s*$/m.test(text) ||
        !/^  main:\s*[^\n]+$/m.test(text) || /^\s*['"]?pre(?:-if)?['"]?\s*:/m.test(text))
      errors.push('GATE_ACTION_PRE_OR_UNKNOWN');
    if (manifest[key] && createHash('sha256').update(text).digest('hex') !== manifest[key])
      errors.push('GATE_METADATA_PIN');
    // Only checkout's reviewed post cleanup is allowed; it is registered when main ran.
    if (/^\s*post(?:-if)?:/m.test(text) && !key.startsWith('actions/checkout@')) errors.push('GATE_ACTION_POST_UNKNOWN');
  }
  if (exceptions !== 1) errors.push('GATE_EXCEPTION_INVENTORY');
  return [...new Set(errors)];
}
export const metadataSourceRoot = fileURLToPath(root);
