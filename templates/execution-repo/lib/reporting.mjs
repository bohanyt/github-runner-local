import { REFUSAL_MARKER, RESULT_MARKER, REPOSITORY, SHA, SHA256, UUID4, exactKeys,
  fenced, parseMarkedComment, redact, reject, utcMillis } from './protocol.mjs';

function sanitizePublic(value) {
  if (typeof value === 'string') return redact(value);
  if (Array.isArray(value)) return value.map(sanitizePublic);
  if (value && typeof value === 'object')
    return Object.fromEntries(Object.entries(value).map(([key, child]) => [key, sanitizePublic(child)]));
  return value;
}

export function validateCanonicalResult(result, identity) {
  const nonnegative = value => Number.isFinite(value) && value >= 0;
  const count = value => Number.isSafeInteger(value) && value >= 0;
  const nonempty = value => typeof value === 'string' && value.length > 0;
  exactKeys(result, ['schema_version', 'request_id', 'request_comment_id',
    'request_body_sha256', 'execution_repo', 'run', 'workflow_sha', 'profile',
    'target', 'runner', 'timing', 'checks', 'execution_status', 'disk',
    'artifacts', 'logs', 'report']);
  exactKeys(result.run, ['id', 'attempt', 'url']);
  exactKeys(result.profile, ['id', 'definition_sha']);
  exactKeys(result.target, ['repository', 'requested_sha', 'tested_sha',
    'containing_branch', 'branch_head_at_admission']);
  exactKeys(result.runner, ['name', 'version', 'os', 'arch', 'identity_class', 'elevated']);
  exactKeys(result.timing, ['admitted_at', 'started_at', 'finished_at', 'phases']);
  exactKeys(result.disk, ['free_before_gib', 'free_after_gib', 'reserve_gib']);
  exactKeys(result.logs, ['url']);
  exactKeys(result.report, ['attempts', 'status_posted', 'comment_posted']);
  if (result.schema_version !== 'grl.result.v1' ||
      result.request_id !== identity.request_id ||
      result.request_comment_id !== identity.request_comment_id ||
      result.request_body_sha256 !== identity.request_body_sha256 ||
      result.execution_repo !== identity.execution_repo ||
      result.run?.id !== identity.run.id || result.run?.attempt !== identity.run.attempt ||
      result.run?.url !== identity.run.url ||
      result.workflow_sha !== identity.workflow_sha ||
      result.profile?.id !== identity.profile.id ||
      result.profile?.definition_sha !== identity.profile.definition_sha ||
      result.target.repository !== identity.target.repository ||
      result.target.requested_sha !== identity.target.requested_sha ||
      result.target.tested_sha !== identity.target.requested_sha ||
      result.target.containing_branch !== identity.target.containing_branch ||
      result.target.branch_head_at_admission !== identity.target.branch_head_at_admission ||
      !SHA.test(result.target.tested_sha) ||
      !['PASS', 'FAIL', 'BLOCKED', 'CANCELLED', 'TIMED_OUT', 'EXPIRED', 'REJECTED', 'INTERRUPTED']
        .includes(result.execution_status))
    reject('INVALID_CANONICAL_RESULT');
  if (!UUID4.test(result.request_id) || !count(result.request_comment_id) ||
      result.request_comment_id < 1 || !SHA256.test(result.request_body_sha256) ||
      !REPOSITORY.test(result.execution_repo) || !REPOSITORY.test(result.target.repository) ||
      !SHA.test(result.workflow_sha) || !SHA.test(result.profile.definition_sha) ||
      !SHA.test(result.target.requested_sha) || !SHA.test(result.target.branch_head_at_admission) ||
      !count(result.run.id) || result.run.id < 1 || !count(result.run.attempt) ||
      result.run.attempt < 1 || result.run.url !==
        `https://github.com/${result.execution_repo}/actions/runs/${result.run.id}` ||
      !nonempty(result.target.containing_branch) ||
      result.target.containing_branch.includes('..') || /\s/.test(result.target.containing_branch) ||
      result.target.containing_branch.startsWith('/') ||
      result.target.containing_branch.endsWith('/') ||
      !nonempty(result.runner.name) || !nonempty(result.runner.version) ||
      result.runner.os !== 'Windows' || result.runner.arch !== 'X64' ||
      !['portable-user', 'service-account'].includes(result.runner.identity_class) ||
      result.runner.elevated !== false ||
      !Array.isArray(result.checks) || !Array.isArray(result.artifacts) ||
      result.logs.url !== identity.run.url ||
      !nonnegative(result.disk.free_before_gib) ||
      !nonnegative(result.disk.free_after_gib) || !count(result.disk.reserve_gib) ||
      !count(result.report.attempts) || result.report.attempts < 1 ||
      typeof result.report.status_posted !== 'boolean' ||
      typeof result.report.comment_posted !== 'boolean') reject('INVALID_CANONICAL_RESULT');
  const admitted = utcMillis(result.timing.admitted_at);
  const started = utcMillis(result.timing.started_at);
  const finished = utcMillis(result.timing.finished_at);
  if (admitted > started || started > finished || !Array.isArray(result.timing.phases))
    reject('INVALID_TIMING');
  for (const phase of result.timing.phases) {
    exactKeys(phase, ['name', 'duration_s']);
    if (!nonempty(phase.name) || !nonnegative(phase.duration_s)) reject('INVALID_PHASE');
  }
  for (const check of result.checks) {
    exactKeys(check, ['name', 'exit_code', 'duration_s', 'tests']);
    exactKeys(check.tests, ['passed', 'failed', 'skipped', 'errored', 'source']);
    if (!nonempty(check.name) || !Number.isSafeInteger(check.exit_code) ||
        !nonnegative(check.duration_s) || !nonempty(check.tests.source) ||
        !['passed', 'failed', 'skipped', 'errored'].every(key => count(check.tests[key])))
      reject('INVALID_CHECK');
  }
  for (const artifact of result.artifacts) {
    exactKeys(artifact, ['name', 'url']);
    if (!nonempty(artifact.name) || !/^https:\/\//.test(artifact.url)) reject('INVALID_ARTIFACT');
  }
  const passed = result.checks.reduce((sum, x) => sum + x.tests.passed, 0);
  if (result.execution_status === 'PASS' &&
      (!count(identity.min_tests) || identity.min_tests < 1 ||
       passed < identity.min_tests || result.checks.length < 1 ||
       result.checks.some(x => x.exit_code !== 0 || x.tests.failed !== 0 || x.tests.errored !== 0)))
    reject('INVALID_PASS');
  if (Buffer.byteLength(JSON.stringify(result), 'utf8') > 32 * 1024) reject('RESULT_TOO_LARGE');
  return result;
}

export function makeRefusal(identity, outcome, report) {
  if (outcome?.status !== 'BLOCKED' ||
      outcome.reason_code !== 'CHECKOUT_SHA_MISMATCH' ||
      outcome.profile_executed !== false ||
      outcome.canonical_result_present !== false ||
      outcome.requested_sha !== identity.target.requested_sha ||
      !SHA.test(outcome.observed_checkout_sha) ||
      outcome.observed_checkout_sha === outcome.requested_sha)
    reject('INVALID_REFUSAL');
  return {
    schema_version: 'grl.exec-refusal.v1',
    request_id: identity.request_id,
    request_comment_id: identity.request_comment_id,
    request_body_sha256: identity.request_body_sha256,
    execution_repo: identity.execution_repo,
    run: identity.run,
    workflow_sha: identity.workflow_sha,
    profile: identity.profile,
    target: {
      repository: identity.target.repository,
      requested_sha: identity.target.requested_sha,
      observed_checkout_sha: outcome.observed_checkout_sha,
      containing_branch: identity.target.containing_branch,
      branch_head_at_admission: identity.target.branch_head_at_admission
    },
    reason_code: 'CHECKOUT_SHA_MISMATCH',
    profile_executed: false,
    report
  };
}

export async function retryPublication(operation, wait = ms => new Promise(resolve => setTimeout(resolve, ms))) {
  for (let attempt = 1; attempt <= 3; attempt++) {
    try { await operation(attempt); return { posted: true, attempts: attempt }; }
    catch {
      if (attempt < 3) await wait(250 * 2 ** (attempt - 1));
    }
  }
  return { posted: false, attempts: 3 };
}

export async function publishReport({
  identity, result = null, refusal = null, api, issueNumber, wait
}) {
  if (Boolean(result) === Boolean(refusal)) reject('INVALID_REPORT_INPUT');
  if (result) validateCanonicalResult(result, identity);
  if (redact(JSON.stringify(identity)) !== JSON.stringify(identity)) reject('SECRET_IDENTITY');
  if (refusal && (refusal.requested_sha !== identity.target.requested_sha ||
      refusal.reason_code !== 'CHECKOUT_SHA_MISMATCH')) reject('INVALID_REFUSAL');
  const context = `grl/${identity.profile.id}`;
  const state = result?.execution_status === 'PASS' ? 'success' : 'failure';
  const status = await retryPublication(() => api.createStatus(identity.target.requested_sha, {
    state, context, target_url: identity.run.url,
    description: refusal ? 'BLOCKED: checkout SHA mismatch' :
      `GRL execution ${result.execution_status}`
  }), wait);
  let comment;
  let attempts = 0;
  const commentResult = await retryPublication(async attempt => {
    attempts = attempt;
    if (attempt > 1 && typeof api.listComments === 'function') {
      const prior = await api.listComments(issueNumber, identity.admitted_at, 200);
      const marker = result ? RESULT_MARKER : REFUSAL_MARKER;
      const existing = prior.find(entry => {
        const payload = parseMarkedComment(entry.body, marker);
        return entry.user?.login === 'github-actions[bot]' &&
          payload?.request_id === identity.request_id &&
          payload?.request_body_sha256 === identity.request_body_sha256 &&
          payload?.run?.id === identity.run.id &&
          payload?.run?.attempt === identity.run.attempt;
      });
      if (existing) { comment = existing.body; return; }
    }
    const report = { attempts: Math.max(status.attempts, attempt),
      status_posted: status.posted, comment_posted: true };
    const payload = result ? sanitizePublic({ ...result, report }) : makeRefusal(identity, refusal, report);
    comment = fenced(result ? RESULT_MARKER : REFUSAL_MARKER,
      payload, result ? 16 * 1024 : 8 * 1024);
    await api.postIssueComment(issueNumber, comment);
  }, wait);
  return {
    executionStatus: result?.execution_status ?? 'BLOCKED',
    statusPosted: status.posted,
    commentPosted: commentResult.posted,
    statusAttempts: status.attempts,
    commentAttempts: commentResult.attempts,
    reportingComplete: status.posted && commentResult.posted,
    marker: result ? RESULT_MARKER : REFUSAL_MARKER,
    comment: commentResult.posted ? comment : null,
    attempts
  };
}

export function evaluateVerdict({ admitted, executionStatus, reportingComplete,
  executeJobResult = 'success', reportJobResult = 'success' }) {
  return admitted === true && executionStatus === 'PASS' && reportingComplete === true &&
    executeJobResult === 'success' && reportJobResult === 'success'
    ? { pass: true, reason: 'PASS' }
    : { pass: false, reason: reportingComplete !== true ? 'REPORTING_INCOMPLETE' :
      executionStatus ?? 'NO_EXECUTION' };
}
