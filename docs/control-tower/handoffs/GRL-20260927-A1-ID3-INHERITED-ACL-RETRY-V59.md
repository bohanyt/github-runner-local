# github-runner-local — A1 ID3 retry with inherited checkpoint permissions (V59)

## 1. Phase

**GRL014_SOURCE_MERGED; A1_ID3_RETRY_AUTHORIZED; A5_ODG_GATED; GRL015_ACCEPTED.**

Active retry packet: Issue #18 `5856442213`, through:
`END_OF_GRL014_A1_RETRY_PACKET key=GRL014-A1-ID3-INHERITED-ACL-RETRY-20260927 sections=6`.

Prior live attempt `5856233967` was BLOCKED before mutation because custom checkpoint ACL setup hit a Windows trust-relationship error. Request count 0; ID3/root/.env/Listener remained unchanged.

## 2. CT correction

Custom ACL manipulation is NOT required for checkpoint validity.

Checkpoint must instead use a new owner-controlled local directory with its existing inherited permissions unchanged, plus exact byte/hash/existence/attribute capture and read-back verification.

Forbidden in this retry: Set-Acl, icacls, ownership changes, domain repair, security-policy changes.

## 3. Worker

Use ONE fresh local Windows Opus proof/operator session. Do not resume the blocked Sol session.

Same owner-authorized A1 live scope applies after checkpoint PASS: same ID3 only, reversible hooks/.env, idle Listener restarts, exactly two sequential existing js-smoke requests PASS then REFUSE-execute, byte-exact rollback.

## 4. Sequence

**SEQUENTIAL:** inherited-permission checkpoint PASS -> A1 two-run witness -> rollback/final same-ID3 proof -> CT adjudication.

A5/OD-G remains separate and unauthorized.

## 5. Safety

Stop before mutation if ordinary inherited permissions cannot create/read/verify the recovery checkpoint.
Preserve prior failed checkpoint stub/evidence; do not use it as restore source.
No re-registration/removal, reboot, workflow mutation, third request, personal enrollment, control repo/App permission or hosted Actions.
ARCHIVE/CHECKPOINT FIRST.

END_OF_GRL_HANDOFF key=GRL-20260927-A1-ID3-INHERITED-ACL-RETRY-V59 sections=5