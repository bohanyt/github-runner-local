import { failAction, optionalInput } from '../../../lib/action-io.mjs';
import { ProtocolError } from '../../../lib/protocol.mjs';
import { evaluateVerdict } from '../../../lib/reporting.mjs';

const verdict = evaluateVerdict({
  admitted: optionalInput('admitted') === 'true',
  executionStatus: optionalInput('execution-status'),
  reportingComplete: optionalInput('reporting-complete') === 'true'
});
if (!verdict.pass) failAction(new ProtocolError(verdict.reason));
