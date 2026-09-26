# github-runner-local — PR #19 NF-1 independent delta review (V40)

## 1. Authority and frozen candidate

**NF1_PR19_INDEPENDENT_DELTA_REVIEW_READY; LIVE_RETRY_GATED; G1_NOT_PASSED.** Main CURRENT selects this handoff. Active Issue #15 reviewer packet `5841404018` must be read IN FULL through `END_OF_GRL_NF1_REVIEW_PACKET key=GRL-PR19-5D4B45B-SCOPE-DELTA-REVIEW-20260926 sections=5`. Original implementation/witness contract `5841236994` remains authoritative except for the explicit scope-only policy exception. CT claim `5841396993`; reviewer waits for its release and checks current ownership.

SAME open/draft/unmerged PR #19, branch `fix/g1-action-input-env`, head `5d4b45b40ddee356b57069267e19a64b60d99789`, source base `6e7ef6efd3e48fcd44645e4270eda800edf22e68`. Compare: two commits ahead, zero behind, five paths. Worker handoff Issue #15 `5841357439`; claim/release Issue #1 `5841285042` / `5841358891`. Prior main/V39 remains the recoverable documentation checkpoint. This CT publication does not change the PR source head.

## 2. Implementer evidence and CT-SCOPE-1

The only production edit is `templates/execution-repo/lib/action-io.mjs`; regression file `templates/execution-repo/tests/action-io.test.mjs` covers real runner-style environment input names. The other paths are acceptance-only `tools/g1-witness/overlay.mjs`, `tools/g1-witness/overlay.test.mjs`, and `docs/dev/G1-SHA-MISMATCH-WITNESS.md`. No normal shipping workflow change.

Worker reports Node v24.14.1 LOCAL_CHECKED: old-helper red 9/10 failures -> fixed 10/10; intermediate template-only `7bc4cec` full suite 118/118 and lint PASS; final head full suite **117/118**, lint **FAIL: WRITE_SCOPE only**, fixture/schema/diff checks PASS, overlay tests 10/10. These are worker results, not CT-executed tests or independent review. Do not transfer intermediate all-green status to final head.

CT inspected the actual unchanged linter/test rule and the authorized five-file delta. The E-era linter permits only paths under templates/execution-repo; packet `5841236994` deliberately permits the three acceptance-only paths outside it. **CT-SCOPE-1** grants a narrow nonblocking policy exception for WRITE_SCOPE caused by exactly those three paths, subject to independent confirmation of no other scope/lint/test failure. Record `WRITE_SCOPE_POLICY_EXCEPTION_CONFIRMED` if verified. All raw failures remain visible. No waiver of runtime/security/schema/witness validity, no broader allowlist, no linter/source changes or moving harness into the product to manufacture green. Empty diff after merge is not pre-merge proof. A future separation of scope-policy lint is deferred tooling work.

## 3. ONE independent review and proportionate checks

A DIFFERENT session from implementer and CT reviews the integrated five-file PR at exact head, after its own Issue #1 claim. No subagents. Linux or Windows isolated local compute is suitable; no office runner is needed. Fresh-read main authority/full handoff, PROTOCOL, latest claims, packets `5841236994`/`5841404018`, worker handoff `5841357439`, exact PR metadata/full delta and supporting code.

Verify NF-1 naming against the official pinned runner contract, all helper/value/decoded semantics, meaningful isolated env-boundary regressions, and unchanged action/pin/permission/schema/profile/dependency behavior. Independently inspect the acceptance tool, offline tests and complete executable runbook: exact two-location delta, pinned baseline, real harmless R/O identities, nonce fence plus envelope validation, preserved production/request identity, isolated fresh outcomes, zero profile execution, exact byte restoration and operator-versus-tool guarantees. Check failure/interruption paths without broad redesign. An invalid required witness is a G1_PATH_BLOCKER; unsafe reachable behavior is a SAFETY_BLOCKER. Non-destructive unrelated limitations may be DEFERRED.

Run focused NF-1/overlay checks where available and inspect/reproduce the scope-only lint outcome. No mandatory repeated full template campaign; one is permitted if needed to substantiate a finding. No .NET 384/384, WPF, D re-audit, RunnerContractProbe or standalone pre-merge Windows witness. Do not manipulate origin/main to hide the scope failure. Distinguish executed results from worker evidence and source reasoning.

Publish ONE exact-head Issue #15 result: G1_NF1_DELTA_REVIEW_PASS, NEEDS_G1_NF1_CORRECTION, or honest G1_NF1_REVIEW_BLOCKED / G1_NF1_REVIEW_STALE. Include helper/harness/scope-exception conclusions and evidence limits, then release the review claim. No source edits, merge or live actions by reviewer. Source PASS is not G1 acceptance.

## 4. Sequence and preserved live progress

**SEQUENTIAL:** completed Opus implementation -> ONE independent delta review -> separate CT merge/resume gate -> SAME local Opus session continues G1 on existing grl-office registration. Do not deploy/retry in parallel with review or start another local implementation agent. This documentation update alone does not require a new test campaign or source commit.

Preserved operator report `5841173894`: real product login/enrollment, private bootstrap `3ca0449e39fdc28cf5ca15b30967833a95649192`, mailbox #1, four variables (reserve 10 GiB), grl-office ID 2; positive run `36201953082` failed before ACK; prefilter negative `36202109916` skipped all jobs. NF-1 correction is not yet deployed. Worker last rechecked ID 2 online/idle under the open wizard at correction handoff; this is not CT current-liveness evidence.

Keep wizard/root/registration, private history/settings and evidence. After independent PASS and CT merge, fresh-check live/queue/ref state, checkpoint private main, update only the corrected reviewed template, send a NEW normal positive request, use the reviewed FAULT_INJECTED negative with exact rollback, and publish complete live evidence. Revalidate historical prefilter relevance; never claim it ran anew. No full G1 PASS until all required proofs exist.

## 5. Standing safety and next

No new mailbox requests, overlay activation, private deployment, implicit Stop Now/unregister, close/relaunch, reboot or reinstall during this review gate. Idle snapshots are not safe-drain proof. No hosted Actions, service/UAC/security/sleep-policy changes, Stage-2/cross-repo execution or personal runner. No global env/auth/credential resets. ARCHIVE/CHECKPOINT FIRST; keep secrets and local machine details out of public evidence.

NF-2 is deferred post-G1 D robustness. Existing-root reopen/restart is the next product priority after G1, then separately proven Issue #18 one-active-at-a-time personal switching. CT changed only coordination/docs and did not start an agent or background monitor. Next action: one independent reviewer under packet `5841404018`; local Opus retains its context for later continuation.

END_OF_GRL_HANDOFF key=GRL-20260926-PR19-NF1-DELTA-REVIEW-V40 sections=5
