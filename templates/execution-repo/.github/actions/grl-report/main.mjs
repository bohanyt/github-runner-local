import { existsSync, readFileSync } from 'node:fs';
import { resolve } from 'node:path';
import { decodedInput, failAction, input, optionalInput, output } from '../../../lib/action-io.mjs';
import { GitHubApi } from '../../../lib/github-api.mjs';
import { decoded } from '../../../lib/protocol.mjs';
import { publishReport } from '../../../lib/reporting.mjs';

try {
  const outcomeDir = resolve(optionalInput('outcome-dir') || 'grl-outcome');
  const artifact = name => {
    const path = resolve(outcomeDir, name);
    if (!existsSync(path)) return null;
    const bytes = readFileSync(path);
    if (bytes.length > 32 * 1024) throw Error('OUTCOME_TOO_LARGE');
    return JSON.parse(bytes.toString('utf8'));
  };
  const identity = optionalInput('identity-b64') ? decodedInput('identity-b64') : artifact('identity.json');
  if (!identity) throw Error('MISSING_IDENTITY');
  const resultText = optionalInput('result-b64');
  const refusalText = optionalInput('outcome-b64');
  const result = resultText ? decoded(resultText) : artifact('result.json');
  const refusal = refusalText ? decoded(refusalText) : artifact('outcome.json');
  if (!result && !refusal) {
    output('reporting_complete', 'false');
    output('execution_status', 'NO_EXECUTION');
  } else {
    const api = new GitHubApi(input('token'), identity.execution_repo);
    const report = await publishReport({
      identity,
      result, refusal,
      api,
      issueNumber: Number(input('mailbox-issue'))
    });
    output('reporting_complete', String(report.reportingComplete));
    output('execution_status', report.executionStatus);
  }
} catch (error) { failAction(error); }
