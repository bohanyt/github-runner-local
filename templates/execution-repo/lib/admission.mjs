import { ACK_MARKER, REFUSAL_MARKER, RESULT_MARKER, ProtocolError, SHA, SHA256,
  exactKeys, fenced, parseMarkedComment, parseRequestEnvelope, reject, validateProfile } from './protocol.mjs';
import { validateCanonicalResult } from './reporting.mjs';

export { parseMarkedComment };

export function ackFor(identity, admitted, reasonCode) {
  return {
    schema_version: 'grl.ack.v1',
    request_id: identity?.request_id ?? null,
    request_comment_id: identity?.request_comment_id ?? null,
    request_body_sha256: identity?.request_body_sha256 ?? null,
    execution_repo: identity?.execution_repo ?? null,
    run: identity?.run ?? null,
    workflow_sha: identity?.workflow_sha ?? null,
    profile: identity?.profile ?? null,
    target: identity?.target ?? null,
    min_tests: identity?.min_tests ?? null,
    admitted,
    reason_code: reasonCode
  };
}

export async function admitRequest({ event, context, api, system, profilePolicies, loadProfileBytes }) {
  const run = {
    id: context.runId,
    attempt: context.attempt,
    url: `https://github.com/${context.repository}/actions/runs/${context.runId}`
  };
  let identity = null;
  let reasonCode = 'ADMITTED';
  try {
    if (event.issue?.number !== context.mailboxIssue || event.issue?.pull_request ||
        event.comment?.author_association !== 'OWNER' ||
        !context.allowedActors.includes(event.comment?.user?.login) ||
        !event.comment?.body?.startsWith('<!-- grl-request v1 -->'))
      reject('PREFILTER_REJECTED');
    const { request, bodySha256 } = parseRequestEnvelope(event.comment.body, {
      createdAt: event.comment.created_at, now: context.now,
      repository: context.repository, profilePolicies
    });
    identity = {
      request_id: request.request_id,
      request_comment_id: event.comment.id,
      request_body_sha256: bodySha256,
      execution_repo: context.repository,
      run,
      workflow_sha: context.workflowSha,
      profile: { id: request.profile.id, definition_sha: request.profile.definition_sha },
      target: {
        repository: request.target.repository,
        requested_sha: request.target.sha,
        containing_branch: null,
        branch_head_at_admission: null
      },
      admitted_at: new Date(context.now).toISOString(),
      timeout_minutes: request.timeout_minutes,
      min_tests: profilePolicies[request.profile.id].minTests,
      reserve_gib: context.reserveGiB,
      disk_before_gib: null
    };
    if (!Number.isSafeInteger(identity.request_comment_id) || identity.request_comment_id < 1 ||
        !Number.isSafeInteger(context.runId) || context.runId < 1 ||
        !Number.isSafeInteger(context.attempt) || context.attempt < 1 ||
        !SHA.test(context.workflowSha)) reject('INVALID_RUN_IDENTITY');

    const loaded = validateProfile(await loadProfileBytes(request.profile.id));
    if (loaded.profile.id !== request.profile.id ||
        loaded.definitionSha !== request.profile.definition_sha ||
        loaded.profile.max_timeout_minutes !== profilePolicies[request.profile.id].maxTimeoutMinutes ||
        loaded.profile.min_tests !== profilePolicies[request.profile.id].minTests)
      reject('PROFILE_DEFINITION_MISMATCH');

    const original = await api.getComment(event.comment.id);
    if (!original) reject('WITHDRAWN');
    if (original.body !== event.comment.body || original.id !== event.comment.id)
      reject('EDITED_REQUEST');

    let contained = false;
    for (const branch of context.allowedBranches.slice(0, 20)) {
      const head = await api.getBranch(branch);
      if (!head || !SHA.test(head.sha)) continue;
      const comparison = await api.compareShaToBranch(request.target.sha, branch);
      if (comparison === 'ahead' || comparison === 'identical') {
        identity.target.containing_branch = branch;
        identity.target.branch_head_at_admission = head.sha;
        contained = true;
        break;
      }
    }
    if (!contained) reject('SHA_NOT_CONTAINED');

    const since = new Date(Date.parse(event.comment.created_at) - 86_400_000).toISOString();
    const recent = await api.listComments(context.mailboxIssue, since, 200);
    for (const comment of recent.slice(0, 200)) {
      if (comment.user?.login !== 'github-actions[bot]') continue;
      const prior = parseMarkedComment(comment.body, ACK_MARKER);
      if (!prior || prior.request_id !== request.request_id) continue;
      if (prior.request_body_sha256 !== bodySha256) reject('REPLAY_DIGEST_MISMATCH');
      if (prior.run?.id !== context.runId) reject('DUPLICATE');
      if (!context.allowedActors.includes(context.triggeringActor)) reject('UNAUTHORIZED_RERUN');
    }
    if (!context.allowedActors.includes(context.triggeringActor)) reject('UNAUTHORIZED_TRIGGER');

    const free = await system.freeDiskGiB();
    identity.disk_before_gib = free;
    if (!Number.isFinite(free) || free < context.reserveGiB + loaded.profile.estimated_disk_gib)
      reject('LOW_DISK');
    if (await system.isElevated()) reject('ELEVATED_RUNNER');
    for (const capability of loaded.profile.required_capabilities)
      if (!await system.hasCapability(capability)) reject('MISSING_CAPABILITY');
  } catch (error) {
    if (!(error instanceof ProtocolError)) throw error;
    reasonCode = error.code;
  }
  const admitted = reasonCode === 'ADMITTED';
  const ack = ackFor(identity, admitted, reasonCode);
  await api.postIssueComment(context.mailboxIssue, fenced(ACK_MARKER, ack, 4096));
  return { admitted, reasonCode, identity, ack };
}

