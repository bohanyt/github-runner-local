import { readFileSync, readdirSync } from 'node:fs';
import { dirname, resolve } from 'node:path';
import { fileURLToPath } from 'node:url';
import { admitRequest, reconcileRecent } from '../../../lib/admission.mjs';
import { eventFromEnvironment, failAction, input, output } from '../../../lib/action-io.mjs';
import { GitHubApi } from '../../../lib/github-api.mjs';
import { ProtocolError, encoded, validateProfile } from '../../../lib/protocol.mjs';
import { systemAdapter } from '../../../lib/system-adapter.mjs';

try {
  const repository = process.env.GITHUB_REPOSITORY;
  const root = resolve(dirname(fileURLToPath(import.meta.url)), '../../..');
  const profilesRoot = resolve(root, 'profiles');
  const policies = {};
  for (const file of readdirSync(profilesRoot).filter(x => x.endsWith('.json'))) {
    const { profile, definitionSha } = validateProfile(readFileSync(resolve(profilesRoot, file)));
    if (file !== profile.id + '.json') throw new ProtocolError('PROFILE_FILENAME_MISMATCH');
    policies[profile.id] = {
      definitionSha, maxTimeoutMinutes: profile.max_timeout_minutes,
      minTests: profile.min_tests
    };
  }
  const allowedActors = JSON.parse(input('authorized-actors'));
  const allowedBranches = JSON.parse(input('allowed-branches'));
  const mailboxIssue = Number(input('mailbox-issue'));
  const reserveGiB = Number(input('reserve-gib'));
  if (!Array.isArray(allowedActors) || !allowedActors.every(x => typeof x === 'string') ||
      !Array.isArray(allowedBranches) || !allowedBranches.every(x => /^[a-zA-Z0-9._/-]+$/.test(x)) ||
      !Number.isSafeInteger(mailboxIssue) || mailboxIssue < 1 ||
      !Number.isSafeInteger(reserveGiB) || reserveGiB < 0)
    throw new ProtocolError('INVALID_POLICY_VARIABLE');
  const api = new GitHubApi(input('token'), repository);
  const context = {
    repository, mailboxIssue, allowedActors, allowedBranches, reserveGiB,
    runId: Number(process.env.GITHUB_RUN_ID),
    attempt: Number(process.env.GITHUB_RUN_ATTEMPT),
    workflowSha: process.env.GITHUB_SHA,
    triggeringActor: process.env.GITHUB_TRIGGERING_ACTOR,
    now: Date.now()
  };
  const result = await admitRequest({
    event: eventFromEnvironment(), context, api,
    system: systemAdapter(process.cwd()), profilePolicies: policies,
    loadProfileBytes: async id => readFileSync(resolve(profilesRoot, id + '.json'))
  });
  output('admitted', String(result.admitted));
  output('identity_b64', result.admitted ? encoded(result.identity) : '');
  output('target_sha', result.admitted ? result.identity.target.requested_sha : '');
  output('timeout_minutes', result.admitted ? String(result.identity.timeout_minutes) : '');
  await reconcileRecent({
    api, mailboxIssue,
    since: new Date(context.now - 86_400_000).toISOString(),
    limit: 20
  });
} catch (error) { failAction(error); }
