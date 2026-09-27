import test from 'node:test';
import assert from 'node:assert/strict';
import { readFileSync } from 'node:fs';
import { lintSource } from '../tools/lint.mjs';
import { gateInventory, loadActionMetadata } from '../tools/gate-inventory.mjs';
const workflow = readFileSync(new URL('../.github/workflows/grl-dispatch.yml', import.meta.url), 'utf8').replaceAll('\r\n', '\n');
test('exact unconditional inventory and pinned action pre/post metadata', () => {
  assert.deepEqual(gateInventory(workflow), []);
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
  assert.ok(lintSource({ workflow: candidate, profiles: [], runAction: '', reportLibrary: '', verdictAction: '' })
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
  assert.ok(gateInventory(workflow.replace(profileStep, replacement)).includes('GATE_STEP_GRAMMAR'));
});

test('unsupported steps collection layout cannot disappear from inventory', () => {
  for (const key of ['"steps":', "'steps':", 'steps: [{if: always()}]'])
    assert.ok(gateInventory(workflow.replace('    steps:', '    ' + key)).includes('GATE_STEP_GRAMMAR'));
  assert.deepEqual(gateInventory(workflow), []); // Exact reviewed artifact exception remains allowed.
});
