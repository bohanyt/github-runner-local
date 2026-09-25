# CURRENT — github-runner-local

Updated: 2026-09-25. Phase: **D_SOURCE_RECOVERY_EVIDENCE_AND_WINDOWS_BUILD_CORRECTION; OFFICE_G1_GATED**.

## Authority

- Canonical branch: `main`; owner-designated successor Control Tower coordinates on Issue #1.
- Current handoff: `docs/control-tower/handoffs/GRL-20260925-D-RECOVERY-EVIDENCE-WINDOWS-BUILD-CORRECTION-V34.md`.
- Required sentinel: `END_OF_GRL_HANDOFF key=GRL-20260925-D-RECOVERY-EVIDENCE-WINDOWS-BUILD-CORRECTION-V34 sections=6`.

## Source status and failures

C and E independently reviewed/merged. D **NOT merged**. SAME DRAFT PR #17 / `feat/grl-d-portable-lifecycle` at `69c0b9e267819e11366729805cb61de456b53524`; original source base `be6a34872db8c059de060fafbb4188a0e39e1ce1`. Independent Issue #16 source review `5835420513`: **NEEDS_D_CORRECTION**, reopened recovery can elevate an ID-less pre-registration state after refused Recover and delete an unrelated same-name runner. Separate office Windows read-only witness `5835428859`: **D_WINDOWS_READONLY_FAIL**; restore PASS, warnings-as-errors build FAIL with two `CS0103 WizardEvent` Integration test errors, both full test commands exit 1. Core 192/192 and Presentation 58/58 ran, Integration and 11 BatchBoundary cases did not; probe and diff check PASS. Both claims released Issue #1. Older-head Windows 365/365 is historical, not transferable. R-D-4 source probe and R-D-1/R-D-2 main protections remain intact per review.

## Active task

Issue #16 CT correction packet `5835514121` dispatches ONE bounded implementation worker **on current office Windows with .NET 10.0.401**, SAME branch/PR, expected old head `69c0b9e267819e11366729805cb61de456b53524`. Fix recovery evidence/state, refuse automatic name-only deletion on ID-less reopen, add repeat-Recover/crash regressions, fix `WizardEvent` compilation, and pass full Windows build/tests **before push**. Claim/release on Issue #1. No authority/Core/schema/template/.github/runner pin/dependency edits. Different exact-head source reviewer plus separate new-head Windows witness still follow before CT merge decision.

## Live gates and next

No hosted Actions until owner re-enables. No live login, runner registration/start, private execution-repo activation, remote removal, Stop Now, merge or G1 during source correction. Office `grl-office` remains owner-approved OD-7/D29 for later Issue #15 G1 on private `bohanyt/github-runner-local-exec` only after reviewed D merge; verify actual runner log version vs E metadata for in-process auto-update. Conditional post-G1 personal `grl-personal` D30 with one active runner at a time awaits Issue #18 safe switching proof.

Dispatch the local worker via Issue #16 `5835514121`; CT then handles worker handoff, independent source rereview and new-head office read-only witness.
