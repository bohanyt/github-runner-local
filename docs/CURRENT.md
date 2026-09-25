# CURRENT — github-runner-local

Updated: 2026-09-25. Phase: **D_CORRECTION_REREVIEW_AND_WINDOWS_PENDING; OFFICE_G1_GATED**.

## Authority

- Canonical branch: `main`; owner-designated successor Control Tower coordinates on Issue #1.
- Current handoff: `docs/control-tower/handoffs/GRL-20260925-D-RD3-RD4-REREVIEW-WINDOWS-PENDING-V33.md`.
- Required sentinel: `END_OF_GRL_HANDOFF key=GRL-20260925-D-RD3-RD4-REREVIEW-WINDOWS-PENDING-V33 sections=6`.

## Source status

C and E are independently reviewed and merged. D is **NOT merged**. SAME DRAFT PR #17 / `feat/grl-d-portable-lifecycle`, current head `69c0b9e267819e11366729805cb61de456b53524`, old reviewed head `d68fc60e29726df25733acb8f00f3ea3ce104ac6`, source base `be6a34872db8c059de060fafbb4188a0e39e1ce1`. Worker R-D-3/R-D-4 correction Issue #16 `5835153272` changed nine allowed paths, fast-forward only, with N-2 close-copy fix. Worker had no .NET/compiler: restore/build/full tests twice and literal `git diff --check` were **NOT RUN**. PR description was updated to remove old Drain/test claims. No independent new-head source verdict exists yet.

Previous Windows read-only witness `5834244169` passed on **old** `d68fc60` (restore/build, two 365/365 runs, 11/11 BatchBoundary, pinned v2.337.0 CLI probe); it cannot be transferred to new head. Neither witness is real-integration `WINDOWS_TESTED`.

## Active tasks

Issue #16 CT packet `5835191415` dispatches a different independent source reviewer for all 21 PR paths/nine correction paths **and** a separate office Windows read-only tester for exact current head. These separate evidence streams may run in parallel, with separate Issue #1 claims and Issue #16 results. Both must pass at unchanged SHA for CT to decide a later merge gate; any failure returns to bounded same-PR correction. No source reviewer self-review and no live action.

## Live gates

No hosted Actions until owner re-enables. No live login, runner registration/start, private execution-repo activation, Stop Now, or G1 under source review. Office `grl-office` is owner approved by OD-7/D29 for later G1 on private `bohanyt/github-runner-local-exec` only after reviewed D merge. G1 Issue #15 must cross-check actual runner log version against E result metadata to detect in-process auto-update. Conditional post-G1 personal `grl-personal` remains D30, one active at a time; switching Issue #18 remains unproved.

## Next

Run independent exact-head review and separate office read-only Windows proof under `5835191415`; CT adjudicates same-head evidence, then decides correction or merge readiness. G1 remains gated.
