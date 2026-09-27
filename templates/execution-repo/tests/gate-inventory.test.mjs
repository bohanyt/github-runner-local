import test from 'node:test';
import assert from 'node:assert/strict';
import { readFileSync, readdirSync } from 'node:fs';
import { fileURLToPath } from 'node:url';
import { schemaDrift } from '../tools/schema-check.mjs';
import { lintSource, changedPaths } from '../tools/lint.mjs';
import { gateInventory, loadActionMetadata } from '../tools/gate-inventory.mjs';
const workflow = readFileSync(new URL('../.github/workflows/grl-dispatch.yml', import.meta.url), 'utf8').replaceAll('\r\n', '\n');
// Match the complete lintTemplate inputs; fixtures replace only in-memory workflow text.
const readTemplate = name => readFileSync(new URL('../' + name, import.meta.url), 'utf8');
const actionMain = name => readTemplate('.github/actions/' + name + '/main.mjs');
const libraryNames = readdirSync(new URL('../lib/', import.meta.url)).filter(name => name.endsWith('.mjs'));
const lintInputs = {
  profiles: readdirSync(new URL('../profiles/', import.meta.url)).filter(name => name.endsWith('.json'))
    .map(name => readTemplate('profiles/' + name)),
  runAction: actionMain('grl-run-profile'), reportLibrary: readTemplate('lib/reporting.mjs'),
  verdictAction: actionMain('grl-verdict'),
  runtimeSources: ['grl-admit', 'grl-report', 'grl-run-profile', 'grl-verdict'].map(actionMain)
    .concat(libraryNames.map(name => readTemplate('lib/' + name))),
  changedPaths: changedPaths(fileURLToPath(new URL('../../../', import.meta.url))),
  drift: schemaDrift(fileURLToPath(new URL('../', import.meta.url)))
};
const fullLint = candidate => lintSource({ ...lintInputs, workflow: candidate });
const rejectsGrammar = candidate => {
  assert.ok(gateInventory(candidate).includes('GATE_STEP_GRAMMAR'));
  assert.ok(fullLint(candidate).includes('GATE_STEP_GRAMMAR'));
};

test('exact unconditional inventory and pinned action pre/post metadata', () => {
  assert.deepEqual(gateInventory(workflow), []);
  assert.deepEqual(fullLint(workflow), []);
  const metadata = loadActionMetadata();
  assert.equal(Object.keys(metadata).length, 7);
  assert.match(metadata['actions/checkout@11bd71901bbe5b1630ceea73d27597364c9af683'], /post:/);
});
test('unknown exceptions, altered artifact, multiline status expressions and action pins refuse', () => {
  for (const candidate of [
    workflow.replace('if: always()', 'if: failure()'),
    workflow.replace('if: always()', 'if: cancelled()'),
    workflow.replace('path: grl-outcome\n', 'path: grl-target\n'),
    workflow.replace('if: always()', 'if: always() || true'),
    workflow.replace('if: always()', 'if: >-\n          always()'),
    workflow.replace('if-no-files-found: warn', 'if-no-files-found: error'),
    workflow.replace('retention-days: 3', 'retention-days: 2'),
    workflow.replace('actions/checkout@11bd71901bbe5b1630ceea73d27597364c9af683', 'vendor/unknown@' + 'a'.repeat(40))
  ]) assert.ok(gateInventory(candidate).length > 0);
});
test('action pre, unknown post, missing or changed pinned metadata fails closed', () => {
  const initial = loadActionMetadata();
  for (const key of Object.keys(initial)) {
    for (const mutation of [text => text + '\n  pre: unsafe.js\n', text => text + '\n  "pre-if": always()\n']) {
      const metadata = { ...initial, [key]: mutation(initial[key]) };
      assert.ok(gateInventory(workflow, metadata).includes('GATE_ACTION_PRE_OR_UNKNOWN'));
    }
    const missing = { ...initial }; delete missing[key];
    assert.ok(gateInventory(workflow, missing).includes('GATE_ACTION_UNKNOWN'));
  }
  const key = Object.keys(initial).find(x => x.startsWith('actions/upload-artifact'));
  assert.ok(gateInventory(workflow, { ...initial, [key]: initial[key] + '\n# changed\n' }).includes('GATE_METADATA_PIN'));
});

const profileStep = `      - id: run-profile
        uses: ./grl-trusted/.github/actions/grl-run-profile
        with:
          identity-b64: ${'${{ needs.admit.outputs.identity_b64 }}'}`;
for (const [name, replacement] of [
  ['first-key always', `      - if: always()
        id: run-profile
        uses: ./grl-trusted/.github/actions/grl-run-profile`],
  ['double-quoted key', `      - id: run-profile
        "if": always()
        uses: ./grl-trusted/.github/actions/grl-run-profile`],
  ['single-quoted key', `      - id: run-profile
        'if': always()
        uses: ./grl-trusted/.github/actions/grl-run-profile`],
  ['first-key not cancelled', `      - if: '!cancelled()'
        id: run-profile
        uses: ./grl-trusted/.github/actions/grl-run-profile`],
  ['first quoted key failure', `      - "if": failure()
        uses: ./grl-trusted/.github/actions/grl-run-profile
        id: run-profile`],
  ['first single-quoted key cancelled', `      - 'if': cancelled()
        uses: ./grl-trusted/.github/actions/grl-run-profile`],
  ['last-key expression', `      - uses: ./grl-trusted/.github/actions/grl-run-profile
        id: run-profile
        if: ${'${{ !cancelled() }}'}`],
  ['condition after input mapping', profileStep + '\n        if: failure()'],
  ['quoted failure expression', profileStep + '\n        if: "failure()"'],
  ['quoted cancelled expression', profileStep + "\n        if: 'cancelled()'"]
]) test('unsafe structural condition: ' + name, () => {
  assert.ok(workflow.includes(profileStep));
  const candidate = workflow.replace(profileStep, replacement);
  assert.ok(gateInventory(candidate).includes('GATE_UNCONDITIONAL_STEP'));
  assert.ok(fullLint(candidate)
    .includes('GATE_UNCONDITIONAL_STEP'));
});

