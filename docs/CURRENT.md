# CURRENT — github-runner-local

Updated: 2026-09-26. Phase: **NF1_PR19_INDEPENDENT_DELTA_REVIEW_READY; LIVE_RETRY_GATED; G1_NOT_PASSED**.

## Authority

- Canonical branch: `main`; continuing owner-designated Control Tower coordinates on Issue #1.
- Current handoff: `docs/control-tower/handoffs/GRL-20260926-PR19-NF1-DELTA-REVIEW-V40.md`.
- Required sentinel: `END_OF_GRL_HANDOFF key=GRL-20260926-PR19-NF1-DELTA-REVIEW-V40 sections=5`.
- Active task: **Issue #15 independent review packet `5841404018`**, FULL through `END_OF_GRL_NF1_REVIEW_PACKET key=GRL-PR19-5D4B45B-SCOPE-DELTA-REVIEW-20260926 sections=5`. Original correction/witness contract `5841236994` remains, with the precise CT-SCOPE-1 exception below.

## Exact candidate and evidence

SAME open/draft/unmerged PR #19 / `fix/g1-action-input-env`, head `5d4b45b40ddee356b57069267e19a64b60d99789`, source base `6e7ef6efd3e48fcd44645e4270eda800edf22e68`. Two commits, five paths: production action-io helper, its env-boundary regression file, acceptance-only overlay tool/test, and witness runbook. Worker handoff `5841357439`; claim/release `5841285042` / `5841358891`.

Worker Node v24.14.1 LOCAL_CHECKED: old-helper red 9/10 failures -> fixed 10/10; intermediate `7bc4cec` template suite 118/118 and lint PASS; FINAL HEAD template suite **117/118**, lint **FAIL: WRITE_SCOPE only**, fixtures/schema/diff PASS, overlay offline 10/10. CT has not executed these tests or independently accepted the implementation.

CT-SCOPE-1 permits ONLY the WRITE_SCOPE policy exception for the previously authorized `tools/g1-witness/overlay.mjs`, `tools/g1-witness/overlay.test.mjs`, and `docs/dev/G1-SHA-MISMATCH-WITNESS.md`. The unchanged E-era linter restricts every diff path to the template, conflicting with the explicit witness location. Independent reviewer must confirm exact five-path scope and no other lint/test failure. Keep raw failure/counts visible; do not call final head all-green. No semantic/security/schema/witness waiver, no new paths, no source/linter changes or ref manipulation to manufacture green. Do not move the acceptance harness into shipping code just to silence this scope check.

## One independent review, then CT gate

After CT claim `5841396993` release/no competitor, ONE DIFFERENT session from implementer and CT takes a review claim and reviews the integrated five-file delta, NF-1 behavior/regressions, full witness tool/tests/runbook, and CT-SCOPE-1. No subagents; isolated Linux or Windows compute is sufficient, no office runner required. Packet `5841404018` defines focused checks, findings triage and exact-head verdict/publication/release. No repeated D/.NET 384/384/WPF/RunnerContractProbe or additional pre-merge Windows campaign.

**SEQUENTIAL:** implementation complete -> ONE independent delta review -> separate CT merge/resume decision -> SAME local Opus session resumes G1 on existing grl-office registration. PR #19 is NOT approved/merged by this publication. No private deployment, overlay activation or new live request before the gate. Docs-only CT publication does not require another source/test round.

## Preserved live state and standing boundaries

Operator report `5841173894`: successful real product login/enrollment, private bootstrap `3ca0449e39fdc28cf5ca15b30967833a95649192`, mailbox #1, four variables (reserve 10 GiB), grl-office ID 2. Positive run `36201953082` failed before ACK due to NF-1; prefilter negative `36202109916` skipped all jobs. No checkout-mismatch live witness/G1 PASS exists. Worker last rechecked ID 2 online/idle under preserved wizard at correction handoff; refresh actual state before later continuation.

Preserve roots, registration, wizard, private history/settings and evidence; no implicit Stop Now/unregister/close-relaunch/reboot/reinstall or idle-based cleanup. After review/CT merge, checkpoint private main, deploy only corrected reviewed template, use new positive request and separately disclosed FAULT_INJECTED mismatch with exact rollback. Do not repeat enrollment.

No hosted Actions, service/UAC/security/sleep-policy changes, Stage-2/cross-repo execution, personal runner or global auth/env reset. ARCHIVE/CHECKPOINT FIRST; previous main `6e7ef6efd3e48fcd44645e4270eda800edf22e68`/V39 remain recoverable history. D remains merged; NF-2 deferred post-G1, then reopen/restart and separate Issue #18 switching priority. Keep secrets/local machine details out of public evidence. No agent/background monitor has been launched by this CT publication.