export function validateRefusal(refusal, ack, status) {
  try {
    exactKeys(refusal, ['schema_version', 'request_id', 'request_comment_id',
      'request_body_sha256', 'execution_repo', 'run', 'workflow_sha', 'profile',
      'target', 'reason_code', 'profile_executed', 'report']);
    exactKeys(refusal.run, ['id', 'attempt', 'url']);
    exactKeys(refusal.profile, ['id', 'definition_sha']);
    exactKeys(refusal.target, ['repository', 'requested_sha', 'observed_checkout_sha',
      'containing_branch', 'branch_head_at_admission']);
    exactKeys(refusal.report, ['attempts', 'status_posted', 'comment_posted']);
    if (refusal.schema_version !== 'grl.exec-refusal.v1' ||
        refusal.reason_code !== 'CHECKOUT_SHA_MISMATCH' ||
        refusal.profile_executed !== false ||
        !SHA.test(refusal.target.requested_sha) ||
        !SHA.test(refusal.target.observed_checkout_sha) ||
        refusal.target.requested_sha === refusal.target.observed_checkout_sha ||
        !SHA256.test(refusal.request_body_sha256) ||
        !Number.isInteger(refusal.report.attempts) ||
        refusal.report.attempts < 1 || refusal.report.attempts > 3 ||
        refusal.report.status_posted !== true ||
        refusal.report.comment_posted !== true) return false;
    for (const key of ['request_id', 'request_comment_id', 'request_body_sha256',
      'execution_repo', 'workflow_sha'])
      if (refusal[key] !== ack[key]) return false;
    for (const key of ['id', 'attempt', 'url'])
      if (refusal.run[key] !== ack.run?.[key]) return false;
    for (const key of ['id', 'definition_sha'])
      if (refusal.profile[key] !== ack.profile?.[key]) return false;
    for (const key of ['repository', 'requested_sha', 'containing_branch', 'branch_head_at_admission'])
      if (refusal.target[key] !== ack.target?.[key]) return false;
    return status?.state === 'failure' &&
      status.context === `grl/${refusal.profile.id}` &&
      status.sha === refusal.target.requested_sha &&
      status.target_url === refusal.run.url;
  } catch { return false; }
}

