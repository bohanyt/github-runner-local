# CURRENT — github-runner-local

Updated: 2026-09-26. Phase: **D_PROVENANCE_REREVIEW_AND_SEPARATE_WINDOWS_PENDING; OFFICE_G1_GATED**.

## Authority

- Canonical branch: `main`; owner-designated successor Control Tower coordinates on Issue #1.
- Current handoff: `docs/control-tower/handoffs/GRL-20260926-D-82F1722-SOURCE-WINDOWS-PENDING-V37.md`.
- Required sentinel: `END_OF_GRL_HANDOFF key=GRL-20260926-D-82F1722-SOURCE-WINDOWS-PENDING-V37 sections=6`.

## Source and worker proof

C/E reviewed and merged; D **NOT merged**. SAME open/draft PR #17 / `feat/grl-d-portable-lifecycle` at `82f1722902e48635bc5e2a55c77a274a1e6e29cc`; old head `25a38842b9665d55fa9c85b3071055149110c1ad`; original source base `be6a34872db8c059de060fafbb4188a0e39e1ce1`. One fast-forward commit, three allowlisted paths; worker handoff Issue #16 `5840372215`, release Issue #1 `5840376225`. Failed/uncertain `config.cmd` now reports no automatically bound numeric ID or remote cleanup; ID-less reopen/repeated Recover remains manual pending. Successful config plus unique ID path remains. Worker office Windows `LOCAL_CHECKED`: restore/build warnings-as-errors PASS (0 warnings/errors), two full unfiltered solution runs **384/384** each (Core 192, Presentation 58, Integration 134), 0 failed/skipped, BatchBoundary **11/11**, diff check PASS. Official RunnerContractProbe not rerun at new head. No independent source verdict or separate new-head Windows witness yet.

## Active tasks

Issue #16 CT packet `5840393630` dispatches ONE different independent source reviewer across full 21 PR files/three correction paths and ONE separate non-admin office Windows read-only tester at this exact head, possibly in parallel with separate Issue #1 claims/results. Reviewer must independently check unrelated same-name runner after failed config, ID-less durable/reopen/repeated Recover, successful unique-ID path, and R-D-1/R-D-2/R-D-4. Tester must run full restore/build/twice-unfiltered tests and official pinned v2.337.0 CLI probe. CT adjudicates only both results at same unchanged SHA. Neither worker nor witness source proof is real-integration `WINDOWS_TESTED`.

## Live gates and next

No hosted Actions until owner re-enables. No live login, runner registration/start/removal, Stop Now, GitHub job, private execution-repo activation, merge or G1 during D proof. Office `grl-office` owner-approved OD-7/D29 for later Issue #15 G1 on private `bohanyt/github-runner-local-exec` only after reviewed D merge; check actual runner log version against E result metadata. Conditional personal `grl-personal` after G1 PASS remains D30, one active at a time; Issue #18 switching proof pending.

Next: collect independent review plus separate exact-head Windows witness under `5840393630`; CT decides correction or merge readiness.
