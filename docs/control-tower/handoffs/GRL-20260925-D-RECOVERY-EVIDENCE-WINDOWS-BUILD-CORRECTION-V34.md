# github-runner-local — D recovery evidence and Windows build correction (V34)

## 1. Phase

**D_SOURCE_RECOVERY_EVIDENCE_AND_WINDOWS_BUILD_CORRECTION; OFFICE_G1_GATED.** Independent source review and separate office Windows read-only witness both failed at the same exact draft PR head `69c0b9e267819e11366729805cb61de456b53524`. Issue #16 CT packet `5835514121` dispatches a single bounded correction on the same branch/PR from the office Windows environment with working .NET 10.0.401.

## 2. Exact refs and evidence

At CT claim main `4f004cf44719c4af3ce0dd973ea8b0c1a0268210` selected V33. SAME DRAFT PR #17 / `feat/grl-d-portable-lifecycle` remains open/unmerged at `69c0b9e267819e11366729805cb61de456b53524`; old head `d68fc60e29726df25733acb8f00f3ea3ce104ac6`; source base `be6a34872db8c059de060fafbb4188a0e39e1ce1`. Independent reviewer result Issue #16 `5835420513`, claim released Issue #1 `5835426470`: `NEEDS_D_CORRECTION`. Windows witness Issue #16 `5835428859`, claim released Issue #1 `5835433322`: `D_WINDOWS_READONLY_FAIL`. CT claim Issue #1 `5835504221`. PR description updated to reflect both failures.

## 3. Blocking findings

R-D-3 reopened recovery: package install persists `Ready` without registration; current refusal handler changes it to `RemoteRemovalPending`, allowing a second Recover to match/delete another unique same-name offline runner despite no registration attempt. `Configuring` persists before `config.cmd`, so a crash before execution also leaves an ID-less record that could take the same path. Review of 21 PR paths/nine correction paths found R-D-4 pre-start version probe and earlier R-D-1/R-D-2 protections source intact, but R-D-3 remains blocking.

Windows non-admin .NET 10.0.401: restore PASS, warnings-as-errors solution build FAIL with `CS0103 WizardEvent` at `tests/Grl.Integration.Tests/PortableLifecycleTests.cs:610,630`; WPF built. Both full unfiltered test commands exited 1: Core 192/192 and Presentation 58/58 ran, Integration not compiled/executed, BatchBoundary 0/11. Pinned runner v2.337.0 CLI probe and literal `git diff --check` passed. Old-head 365/365 result at `d68fc60` cannot be transferred.

## 4. Active worker and contract

Issue #16 `5835514121` is the complete correction packet. One worker different from CT/reviewer should claim on Issue #1, work locally on current office Windows with `dotnet`, fix the test import and the recovery evidence flaw on SAME `feat/grl-d-portable-lifecycle`/DRAFT PR #17, and build/test fully **before pushing**. Scope is Issue #16 Integration source/tests, with minimal presentation copy only if needed. No authority/Core/schema/template/.github/runner-pin/dependency edits. Approved conservative change: reopened records with no durably stored numeric ID cannot automatically resolve/delete by name; keep pending and guide manual repo Actions runner review. Preserve original evidence on refused Recover. Retain exact-ID/offline/nonbusy/survivor safeguards when ID is stored, and same-session re-resolution only after actual registration attempt with uncertainty failing closed. Replace old ID-less reopen deletion expectation with real composition regressions on repeat Recover and pre-execution crash.

## 5. Proof and product gates

Worker must locally pass restore, warnings-as-errors solution build, two full unfiltered solution test runs with exact counts and `git diff --check` on candidate head, then normal fast-forward push same branch, publish handoff on Issue #16 and release claim. If blocked, publish BLOCKED with no push. After push, CT dispatches a different source reviewer and a separate read-only office Windows witness at the new exact head. No hosted Actions (owner restriction), no cloud Windows environment (D33), no real-integration `WINDOWS_TESTED` or G1 during source correction.

C and E are merged; D is not. Office `grl-office` is owner-approved OD-7/D29 for later G1 on private `bohanyt/github-runner-local-exec` only after reviewed D merge. Issue #15 remains gated. G1 must cross-check actual runner log version against E metadata for in-process auto-update. Conditional post-G1 personal runner D30/one active at a time awaits Issue #18 switching proof.

## 6. Next

Dispatch ONE bounded local Windows implementation worker using Issue #16 `5835514121`. CT checks exact-head source/proof handoff, then independent rereview plus separate Windows read-only witness. No live login, registration/start, Stop Now, remote deletion, execution-repo activation, merge, G1 or personal enrollment now.

END_OF_GRL_HANDOFF key=GRL-20260925-D-RECOVERY-EVIDENCE-WINDOWS-BUILD-CORRECTION-V34 sections=6