export function observeTerminal({ ack, run, comments, status, refusalExpected = false }) {
  if (!run?.concluded) return { outcome: 'ACTIVE', reportingComplete: false };
  const botComments = comments.filter(x => x.user?.login === 'github-actions[bot]');
  const refusals = botComments.map(x => parseMarkedComment(x.body, REFUSAL_MARKER)).filter(Boolean);
  const matchingRefusal = refusals.find(x => x.request_id === ack.request_id && x.run?.id === ack.run?.id);
  if (matchingRefusal) {
    if (validateRefusal(matchingRefusal, ack, status))
      return { outcome: 'BLOCKED', reportingComplete: true };
    return { outcome: 'REPORTING_INCOMPLETE', reportingComplete: false };
  }
  if (refusalExpected)
    return { outcome: 'REPORTING_INCOMPLETE', reportingComplete: false };
  const results = botComments.map(x => parseMarkedComment(x.body, RESULT_MARKER)).filter(Boolean);
  const result = results.find(x => x.request_id === ack.request_id && x.run?.id === ack.run?.id);
  if (result) {
    try { validateCanonicalResult(result, ack); }
    catch { return { outcome: 'REPORTING_INCOMPLETE', reportingComplete: false }; }
    const complete = result.request_comment_id === ack.request_comment_id &&
      result.request_body_sha256 === ack.request_body_sha256 &&
      result.execution_repo === ack.execution_repo &&
      result.run?.attempt === ack.run?.attempt && result.run?.url === ack.run?.url &&
      result.workflow_sha === ack.workflow_sha &&
      result.profile?.id === ack.profile?.id &&
      result.profile?.definition_sha === ack.profile?.definition_sha &&
      result.target?.repository === ack.target?.repository &&
      result.target?.requested_sha === ack.target?.requested_sha &&
      result.target?.tested_sha === ack.target?.requested_sha &&
      result.report?.status_posted === true && result.report?.comment_posted === true &&
      status?.sha === ack.target?.requested_sha &&
      status?.context === `grl/${ack.profile?.id}` &&
      status?.target_url === ack.run?.url &&
      status?.state === (result.execution_status === 'PASS' ? 'success' : 'failure');
    return complete ? { outcome: result.execution_status, reportingComplete: true } :
      { outcome: 'REPORTING_INCOMPLETE', reportingComplete: false };
  }
  return { outcome: status ? 'REPORTING_INCOMPLETE' :
    run.conclusion === 'cancelled' || run.conclusion === 'timed_out'
    ? 'INTERRUPTED' : 'REPORTING_INCOMPLETE', reportingComplete: false };
}

export async function reconcileRecent({ api, mailboxIssue, since, limit = 20 }) {
  const comments = (await api.listComments(mailboxIssue, since, 200)).slice(0, 200);
  const acks = comments.filter(x => x.user?.login === 'github-actions[bot]')
    .map(x => parseMarkedComment(x.body, ACK_MARKER))
    .filter(x => x?.admitted === true).slice(0, limit);
  const notes = [];
  for (const ack of acks) {
    const run = await api.getRun(ack.run.id, ack.run.attempt);
    const artifacts = run?.concluded ? await api.getRunArtifacts(ack.run.id) : [];
    const refusalExpected = artifacts.includes(`grl-exec-refusal-${ack.run.id}-${ack.run.attempt}`);
    const status = await api.getCommitStatus(ack.target.requested_sha, `grl/${ack.profile.id}`);
    const state = observeTerminal({ ack, run, comments, status, refusalExpected });
    if (state.outcome === 'ACTIVE' || state.reportingComplete) continue;
    if (comments.some(x => {
      const note = parseMarkedComment(x.body, '<!-- grl-reconcile v1 -->');
      return note?.request_id === ack.request_id && note?.run_id === ack.run.id &&
        note?.run_attempt === ack.run.attempt;
    })) continue;
    const note = { schema_version: 'grl.reconcile.v1', request_id: ack.request_id,
      run_id: ack.run.id, run_attempt: ack.run.attempt, outcome: state.outcome };
    await api.postIssueComment(mailboxIssue, fenced('<!-- grl-reconcile v1 -->', note, 2048));
    notes.push(note);
  }
  return notes;
}
