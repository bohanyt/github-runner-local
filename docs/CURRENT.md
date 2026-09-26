# CURRENT — github-runner-local

Updated: 2026-09-26. Phase: **PR19_W1_CORRECTION_READY; NF1_REVIEW_ACCEPTED; LIVE_RETRY_GATED; G1_NOT_PASSED**.

## Authority

- Canonical branch: `main`; continuing owner-designated Control Tower coordinates on Issue #1.
- Current handoff: `docs/control-tower/handoffs/GRL-20260926-PR19-W1-RECOVERY-CORRECTION-V41.md`.
- Required sentinel: `END_OF_GRL_HANDOFF key=GRL-20260926-PR19-W1-RECOVERY-CORRECTION-V41 sections=5`.
- Active task: **Issue #15 W-1 correction packet `5841514277`**, FULL through `END_OF_GRL_W1_CORRECTION_PACKET key=GRL-PR19-W1-INTERRUPTION-REMOTE-RESTORE-20260926 sections=6`.

## Accepted review and remaining blocker

Independent review `5841490920`, reviewer claim/release `5841467525` / `5841493340`: NEEDS_G1_NF1_CORRECTION at SAME DRAFT PR #19 head `5d4b45b40ddee356b57069267e19a64b60d99789`, source base `6e7ef6efd3e48fcd44645e4270eda800edf22e68`. NF-1 production source/focused checks PASS; WRITE_SCOPE_POLICY_EXCEPTION_CONFIRMED. Reviewer executed NF-1 10/10, existing overlay 10/10 and template 117/118 with WRITE_SCOPE as the sole failure. Keep the raw exception visible, not all-green. CT did not run those tests or self-review.

**W-1 SAFETY_BLOCKER:** interrupted apply/restore before commit leaves DIRTY_TREE which restore refuses; local-HEAD-only verify-normal does not prove clean worktree or actual private remote restoration. Reviewer reproduced the interruption cases offline. The overlay has not been deployed; no current runner damage is claimed. No merge/live retry until W-1 closes.

## Immediate correction: same Opus, same PR, three paths

After CT claim `5841507387` is released and no competing claim exists, SAME local Opus takes a NEW correction claim and continues SAME `fix/g1-action-input-env` / DRAFT PR #19 from exact old head `5d4b45b40ddee356b57069267e19a64b60d99789`. Normal fast-forward only; no new PR or unnecessary main merge.

Change ONLY `tools/g1-witness/overlay.mjs`, `tools/g1-witness/overlay.test.mjs`, `docs/dev/G1-SHA-MISMATCH-WITNESS.md`. Freeze the entire `templates/execution-repo/**` tree (including NF-1 fix/regressions/linter/workflow) and D/dependencies. CT-SCOPE-1 remains limited to the same three acceptance-only paths.

Implement bounded checkpointed/idempotent recovery for exact known apply/restore/index/worktree/commit/push states; preserve/refuse unknown or unrelated changes. Add executable correct-private-remote/branch preflight and freshly verified post-push restoration; overall normal requires exact baseline, clean local state, tree equality to pre-overlay P and confirmed actual private main. Uncertain push/network state is pending, never assumed success. No global relaxation of DIRTY_TREE, reset/clean/force-push, or general recovery framework. Full contract and interruption matrix are in packet `5841514277`.

Run complete offline witness tests plus diff/path/blob checks; prove the template tree remains unchanged and reuse existing NF-1/template evidence. No mandatory full template rerun, old NF-1 red repetition, .NET 384/384/WPF/RunnerContractProbe, whole-D audit or separate Windows campaign.

## Sequence and preserved state

**SEQUENTIAL:** SAME Opus correction/tests/SAME-PR handoff -> ONE focused independent W-1 delta rereview (prefer SAME reviewer, separate from implementer/CT) -> separate CT merge/resume -> SAME office operator continues G1 on existing registration. Worker releases claim at handoff. No live deployment/requests during review; unresolved genuine blocker requires concrete scope escalation, not waiver.

Preserve earlier live progress: grl-office ID 2, wizard/root, private bootstrap `3ca0449e39fdc28cf5ca15b30967833a95649192`, mailbox #1, four variables/reserve 10 GiB, failed-positive and passed-prefilter evidence. Last online/idle status is an operator snapshot, not current CT evidence. Do not repeat enrollment or implicitly stop/unregister/close-relaunch/reboot. Idle is not safe-drain proof.

ARCHIVE/CHECKPOINT FIRST; previous main `9236b712bb177b326c44b3b1a9e4a3e22e922f9b`/V40 and original PR head remain recoverable. No global env/auth/credential resets or public secrets/machine details. No hosted Actions, service/UAC/security/sleep-policy changes, Stage-2/cross-repo execution or personal runner. NF-2 and app reopen/restart remain post-G1 before separately proved Issue #18 switching. CT has not launched an agent or background monitor.
