# CURRENT — github-runner-local

Updated: 2026-09-26. Phase: **D_CONFIGURE_FAIL_PROVENANCE_CORRECTION; OFFICE_G1_GATED**.

## Authority

- Canonical branch: `main`; owner-designated successor Control Tower coordinates on Issue #1.
- Current handoff: `docs/control-tower/handoffs/GRL-20260926-D-CONFIGURE-FAIL-PROVENANCE-CORRECTION-V36.md`.
- Required sentinel: `END_OF_GRL_HANDOFF key=GRL-20260926-D-CONFIGURE-FAIL-PROVENANCE-CORRECTION-V36 sections=6`.

## Exact-head status

C/E independently reviewed and merged. D **NOT merged**. SAME open/draft PR #17 / `feat/grl-d-portable-lifecycle` at `25a38842b9665d55fa9c85b3071055149110c1ad`; source base `be6a34872db8c059de060fafbb4188a0e39e1ce1`. Independent Issue #16 review `5836285914`: **NEEDS_D_CORRECTION**, one blocking provenance flaw: failed-to-start `config.cmd` can be followed by an unrelated same-name runner appearing, whose numeric ID is wrongly persisted and later eligible for DELETE. Reviewer released claim Issue #1 `5836293286`. Separate office Windows read-only Issue #16 `5836279999` PASS provisional on this SHA: restore/build 0 warnings/errors; two full solution runs 382/382 each; 11/11 BatchBoundary; official v2.337.0 pinned CLI probe PASS; claim released Issue #1 `5836284576`. This `LOCAL_CHECKED` source proof cannot clear a source blocker or count as real-integration `WINDOWS_TESTED`.

## Active task

Issue #16 CT packet `5836392337` dispatches ONE bounded local Windows worker to correct failed-config ID binding on SAME branch/PR, expected old head `25a38842b9665d55fa9c85b3071055149110c1ad`. Revised contract: bind remote numeric ID automatically only after successful `config.cmd` completion and unique prompt exact-name lookup during the bounded registration; any failed/uncertain configuration or ID attribution is manual inspection, no automatic name-based recovery/deletion. Add real lifecycle+composition regressions for unrelated same-name runner after failed config and repeat Recover; preserve R-D-1/R-D-2/R-D-4. Build and twice-full tests on office Windows before push; claim/release on Issue #1. Another different source review and separate new-head Windows witness follow before CT merge decision.

## Live gates and next

No hosted Actions until owner re-enables. No live login, runner registration/start, Stop Now, remote removal, private execution-repo activation, merge or G1 during source correction. Office `grl-office` approved OD-7/D29 for later Issue #15 G1 on private `bohanyt/github-runner-local-exec` only after reviewed D merge. G1 checks actual runner log version versus E result metadata for in-process auto-update. Conditional personal `grl-personal` after G1 PASS remains D30, one active at a time with Issue #18 switch proof. Next: one correction worker under `5836392337`.
