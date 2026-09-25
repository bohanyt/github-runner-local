# CURRENT — github-runner-local

Updated: 2026-09-25. Phase: **D_SOURCE_REREVIEW_AND_SEPARATE_WINDOWS_PENDING; OFFICE_G1_GATED**.

## Authority

- Canonical branch: `main`; owner-designated successor Control Tower coordinates on Issue #1.
- Current handoff: `docs/control-tower/handoffs/GRL-20260925-D-25A3884-SOURCE-WINDOWS-PENDING-V35.md`.
- Required sentinel: `END_OF_GRL_HANDOFF key=GRL-20260925-D-25A3884-SOURCE-WINDOWS-PENDING-V35 sections=6`.

## Source status

C and E independently reviewed/merged. D **NOT merged**. SAME DRAFT PR #17 / `feat/grl-d-portable-lifecycle` at `25a38842b9665d55fa9c85b3071055149110c1ad`; old failed head `69c0b9e267819e11366729805cb61de456b53524`; original source base `be6a34872db8c059de060fafbb4188a0e39e1ce1`. Worker handoff Issue #16 `5835825727`, claim released Issue #1 `5835830917`, one fast-forward commit/four allowlisted paths. Worker reports ID-less reopen removal now blocked, refused recovery preserves evidence, test import fixed.

Local Windows worker proof at new head: restore/build warnings-as-errors PASS (0 warnings/errors), two full unfiltered solution tests **382/382 each**, Core 192, Presentation 58, Integration 132, 0 failed/skipped, BatchBoundary **11/11**, `git diff --check` PASS. This is `LOCAL_CHECKED` worker evidence only; official RunnerContractProbe was not rerun on new head. No independent new-head source verdict or separate new-head Windows witness yet. Old-head failures and passes cannot transfer.

## Active tasks

Issue #16 CT packet `5836048062` dispatches one independent source reviewer different from worker/CT across all 21 PR paths and four correction paths, **and** one separate office Windows read-only tester on exact head. They may run in parallel with separate Issue #1 claims and Issue #16 results. Reviewer checks R-D-3 ID-less repeated Recover/crash safety plus R-D-1/R-D-2/R-D-4 regression guards. Tester runs clean non-admin restore/build/twice-full-tests, official v2.337.0 CLI probe, diff/status. CT adjudicates only same unchanged SHA; any FAIL returns to bounded same-PR correction, both PASS lead to separate merge readiness decision. No self-review.

## Live gates and next

No hosted Actions until owner re-enables. No live login, runner registration/start, Stop Now, remote removal, private execution-repo activation, merge or G1 during source proof. Office `grl-office` owner approved OD-7/D29 for later Issue #15 G1 on private `bohanyt/github-runner-local-exec` only after reviewed D merge; G1 checks actual log runner version vs E metadata. Conditional post-G1 personal `grl-personal` remains D30 one active at a time, with Issue #18 switch contract unproved.

Dispatch both exact-head proofs under `5836048062`; CT then decides correction or merge readiness.
