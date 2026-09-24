# CURRENT — github-runner-local

Updated: 2026-09-24. Phase: **A1_MERGED / B_IN_PROGRESS_UNDER_LEASE**.

## Authority

- Canonical branch: `main`.
- Control Tower: Issue #1.
- Owner authorization: Issue #1 comment `5809860108`.
- Opus plan: Issue #2 comment `5807784901`.
- CT plan review: Issue #2 comment `5807941253`.
- Active task: Issue #8 — GRL-005 Checkpoint B WPF wizard shell with fake adapters.
- Task mirror: `docs/tasks/GRL-005-b-wpf-shell.md`.
- Active lease: `GRL-SOL-LEASE-A1-CLOSEOUT-B-20260924`, Issue #1 claim `5809865391`, expiring `2026-09-24T17:35:48Z` (soft cutoff `2026-09-24T15:35:48Z`).
- Current handoff: `docs/control-tower/handoffs/GRL-20260924-A1-MERGED-B-ACTIVE-V6.md`.
- Required sentinel: `END_OF_GRL_HANDOFF key=GRL-20260924-A1-MERGED-B-ACTIVE-V6 sections=8`.

## A1 lineage and merge

- Implementation: Issue #4 comment `5808741650`.
- Independent review: Issue #4 comment `5809108582` identified R-A1-1.
- Correction: Issue #4 comment `5809246654`.
- Independent exact-head rereview: Issue #4 comment `5809464438`, `PASS_A1_CORRECTION_EXACT_HEAD`, R-A1-1 closed.
- Reviewed head: `e21599eda1d4c40c391c2d34546e616e59645144`; original base: `3e02566f5dcaeea01f5f34284e21731c71b1144b`.
- PR #5 merged by merge commit `49569022b7773c12ebd3681f2ddc7f40f9826f2e`, method `merge`. The merge has parents `68ad9ea3ee3cfa81da91d4edb7400fb9ac8eb5a5` and the reviewed head.
- Post-merge local Windows .NET SDK 10.0.401: `dotnet build -warnaserror` passed with 0 warnings/errors; `dotnet test` passed Core 192/192, 0 failed, 0 skipped. Evidence: `LOCAL_CHECKED`.
- Issues #4, #6 and #7 are closed as completed. PR #3 remains open/draft and proposed.

## Checkpoint B

The authorized B task is Issue #8. It permits only a WPF shell using fake adapters and no live GitHub, auth, runner or OS integration. One implementation branch `feat/grl-b-wpf-shell` and one DRAFT PR will be published from the T1 continuity commit. Evidence ceiling: `LOCAL_CHECKED`. The implementing session does not review its own work.

## Constraints

No GitHub-hosted Actions or workflow changes. No runner download/registration, GitHub App/OAuth, private execution repo, real device flow, UAC/service/elevated helper, machine-policy/security change, release, or later checkpoint work. PR #3 is untouched and still proposed.

## Next

The lease holder implements GRL-005 and publishes one DRAFT PR. Independent exact-head review follows. No other writer should take GRL-005 while the lease is active.
