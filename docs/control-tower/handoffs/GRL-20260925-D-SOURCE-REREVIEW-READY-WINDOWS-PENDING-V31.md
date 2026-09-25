# github-runner-local — D source rereview ready, Windows pending (V31)

## 1. Phase

**D_SOURCE_REREVIEW_READY_WINDOWS_PENDING; OFFICE_G1_GATED.** D correction was pushed to the same draft PR. This is an implementation handoff, not an independent PASS or merge approval.

## 2. Exact refs

At CT claim, main `70fa693c8cb7e9f3a56bb8fe8ef75723c6d6bfad` selected V30. DRAFT PR #17 stays open/draft/unmerged on `feat/grl-d-portable-lifecycle`, old head `b14ff5fa108a4a1891efbc03f700bbffaaa3cfcf`, new head `d68fc60e29726df25733acb8f00f3ea3ce104ac6`; original source base `be6a34872db8c059de060fafbb4188a0e39e1ce1`. Source correction commit changes 14 paths inside Issue #16 allowlist; full PR now has 21 paths. No Core/schema/template/root workflow/runner pin/dependency delta was reported. Commit file list matched handoff list at CT triage; no CT source verdict.

## 3. Worker handoff and proof

Issue #16 `5829540017`: `D_SOURCE_CORRECTION_REVIEW_READY_WINDOWS_PENDING`; Issue #1 worker release `5829553937`. Worker reports Drain unavailable/no implicit stop; unregister rejects a live owned process with exact identity gating; register/resume failure catches preserve ownership; non-secret recovery file and reopen-only mode; E-required runner metadata in child environment; truthful live UI.

Linux .NET 10.0.401 solution restore and warnings-as-errors cross-build passed (0 warnings/errors). Two filtered full-solution test runs each passed **354/354** (Core 192, Presentation 56, Integration 106), 0 failures/skips among executed cases. Ten Windows batch-boundary cases were explicitly excluded. Earlier intermediate Linux failure was reported and fixed before final runs. No new-head Windows runtime, batch/probe or live G1 proof exists. Old-head 352/352 Windows result is historical only.

## 4. Independent review task

Issue #16 CT exact-head packet `5832173544` directs ONE different reviewer to inspect all 21 PR files and 14 correction paths, R-D-1/R-D-2 plus recovery/security/integration seams, and run compatible independent source proof. Allowed source result `D_SOURCE_REVIEW_PASS_WINDOWS_PENDING` or `NEEDS_D_CORRECTION`; no source PASS may claim `WINDOWS_TESTED`, graceful Drain, merge or G1. Reviewer claims and releases on Issue #1.

## 5. Windows and live gates

D33 owner constraint: no cloud Windows environment in foreseeable future. After an exact-head source PASS, one **read-only/non-live** proof on the current office Windows laptop must restore, warnings-as-errors build, run full solution tests twice with exact counts, perform the pinned runner read-only CLI witness and report WPF/Windows limitations. CT then decides a separate merge gate. Only after D merge can Issue #15's office G1 run real Device Flow, enrollment and harmless same-repo fixture. The private execution repo remains inactive until that packet. No GitHub-hosted Actions.

## 6. Product objective

First prove a functioning office `grl-office` portable trusted-code-only runner on the approved private execution repo; G1 is not complete now. Conditional post-G1 `grl-personal` with one active at a time remains approved in D30. Issue #18's server-delete/422 and request-level fence design is unproved and separate from this source rereview. No personal activation or automatic laptop switching yet.

## 7. Next

Dispatch ONE independent exact-head source reviewer under Issue #16 `5832173544`. CT incorporates the result. If source PASS, prepare the office-laptop **read-only Windows proof** before merge; if NEEDS_D_CORRECTION, use same branch/PR for bounded correction. No live action or merge under this handoff.

END_OF_GRL_HANDOFF key=GRL-20260925-D-SOURCE-REREVIEW-READY-WINDOWS-PENDING-V31 sections=7
