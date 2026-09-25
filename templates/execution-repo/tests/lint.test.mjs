import test from 'node:test';
import assert from 'node:assert/strict';
import { readFileSync } from 'node:fs';
import { resolve } from 'node:path';
import { fileURLToPath } from 'node:url';
import { lintSource, lintTemplate } from '../tools/lint.mjs';

const root = resolve(fileURLToPath(new URL('..', import.meta.url)));
const runtimeSources = [
  readFileSync(resolve(root, '.github/actions/grl-admit/main.mjs'), 'utf8'),
  readFileSync(resolve(root, '.github/actions/grl-report/main.mjs'), 'utf8'),
  readFileSync(resolve(root, '.github/actions/grl-run-profile/main.mjs'), 'utf8'),
  readFileSync(resolve(root, '.github/actions/grl-verdict/main.mjs'), 'utf8'),
  ...['action-io.mjs', 'admission.mjs', 'execution.mjs', 'github-api.mjs',
    'protocol.mjs', 'reporting.mjs', 'system-adapter.mjs']
    .map(name => readFileSync(resolve(root, 'lib', name), 'utf8'))
];
const source = {
  workflow: readFileSync(resolve(root, '.github/workflows/grl-dispatch.yml'), 'utf8').replaceAll('\r\n', '\n'),
  profiles: [readFileSync(resolve(root, 'profiles/js-smoke.json'))],
  runAction: readFileSync(resolve(root, '.github/actions/grl-run-profile/main.mjs'), 'utf8'),
  reportLibrary: readFileSync(resolve(root, 'lib/reporting.mjs'), 'utf8'),
  verdictAction: readFileSync(resolve(root, '.github/actions/grl-verdict/main.mjs'), 'utf8'),
  runtimeSources,
  changedPaths: ['templates/execution-repo/README.md'], drift: []
};
const mutate = (field, before, after) => ({ ...source,
  [field]: typeof source[field] === 'string' ? source[field].replace(before, after) : after });

test('unaltered template passes lint and schema mirror check', () => {
  assert.deepEqual(lintSource(source), []);
  assert.deepEqual(lintTemplate(root), []);
});

test('linter rejects workflow trigger, permission, prefilter and runner regressions', () => {
  const cases = [
    [mutate('workflow', 'types: [created]', 'types: [edited]'), 'TRIGGER'],
    [mutate('workflow', 'types: [created]', 'types: [created]\n  push:'), 'TRIGGER'],
    [mutate('workflow', 'permissions: {}', 'permissions: write-all'), 'TOP_PERMISSIONS'],
    [mutate('workflow', "github.event.comment.author_association == 'OWNER'", 'true'), 'PREFILTER_'],
    [mutate('workflow', 'github.event.issue.pull_request == null', 'true'), 'PREFILTER_'],
    [mutate('workflow', 'runs-on: [self-hosted, Windows, X64, grl-exec]',
      'runs-on: ubuntu-latest'), 'RUNNER_'],
    [mutate('workflow', 'if: >-', 'if: true'), 'JOB_LEVEL_PREFILTER'],
    [mutate('workflow', "startsWith(github.event.comment.body, '<!-- grl-request v1 -->')",
      "startsWith(github.event.comment.body, '<!-- grl-request v1 -->') || true"), 'WEAK_PREFILTER']
  ];
  for (const [candidate, code] of cases)
    assert.ok(lintSource(candidate).some(x => x.startsWith(code)), code);
});

test('linter rejects action, checkout, shell, Stage-2 and retention regressions', () => {
  const cases = [
    [mutate('workflow', 'actions/checkout@11bd71901bbe5b1630ceea73d27597364c9af683',
      'actions/checkout@v4'), 'ACTION_PIN'],
    [mutate('workflow', 'actions/checkout@11bd71901bbe5b1630ceea73d27597364c9af683',
      'vendor/checkout@11bd71901bbe5b1630ceea73d27597364c9af683'), 'ACTION_PIN'],
    [mutate('workflow', 'persist-credentials: false', 'persist-credentials: true'),
      'CHECKOUT_CREDENTIALS'],
    [mutate('workflow', '    steps:\n      - uses:',
      '    steps:\n      - name: shell mutation\n        run: echo ${{ github.event.comment.body }}\n      - uses:'), 'SHELL_RUN'],
    [mutate('workflow', '          path: grl-target',
      '          repository: owner/other\n          path: grl-target'), 'STAGE2_CREDENTIAL'],
    [mutate('workflow', '          path: grl-target',
      '          repository: ${{ vars.STAGE2_REPOSITORY }}\n          path: grl-target'), 'STAGE2_CREDENTIAL'],
    [{ ...source, runtimeSources: [...source.runtimeSources,
      "const stage2Token = process.env.GRL_STAGE2_TOKEN;\n"] }, 'STAGE2_CREDENTIAL'],
    [mutate('workflow', 'timeout-minutes: 10',
      'retention-days: 4\n    timeout-minutes: 10'), 'ARTIFACT_RETENTION']
  ];
  for (const [candidate, code] of cases)
    assert.ok(lintSource(candidate).includes(code), code);
});

test('linter rejects profile, dependency, schema drift, scope and E-B1 routing regressions', () => {
  const shellProfile = JSON.parse(source.profiles[0]);
  shellProfile.steps[0] = { ...shellProfile.steps[0], shell: 'pwsh -Command echo unsafe' };
  const cases = [
    [{ ...source, profiles: [Buffer.from(JSON.stringify(shellProfile))] }, 'PROFILE'],
    [mutate('workflow', 'needs: [admit, execute]', 'needs: [admit]'), 'REPORT_DEPENDENCY'],
    [mutate('workflow', 'needs: [admit, execute, report]', 'needs: [admit, execute]'),
      'VERDICT_DEPENDENCY'],
    [{ ...source, drift: ['grl.request.v1.schema.json'] }, 'SCHEMA_DRIFT_'],
    [{ ...source, changedPaths: ['src/Program.cs'] }, 'WRITE_SCOPE'],
    [mutate('workflow', 'outcome-b64: ${{ needs.execute.outputs.outcome_b64 }}',
      'outcome-b64: ${{ needs.execute.outputs.result_b64 }}'), 'REFUSAL_ROUTE'],
    [mutate('workflow', "if: steps.run-profile.outputs.execution_status == 'BLOCKED'",
      'if: false'), 'REFUSAL_MARKER_ARTIFACT'],
    [mutate('workflow', 'reporting-complete: ${{ needs.report.outputs.reporting_complete }}',
      'reporting-complete: true'), 'GREEN_ON_REPORT_FAILURE'],
    [mutate('runAction', "output('result_b64', '');",
      "writeFileSync('result.json', '{}');"), 'REFUSAL_CANONICAL_RESULT']
  ];
  for (const [candidate, code] of cases)
    assert.ok(lintSource(candidate).some(x => x.startsWith(code)), code);
});
