import { readFileSync, readdirSync } from 'node:fs';
import { execFileSync } from 'node:child_process';
import { dirname, resolve } from 'node:path';
import { fileURLToPath } from 'node:url';
import { validateProfile } from '../lib/protocol.mjs';
import { schemaDrift } from './schema-check.mjs';

export function lintSource({ workflow, profiles, runAction, reportLibrary, changedPaths = [], drift = [] }) {
  const errors = [];
  const requireText = (condition, code) => { if (!condition) errors.push(code); };
  requireText(/^on:\s*\r?\n\s+issue_comment:\s*\r?\n\s+types:\s*\[created\]/m.test(workflow), 'TRIGGER');
  requireText(/^permissions:\s*\{\s*\}$/m.test(workflow), 'TOP_PERMISSIONS');
  const admit = /\n  admit:\s*\r?\n([\s\S]*?)(?=\n  execute:)/.exec(workflow)?.[1] ?? '';
  for (const fragment of [
    'github.event.issue.number == fromJSON(vars.GRL_MAILBOX_ISSUE)',
    'github.event.issue.pull_request == null',
    'contains(fromJSON(vars.GRL_AUTHORIZED_ACTORS), github.event.comment.user.login)',
    "github.event.comment.author_association == 'OWNER'",
    "startsWith(github.event.comment.body, '<!-- grl-request v1 -->')"
  ]) requireText(admit.includes(fragment), 'PREFILTER_' + fragment.slice(0, 18));
  requireText(admit.includes('if: >-') && admit.indexOf('if: >-') < admit.indexOf('runs-on:'),
    'JOB_LEVEL_PREFILTER');
  const jobs = ['admit', 'execute', 'report', 'verdict'];
  for (const job of jobs) {
    const section = new RegExp(`\\n  ${job}:\\s*\\r?\\n([\\s\\S]*?)(?=\\n  (?:admit|execute|report|verdict):|$)`)
      .exec(workflow)?.[1] ?? '';
    requireText(section.includes('runs-on: [self-hosted, Windows, X64, grl-exec]'), 'RUNNER_' + job);
    requireText(/\n    permissions:\s*\r?\n/.test(section), 'JOB_PERMISSIONS_' + job);
  }
  requireText(!/ubuntu-|macos-|windows-latest|windows-2022|windows-2025/i.test(workflow), 'HOSTED_RUNNER');
  for (const match of workflow.matchAll(/\buses:\s*([^\s]+)/g)) {
    const ref = match[1];
    if (ref.startsWith('./')) continue;
    requireText(/^actions\/[a-z0-9_-]+@[0-9a-f]{40}$/.test(ref), 'ACTION_PIN');
  }
  const checkoutBlocks = workflow.split(/(?=^\s+- uses: )/m)
    .filter(x => /uses: actions\/checkout@/.test(x));
  requireText(checkoutBlocks.length >= 4, 'CHECKOUT_COUNT');
  for (const block of checkoutBlocks)
    requireText(/persist-credentials:\s*false/.test(block), 'CHECKOUT_CREDENTIALS');
  requireText(!/\brun:\s*(?:\||>|.*\$\{\{)/.test(workflow), 'SHELL_RUN');
  requireText(!/\brepository:\s*\$\{\{|installation.token|private.key|APP_PRIVATE_KEY|STAGE.?2/i.test(workflow),
    'STAGE2_CREDENTIAL');
  for (const match of workflow.matchAll(/retention-days:\s*(\d+)/g))
    requireText(Number(match[1]) <= 3, 'ARTIFACT_RETENTION');
  requireText(/report:\s*\r?\n\s+needs:\s*\[admit, execute\]/.test(workflow) &&
    /report:\s*\r?\n[\s\S]*?if:\s*always\(\)\s*&&/.test(workflow), 'REPORT_DEPENDENCY');
  requireText(/verdict:\s*\r?\n\s+needs:\s*\[admit, execute, report\]/.test(workflow) &&
    /verdict:\s*\r?\n[\s\S]*?if:\s*always\(\)\s*&&/.test(workflow), 'VERDICT_DEPENDENCY');
  requireText(workflow.includes('outcome-b64: ${{ needs.execute.outputs.outcome_b64 }}') &&
    workflow.includes('result-b64: ${{ needs.execute.outputs.result_b64 }}'), 'REFUSAL_ROUTE');
  const refusalBranch = /if \(execution.kind === 'refusal'\) \{([\s\S]*?)\} else \{/.exec(runAction)?.[1] ?? '';
  requireText(refusalBranch.includes("output('result_b64', '')") &&
    !refusalBranch.includes('result.json') && !refusalBranch.includes('writeFileSync'),
    'REFUSAL_CANONICAL_RESULT');
  requireText(reportLibrary.includes('REFUSAL_MARKER') && reportLibrary.includes('makeRefusal'),
    'REFUSAL_REPORT');
  for (const bytes of profiles) {
    try { validateProfile(bytes); } catch { errors.push('PROFILE'); }
  }
  for (const path of changedPaths)
    requireText(path.replaceAll('\\', '/').startsWith('templates/execution-repo/'), 'WRITE_SCOPE');
  for (const name of drift) errors.push('SCHEMA_DRIFT_' + name);
  return [...new Set(errors)];
}

export function changedPaths(repositoryRoot) {
  const git = args => execFileSync('git', args, { cwd: repositoryRoot, encoding: 'utf8' })
    .split(/\r?\n/).filter(Boolean);
  const paths = new Set([
    ...git(['diff', '--name-only', 'origin/main...HEAD']),
    ...git(['diff', '--name-only']),
    ...git(['ls-files', '--others', '--exclude-standard'])
  ]);
  return [...paths];
}

export function lintTemplate(templateRoot) {
  const workflow = readFileSync(resolve(templateRoot, '.github/workflows/grl-dispatch.yml'), 'utf8');
  const profiles = readdirSync(resolve(templateRoot, 'profiles')).filter(x => x.endsWith('.json'))
    .map(x => readFileSync(resolve(templateRoot, 'profiles', x)));
  return lintSource({
    workflow, profiles,
    runAction: readFileSync(resolve(templateRoot, '.github/actions/grl-run-profile/main.mjs'), 'utf8'),
    reportLibrary: readFileSync(resolve(templateRoot, 'lib/reporting.mjs'), 'utf8'),
    changedPaths: changedPaths(resolve(templateRoot, '../..')),
    drift: schemaDrift(templateRoot)
  });
}

if (process.argv[1] && resolve(process.argv[1]) === fileURLToPath(import.meta.url)) {
  const root = resolve(dirname(fileURLToPath(import.meta.url)), '..');
  const errors = lintTemplate(root);
  if (errors.length) {
    process.stderr.write('Template lint failed: ' + errors.join(', ') + '\n');
    process.exitCode = 1;
  } else process.stdout.write('Template lint: PASS\n');
}
