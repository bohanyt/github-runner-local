# CURRENT — github-runner-local

Updated: 2026-09-24. Phase: **B_IMPLEMENTED / AWAITING_INDEPENDENT_REVIEW**.

## Authority

- Canonical branch: `main`.
- Control Tower: Issue #1.
- Owner authorization: Issue #1 comment `5809860108`.
- Opus plan: Issue #2 comment `5807784901`.
- CT plan review: Issue #2 comment `5807941253`.
- Implemented task: Issue #8 — GRL-005 Checkpoint B WPF wizard shell with fake adapters.
- Task mirror: `docs/tasks/GRL-005-b-wpf-shell.md`.
- Implementation handoff: Issue #8 comment `5810381568`.
- Implementation lease: `GRL-SOL-LEASE-A1-CLOSEOUT-B-20260924`, Issue #1 claim `5809865391`; work concluded and release recorded on Issue #1 after this continuity transaction.
- Current handoff: `docs/control-tower/handoffs/GRL-20260924-B-IMPLEMENTED-REVIEW-PENDING-V7.md`.
- Required sentinel: `END_OF_GRL_HANDOFF key=GRL-20260924-B-IMPLEMENTED-REVIEW-PENDING-V7 sections=8`.

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

Issue #8 is implemented on `feat/grl-b-wpf-shell` from T1 `31d823604fa21fc755a1cfbf28e9db90ab8df8ec`. DRAFT PR #9 is open and unmerged at exact head `bbceeeeef111d1e5dcc62690c263a43f97b93ac6`, with 19 allowed changed paths. Issue #8 comment `5810381568` records the implementation and full local evidence. The fake-only solution build passed with zero warnings/errors; Core tests passed 192/192 and Presentation tests 42/42, each with zero failed/skipped. Fake-mode UIA smoke S1–S5 passed on one worker Windows desktop at one DPI. Evidence ceiling: `LOCAL_CHECKED`. The implementing session did not review its own work.

## Constraints

No GitHub-hosted Actions or workflow changes. No runner download/registration, GitHub App/OAuth, private execution repo, real device flow, UAC/service/elevated helper, machine-policy/security change, release, or later checkpoint work. PR #3 is untouched and still proposed.

## Next

Owner/CT creates ONE independent exact-head review packet for PR #9 head `bbceeeeef111d1e5dcc62690c263a43f97b93ac6`. The implementing session must not review. PR #9 is not merge-authorized. Checkpoint C remains unauthorized.
