# github-runner-local — PR #19 W-1 recovery correction (V41)

## 1. Authority and exact target

**PR19_W1_CORRECTION_READY; NF1_REVIEW_ACCEPTED; LIVE_RETRY_GATED; G1_NOT_PASSED.** Active Issue #15 packet `5841514277` must be read IN FULL through `END_OF_GRL_W1_CORRECTION_PACKET key=GRL-PR19-W1-INTERRUPTION-REMOTE-RESTORE-20260926 sections=6`. It narrows the immediate work to W-1; original correction/witness contract `5841236994` and CT-SCOPE-1 in `5841404018` remain except where this recovery packet explicitly refines them.

SAME open/draft/unmerged PR #19 / `fix/g1-action-input-env`, exact old head `5d4b45b40ddee356b57069267e19a64b60d99789`, original source base `6e7ef6efd3e48fcd44645e4270eda800edf22e68`. Main before this publication `9236b712bb177b326c44b3b1a9e4a3e22e922f9b` and V40 remain recoverable history. CT claim `5841507387`; implementer must wait for its release and verify ownership.

## 2. Independent evidence and the one blocker

Independent reviewer Issue #15 `5841490920`, claim/release Issue #1 `5841467525` / `5841493340`, published NEEDS_G1_NF1_CORRECTION. **NF-1 source/focused tests PASS** and **WRITE_SCOPE_POLICY_EXCEPTION_CONFIRMED** are retained. Reviewer executed NF-1 10/10, existing witness 10/10, full template 117/118; only failure is the unchanged E-era WRITE_SCOPE policy against the three authorized acceptance-only paths. Do not call final head all-green or confuse these results with live G1. CT did not execute those tests or act as independent reviewer.

W-1 SAFETY_BLOCKER is in the not-yet-deployed witness tool/runbook, not a new production input bug: interruption after apply writes or restore writes but before commit leaves a dirty tree that restore refuses. Local-only verify-normal can miss an uncommitted overlay or remote main still using the overlay. Reviewer reproduced both DIRTY_TREE cases in disposable repositories. No evidence says an overlay has been deployed or the office runner damaged.

## 3. SAME-PR correction and proof

SAME local Opus implementer, no subagents, takes a NEW Issue #1 claim after CT release/no competitor. Continue existing branch/worktree and SAME DRAFT PR #19 from the exact old head, normal fast-forward only. Change ONLY tools/g1-witness/overlay.mjs, tools/g1-witness/overlay.test.mjs and docs/dev/G1-SHA-MISMATCH-WITNESS.md. Keep the entire templates/execution-repo tree, NF-1 fix/tests, normal workflow, linter, D and dependencies byte-identical to the reviewed old head. No new PR, force push, broad refactor or main merge merely to update docs.

Packet requires recoverable pre-overlay P/tree/baseline/expected-overlay/parameter/destination evidence; bounded idempotent recovery for recognized HEAD/index/worktree states before/after apply/restore commits and uncertain pushes; refusal without mutation for unknown or unrelated dirty state; executable correct private remote/branch checks before push and fresh actual remote verification after it. Overall normal requires exact baseline, clean local state, tree equality to P and actual private main restoration. Network uncertainty remains pending. Never remove DIRTY_TREE globally or use destructive reset/clean/force-push to make recovery appear successful.

Keep the two-location FAULT_INJECTED method, distinct harmless R/O, request identity, pins/permissions/actions, nonce/envelope guards and fresh outcomes unchanged. Queue/content checks may remain explicit operator checks; do not pretend to lock outside actors. Minimal tool plus executable runbook is enough, not a new recovery framework.

Add disposable local/bare-remote interruption tests for staged/unstaged recovery, before/after commit/push, uncertain push acknowledged remotely versus remote still overlaid, misleading local-normal cases, wrong destination/branch, remote drift and preservation of unrelated bytes. Run the complete witness tests at final head plus diff/path/blob checks. Reuse existing independent template results after demonstrating unchanged template tree. No mandatory full template rerun or repeated NF-1 red, .NET 384/384, WPF, D review, RunnerContractProbe or standalone Windows witness.

## 4. Sequential gate and continuation

**SEQUENTIAL:** SAME Opus implementation/tests/SAME draft PR handoff -> ONE W-1-focused independent delta rereview, preferably the SAME reviewer session -> separate CT merge/resume -> SAME local operator resumes real G1 with existing registration. Reviewer remains different from implementer/CT. Recheck unchanged template blobs to retain accepted NF-1/scope conclusions; do not restart broad review. Worker publishes exact old/new head, three-path delta, interruption matrix/results and unchanged template evidence on Issue #15, then releases claim. No deployment during review.

If the bounded recovery approach proves infeasible, report a concrete smaller scope/disabled-witness decision to CT/owner rather than expanding scope or waiving the known blocker. G1 still needs the normal positive and required negatives after correction review/merge; no partial source PASS is G1_OFFICE_PASS.

## 5. Preserved live state and safety

Earlier operator evidence `5841173894`: product enrollment grl-office ID 2, private bootstrap `3ca0449e39fdc28cf5ca15b30967833a95649192`, mailbox #1, four variables including reserve 10 GiB; positive run `36201953082` failed before ACK; prefilter `36202109916` all skipped. Correction implementer last reported ID 2 online/idle under the open wizard. CT has not rechecked current liveness. Preserve registration/root/wizard/private history/settings/evidence. No new mailbox requests, overlay activation, private deployment, reinstall, registration, implicit Stop Now/unregister, close/relaunch or reboot while gated.

ARCHIVE/CHECKPOINT FIRST. No global auth/env reset, secret exposure, hosted Actions, service/UAC/security/sleep-policy changes, cross-repo/Stage-2 execution or personal runner. Idle snapshots are not safe-drain proof. NF-2 and app reopen/restart remain post-G1 before separate Issue #18 one-active-at-a-time switching. This CT publication changes coordination/docs only; no background agent, monitoring, code implementation, self-review or live work.

END_OF_GRL_HANDOFF key=GRL-20260926-PR19-W1-RECOVERY-CORRECTION-V41 sections=5
