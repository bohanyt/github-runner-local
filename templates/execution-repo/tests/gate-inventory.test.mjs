import test from 'node:test';
import assert from 'node:assert/strict';
import { readFileSync } from 'node:fs';
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