for (const [name, replacement] of [
  ['flow step', `      - {if: always(), uses: ./grl-trusted/.github/actions/grl-run-profile}`],
  ['unknown key', profileStep + '\n        unexpected: always()'],
  ['duplicate key', profileStep + '\n        if: success()\n        "if": always()'],
  ['block condition', profileStep + '\n        if: >-\n          always()'],
  ['ambiguous indentation', profileStep + '\n         if: always()'],
  ['explicit mapping key', profileStep + '\n        ? if\n        : always()'],
  ['alias merge', profileStep + '\n        <<: *unsafe'],
  ['bare sequence mapping', `      -\n        if: always()\n        uses: ./grl-trusted/.github/actions/grl-run-profile`]
]) test('unsupported step grammar: ' + name, () => {
  rejectsGrammar(workflow.replace(profileStep, replacement));
});

test('unsupported steps collection layout cannot disappear from inventory', () => {
  for (const key of ['"steps":', "'steps':", 'steps: [{if: always()}]'])
    rejectsGrammar(workflow.replace('    steps:', '    ' + key));
  assert.deepEqual(gateInventory(workflow), []); // Exact reviewed artifact exception remains allowed.
});


const beforeAdmit = child => workflow.replace('jobs:\n', 'jobs:\n' + child + '\n');
const flowJob = condition => `  surprise: {runs-on: [self-hosted, Windows, X64, grl-exec], steps: [{uses: ./.github/actions/grl-verdict, if: ${condition}}]}`;
for (const condition of ['always()', 'failure()', 'cancelled()'])
  test('entire jobs mapping rejects reviewer flow surprise before admit: ' + condition, () => {
    rejectsGrammar(beforeAdmit(flowJob(condition)));
  });

for (const [name, child] of [
  ['extra block job', '  surprise:\n    runs-on: [self-hosted, Windows, X64, grl-exec]\n    steps:\n      - uses: ./.github/actions/grl-verdict\n        if: always()'],
  ['flow expected job', '  admit: {runs-on: [self-hosted, Windows, X64, grl-exec], steps: [{uses: ./.github/actions/grl-verdict, if: always()}]}'],
  ['double quoted unexpected key', '  "surprise": {}'],
  ['single quoted unexpected key', "  'surprise': {}"],
  ['quoted expected key', '  "admit":'],
  ['explicit mapping key', '  ? surprise\n  : {}'],
  ['merge alias entry', '  <<: *unexpected'],
  ['aliased job entry', '  surprise: *unexpected'],
  ['anchored expected job', '  admit: &unexpected'],
  ['ambiguous one-space indentation', ' surprise: {}'],
  ['ambiguous three-space indentation', '   surprise: {}'],
  ['ambiguous four-space extra header', '    surprise: {}'],
  ['tab indentation', '\tsurprise: {}'],
  ['unknown direct value', '  unexpected'],
  ['direct flow mapping', '  {surprise: {steps: []}}'],
  ['direct block scalar', '  |\n    surprise'],
  ['duplicate expected job', '  admit:\n    steps:\n      - uses: ./.github/actions/grl-verdict']
]) test('unsupported jobs child: ' + name, () => rejectsGrammar(beforeAdmit(child)));

for (const [name, mutate] of [
  ['flow jobs container', text => text.replace('jobs:', 'jobs: {surprise: {steps: []}}')],
  ['block scalar jobs container', text => text.replace('jobs:', 'jobs: |')],
  ['aliased jobs container', text => text.replace('jobs:', 'jobs: *unexpected')],
  ['quoted jobs key', text => text.replace('jobs:', '"jobs":')],
  ['missing jobs key', text => text.replace('jobs:\n', '')],
  ['duplicate jobs container', text => text + '\njobs:\n'],
  ['later quoted jobs override', text => text + '\n"jobs": {surprise: {steps: []}}\n'],
  ['escaped quoted jobs override after another top-level key', text => text + '\nenv: {}\n"\\x6aobs": {surprise: {steps: []}}\n'],
  ['duplicate steps collection', text => text.replace('    steps:', '    steps:\n      - uses: ./.github/actions/grl-verdict\n    steps:')],
  ['missing one job steps collection', text => text.replace('    steps:', '    outputs:')],
  ['empty job steps collection', text => text.replace(/(  admit:\n[\s\S]*?    steps:)[\s\S]*?(?=\n  execute:)/, '$1\n')]
]) test('unsupported jobs container or collection: ' + name, () => rejectsGrammar(mutate(workflow)));

test('extra job cannot disappear between jobs or after the final job', () => {
  rejectsGrammar(workflow.replace('  execute:', flowJob('always()') + '\n  execute:'));
  rejectsGrammar(workflow + '\n' + flowJob('always()') + '\n');
});

test('exact reviewed workflow and upload exception remain valid through full lint', () => {
  assert.equal((workflow.match(/^        if: always\(\)$/gm) ?? []).length, 1);
  assert.deepEqual(gateInventory(workflow), []);
  assert.deepEqual(fullLint(workflow), []);
});
