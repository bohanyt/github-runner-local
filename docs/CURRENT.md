# CURRENT — github-runner-local

Updated: 2026-09-24. Phase: **B_NEEDS_CORRECTION / R-B-1_R-B-2_PACKET_READY**.

## Authority

- Canonical branch: `main`.
- Control Tower: Issue #1.
- Owner authorization: Issue #1 comment `5809860108`.
- Opus plan: Issue #2 comment `5807784901`.
- CT plan review: Issue #2 comment `5807941253`.
- Active task: Issue #8 — GRL-005 Checkpoint B WPF wizard shell with fake adapters.
- Task mirror: `docs/tasks/GRL-005-b-wpf-shell.md`.
- Implementation handoff: Issue #8 comment `5810381568`.
- Independent B review: Issue #8 comment `5811022199` → `NEEDS_B_CORRECTION`, findings R-B-1 and R-B-2.
- Reviewer claim released: Issue #1 comment `5811024611`.
- Correction packet: Issue #8 comment `5811157965`.
- Current handoff: `docs/control-tower/handoffs/GRL-20260924-B-CORRECTION-READY-V8.md`.
- Required sentinel: `END_OF_GRL_HANDOFF key=GRL-20260924-B-CORRECTION-READY-V8 sections=7`.

## A1 lineage and merge

A1 remains merged and closed. PR #5 merged by merge commit `49569022b7773c12ebd3681f2ddc7f40f9826f2e` from independently reviewed head `e21599eda1d4c40c391c2d34546e616e59645144`. Post-merge Core proof remains 192/192, 0 skipped, `LOCAL_CHECKED`.

## Checkpoint B candidate

- Branch: `feat/grl-b-wpf-shell`.
- Base: `31d823604fa21fc755a1cfbf28e9db90ab8df8ec`.
- SAME DRAFT PR #9 is open/unmerged.
- Exact reviewed head before correction: `bbceeeeef111d1e5dcc62690c263a43f97b93ac6`.
- Original implementation evidence: build 0 warnings/errors; Core 192/192; Presentation 42/42; fake-mode UI smoke S1–S5 PASS; evidence `LOCAL_CHECKED`.

Independent review found product architecture/source otherwise sound, but two blocking defects in test/evidence code:
- R-B-1: forbidden-API regression guard has explicit false negatives and non-recursive Grl.App source coverage.
- R-B-2: UiSmoke S1 can misclassify real product failures as BLOCKED with exit 0.

## Correction boundary

Correct ONLY R-B-1 and R-B-2 on the SAME branch/PR. Exactly two files may change relative to reviewed head:
- `tests/Grl.App.Presentation.Tests/PresentationTests.cs`
- `tests/Grl.App.UiSmoke/Program.cs`

Non-blocking N1–N5 are out of scope. No product source, Core, project/package, docs, authority, workflow, PR metadata or Checkpoint C changes.

## Constraints

No GitHub-hosted Actions. No merge. No self-rereview by the correction worker. No runner/auth/App/execution-repo/UAC/service/machine-policy/release/Checkpoint C work. PR #3 remains untouched.

## Next

One bounded correction worker may claim GRL-007, execute Issue #8 correction packet `5811157965` against exact old head `bbceeeeef111d1e5dcc62690c263a43f97b93ac6`, push to SAME PR #9, publish correction evidence, release, and stop. Then a different independent reviewer rereviews the NEW exact PR #9 head.

