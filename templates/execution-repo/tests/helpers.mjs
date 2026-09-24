import { readFileSync } from 'node:fs';
import { resolve } from 'node:path';
import { fileURLToPath } from 'node:url';
import { gitBlobSha, sha256 } from '../lib/protocol.mjs';

export const root = resolve(fileURLToPath(new URL('..', import.meta.url)));
export const profileBytes = readFileSync(resolve(root, 'profiles/js-smoke.json'));
export const definitionSha = gitBlobSha(profileBytes);
export const A = 'a'.repeat(40);
export const B = 'b'.repeat(40);
export const W = 'c'.repeat(40);
export const requestId = '123e4567-e89b-42d3-a456-426614174000';
export const createdAt = '2026-09-24T12:00:00Z';
export const now = Date.parse('2026-09-24T12:01:00Z');

export function request(overrides = {}) {
  return {
    schema_version: 'grl.request.v1',
    request_id: requestId,
    target: { repository: 'owner/execution', sha: A },
    profile: { id: 'js-smoke', definition_sha: definitionSha },
    expires_at: '2026-09-24T12:10:00Z',
    timeout_minutes: 5,
    ...overrides
  };
}
export const body = payload => '<!-- grl-request v1 -->\n```json\n' +
  JSON.stringify(payload) + '\n```';
export const bodyDigest = payload => sha256(Buffer.from('```json\n' +
  JSON.stringify(payload) + '\n```'));
export const profilePolicies = () => ({
  'js-smoke': { definitionSha, maxTimeoutMinutes: 5, minTests: 1 }
});
export const event = payload => ({
  issue: { number: 7 },
  comment: { id: 77, body: body(payload), created_at: createdAt,
    user: { login: 'owner' }, author_association: 'OWNER' }
});
export const context = () => ({
  repository: 'owner/execution', mailboxIssue: 7,
  allowedActors: ['owner'], allowedBranches: ['main'], reserveGiB: 15,
  runId: 123, attempt: 1, workflowSha: W, triggeringActor: 'owner', now
});

export function fakeApi(currentEvent = event(request())) {
  const posted = [];
  const statuses = [];
  const api = {
    posted, statuses,
    original: currentEvent.comment,
    comments: [],
    comparison: 'ahead',
    branch: { sha: B },
    run: { concluded: false, conclusion: null },
    commitStatus: null,
    async getComment() { return this.original; },
    async listComments() { return this.comments; },
    async getBranch() { return this.branch; },
    async compareShaToBranch() { return this.comparison; },
    async postIssueComment(issue, text) { posted.push({ issue, body: text }); },
    async createStatus(sha, status) { statuses.push({ sha, ...status }); },
    async getRun() { return this.run; },
    async getCommitStatus() { return this.commitStatus; }
  };
  return api;
}
export function fakeSystem() {
  return {
    free: 40, elevated: false, capability: true,
    async freeDiskGiB() { return this.free; },
    async isElevated() { return this.elevated; },
    async hasCapability() { return this.capability; }
  };
}
export function identity() {
  return {
    request_id: requestId, request_comment_id: 77,
    request_body_sha256: bodyDigest(request()),
    execution_repo: 'owner/execution',
    run: { id: 123, attempt: 1, url: 'https://github.com/owner/execution/actions/runs/123' },
    workflow_sha: W,
    profile: { id: 'js-smoke', definition_sha: definitionSha },
    target: { repository: 'owner/execution', requested_sha: A,
      containing_branch: 'main', branch_head_at_admission: B },
    admitted_at: '2026-09-24T12:01:00.000Z',
    timeout_minutes: 5, min_tests: 1, reserve_gib: 15, disk_before_gib: 40
  };
}
export function canonicalResult(executionStatus = 'PASS') {
  const id = identity();
  return {
    schema_version: 'grl.result.v1',
    request_id: id.request_id, request_comment_id: id.request_comment_id,
    request_body_sha256: id.request_body_sha256, execution_repo: id.execution_repo,
    run: id.run, workflow_sha: id.workflow_sha, profile: id.profile,
    target: { ...id.target, tested_sha: A },
    runner: { name: 'runner', version: '2.337.0', os: 'Windows', arch: 'X64',
      identity_class: 'portable-user', elevated: false },
    timing: { admitted_at: id.admitted_at, started_at: id.admitted_at,
      finished_at: id.admitted_at, phases: [{ name: 'execute', duration_s: 1 }] },
    checks: [{ name: 'js-smoke', exit_code: executionStatus === 'PASS' ? 0 : 1,
      duration_s: 1, tests: { passed: executionStatus === 'PASS' ? 1 : 0,
        failed: executionStatus === 'PASS' ? 0 : 1, skipped: 0, errored: 0, source: 'junit' } }],
    execution_status: executionStatus,
    disk: { free_before_gib: 40, free_after_gib: 39, reserve_gib: 15 },
    artifacts: [], logs: { url: id.run.url },
    report: { attempts: 1, status_posted: false, comment_posted: false }
  };
}
