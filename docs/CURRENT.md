# CURRENT — github-runner-local

Updated: 2026-09-24. Phase: **A1_CORRECTED / AWAITING_EXACT_HEAD_REREVIEW**.

## Authority

- Canonical branch: `main`.
- Control Tower: Issue #1.
- Opus plan: Issue #2 comment `5807784901`.
- CT plan review: Issue #2 comment `5807941253`.
- A1 implementation handoff: Issue #4 comment `5808741650`.
- Independent review: Issue #4 comment `5809108582` → `NEEDS_A1_CORRECTION`.
- Correction handoff: Issue #4 comment `5809246654`.
- Correction claim released: Issue #1 comment `5809250270`.
- SAME DRAFT PR #5 corrected head: `e21599eda1d4c40c391c2d34546e616e59645144`.
- Base remains: `3e02566f5dcaeea01f5f34284e21731c71b1144b`.
- Active task: Issue #7 — GRL-004 exact-head rereview of R-A1-1 correction.
- Task mirror: `docs/tasks/GRL-004-a1-correction-rereview.md`.
- Current handoff: `docs/control-tower/handoffs/GRL-20260924-A1-CORRECTION-REREVIEW-READY-V5.md`.
- Required sentinel: `END_OF_GRL_HANDOFF key=GRL-20260924-A1-CORRECTION-REREVIEW-READY-V5 sections=7`.

## Correction state

R-A1-1 was corrected on SAME branch/PR. Only:
- `src/Grl.Core/WindowsPathPolicy.cs`
- `tests/Grl.Core.Tests/WindowsPathPolicyTests.cs`

changed after the reviewed head.

Worker-local Windows proof at corrected head:
- .NET SDK 10.0.401;
- `dotnet build -warnaserror`: PASS, 0 warnings/errors;
- `dotnet test`: 192/192 PASS, 0 skipped.

Evidence remains `LOCAL_CHECKED`.

## Exact rereview target

Review ONLY corrected PR #5 head:

`e21599eda1d4c40c391c2d34546e616e59645144`

against original finding R-A1-1 and correction handoff `5809246654`.

The rereviewer may reuse prior review conclusions for unaffected files. If the PR head moves, stop as stale.

## Constraints

No GitHub-hosted Actions. Rereview is read-only except claim/result/release comments. No source/PR edits, merge, later checkpoint work, runner/App/execution-repo/UAC/service/machine changes.

## Next

One independent reviewer may claim Issue #7, perform the bounded correction rereview, post one result on Issue #4, release the claim, and stop.
