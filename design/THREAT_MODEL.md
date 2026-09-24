# Threat model proposal

Status: PROPOSED. This is not a completed security audit. See R3-R10 in main `docs/REFERENCES.md`.

## Assets and trust boundaries

Protect GitHub credentials, local runner identity, company/user files, reachable internal services, privileged setup code, execution profiles, source provenance and result integrity. Treat comments, branches, checkout contents, dependency scripts, artifacts and logs as data from potentially compromised sources.

Public installer code/release -> local UI is a software-supply-chain boundary. UI -> elevated helper is a privilege boundary. GitHub request -> workflow is an authorization boundary. Workflow -> checked-out project is code execution, not harmless data ingestion. Local job -> reporter is an evidence and secret boundary.

## Required mitigations and tests

| Risk | Proposed boundary | Required negative proof |
|---|---|---|
| Public contributor dispatches work | No public execution mailbox; authenticated actor and target allowlist | Public/fork/bot impostor cannot start a job. |
| Authorized requester supplies hostile code | Known provenance, minimal credentials, isolated job identity/environment | Explain residual same-user and network exposure; do not advertise sandboxing. |
| Comment injection | Parse strict data; fixed profiles; no string-to-shell interpolation | Shell metacharacters, paths, unknown fields and malformed SHAs are rejected. |
| Replay / changed HEAD | Immutable SHA, request ID, expiry, body digest and idempotency record | Repeated or edited requests cannot substitute a new checkout or duplicate side effects. |
| Forged PASS comment | Match publisher identity, run/attempt/target and actual workflow evidence | A copied result comment or wrong run does not count as proof. |
| Credential theft | Setup/job identity separation, least access, no plaintext fallback | Target job cannot read setup credentials under the chosen deployment; otherwise mode must be disclosed as trusted-only. |
| Privileged binary replacement | Protected binary/config paths and ancestors; validated helper operations | A job cannot replace the helper or service executable; path/reparse tricks fail. |
| Arbitrary admin helper action | Narrow operation enum, authenticated IPC, ownership/nonce/ACL checks | Caller cannot pass an arbitrary EXE, executable path or registry command. |
| Partial setup | Transaction journal without secrets, compensating cleanup, explicit pending state | Cancel/crash at every stage leaves no hidden service or claimed success. |
| Policy restriction | Inspect actual execution results; fail closed | GUI/UAC success cannot mask a blocked shell or child process. |
| Excess disk or runaway process | One slot, quotas/estimates, timeout/process-tree cleanup | No new job below reserve; cancellation has a bounded observable outcome. |
| Secret output / cross-repo leak | Redaction + bounded reports + private destination | Tokens and private outputs never appear in the public product repo. |
| Malicious update/archive | Authenticated source, pinned version/digest, safe extraction, rollback | Mismatched checksum, traversal, reparse points and unexpected binaries are rejected. |

## Important non-solutions

A `.gitignore` is not a security boundary. A fixed script can execute malicious target code/dependency hooks. A private repo can have untrusted readers/contributors. A low-privilege Windows user can still read its own profile and access the network. DPAPI/user credential storage does not defend against that same user's arbitrary process. A runner folder is not a VM.

UAC is explicit authorization for a bounded privileged operation, not a license to disable application control or run all future jobs as admin. Do not copy Windows No Sleep's power/lock behavior or assume its broker IPC is safe for this different product.

## Proposed defaults

No automatic public jobs. No live execution workflow in the product repo. No source push-back. No administrator job identity. No remembered broad admin token available to target builds. No internet-exposed inbound remote control service. No automatic service mode until protected paths and identities are proved.

These are design requirements, not verified implementation guarantees. The planner must specify an achievable first milestone and explicitly block any mode whose boundary cannot yet be enforced.

## Emergency control

Design Stop taking jobs / Cancel current job / Disconnect and revoke as distinct actions. Cancellation cannot guarantee rollback of arbitrary build side effects. Remote runner removal does not by itself wipe local data or revoke an unrelated credential. Never hide a pending cleanup/revocation failure.
