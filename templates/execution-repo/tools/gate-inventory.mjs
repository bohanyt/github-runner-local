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

// Bounded block step-mapping grammar. Every line/key is consumed or rejected;
// flow mappings, aliases, block scalars and ambiguous indentation are unsupported.
function stepMapping(text, errors) {
  const values = new Map();
  let inputs = false;
  const inputKeys = new Set();
  for (const [index, line] of text.split('\n').entries()) {
    const prefix = index === 0 ? /^      - (.*)$/ : /^        (\S.*)$/;
    const top = prefix.exec(line);
    if (!top) {
      const input = inputs && /^          ([A-Za-z][A-Za-z0-9-]*): (.+)$/.exec(line);
      if (!input || inputKeys.has(input[1]) || /^[|>&*!{[]/.test(input[2])) errors.push('GATE_STEP_GRAMMAR');
      else inputKeys.add(input[1]);
      continue;
    }
    const entry = /^(?:([A-Za-z][A-Za-z0-9-]*)|"([A-Za-z][A-Za-z0-9-]*)"|'([A-Za-z][A-Za-z0-9-]*)'): *(.*)$/.exec(top[1]);
    if (!entry) { errors.push('GATE_STEP_GRAMMAR'); inputs = false; continue; }
    const key = entry[1] ?? entry[2] ?? entry[3], value = entry[4];
    if (!['id', 'uses', 'if', 'with', 'continue-on-error'].includes(key) || values.has(key)) errors.push('GATE_STEP_GRAMMAR');
    values.set(key, value);
    inputs = key === 'with';
    if (inputs ? value !== '' : value === '' || /^[|>&*{[]/.test(value)) errors.push('GATE_STEP_GRAMMAR');
  }
  return values;
}
const scalar = value => value && ((value.startsWith('"') && value.endsWith('"')) ||
  (value.startsWith("'") && value.endsWith("'"))) ? value.slice(1, -1) : value;

// Deliberately a narrow grammar for this fixed template, not a permissive general YAML interpreter.
// Unsupported conditions/layouts/action metadata fail closed and require a new inventory decision.
export function gateInventory(workflow, metadata = loadActionMetadata()) {
  const errors = [];
  const lines = normalize(workflow).split('\n');
  let job, inSteps = false, steps = [], current;
  const stepJobs = new Set();
  const flush = () => { if (current) steps.push({ job, text: current.join('\n').trimEnd() }); current = null; };
  for (const line of lines) {
    const match = /^  ([a-z]+):$/.exec(line);
    if (match) { flush(); job = match[1]; inSteps = false; }
    if (line === '    steps:') {
      if (!['admit', 'execute', 'report', 'verdict'].includes(job) || stepJobs.has(job)) errors.push('GATE_STEP_GRAMMAR');
      stepJobs.add(job); inSteps = true; continue;
    }
    if (/^    (?:steps|"steps"|'steps')\s*:/.test(line)) errors.push('GATE_STEP_GRAMMAR');
    if (!inSteps || !line.trim()) continue;
    if (/^      - /.test(line)) { flush(); current = [line]; }
    else if (/^        /.test(line) && current) current.push(line);
    else errors.push('GATE_STEP_GRAMMAR');
  }
  flush();
  if (stepJobs.size !== 4) errors.push('GATE_STEP_GRAMMAR');
  let exceptions = 0;
  for (const step of steps) {
    const mapping = stepMapping(step.text, errors);
    const condition = scalar(mapping.get('if'));
    if (condition && (/^[*!&]/.test(condition) || /\bsuccess\s*\(/i.test(condition) &&
        !/^(?:\$\{\{\s*)?success\(\)(?:\s*\}\})?$/.test(condition))) errors.push('GATE_CONDITION_UNKNOWN');
    if (condition && (/[|>]/.test(condition) || /(?:always|failure|cancelled)\s*\(/i.test(condition))) {
      if (step.job !== 'execute' || step.text !== allowedArtifact) errors.push('GATE_UNCONDITIONAL_STEP');
      else exceptions++;
    }
    if (condition && !/(?:always|failure|cancelled)\s*\(/i.test(condition) &&
        !['success()', '${{ success() }}', "steps.run-profile.outputs.execution_status == 'BLOCKED'"].includes(condition))
      errors.push('GATE_CONDITION_UNKNOWN');
    const ref = scalar(mapping.get('uses'));
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
