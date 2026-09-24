import { decodedInput, failAction, input, optionalInput, output } from '../../../lib/action-io.mjs';
import { GitHubApi } from '../../../lib/github-api.mjs';
import { decoded } from '../../../lib/protocol.mjs';
import { publishReport } from '../../../lib/reporting.mjs';

try {
  const identity = decodedInput('identity-b64');
  const resultText = optionalInput('result-b64');
  const refusalText = optionalInput('outcome-b64');
  if (!resultText && !refusalText) {
    output('reporting_complete', 'false');
    output('execution_status', 'NO_EXECUTION');
  } else {
    const api = new GitHubApi(input('token'), identity.execution_repo);
    const report = await publishReport({
      identity,
      result: resultText ? decoded(resultText) : null,
      refusal: refusalText ? decoded(refusalText) : null,
      api,
      issueNumber: Number(input('mailbox-issue'))
    });
    output('reporting_complete', String(report.reportingComplete));
    output('execution_status', report.executionStatus);
  }
} catch (error) { failAction(error); }
