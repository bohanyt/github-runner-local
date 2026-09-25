# CURRENT — github-runner-local

Updated: 2026-09-25. Phase: **D_SOURCE_REREVIEW_READY_WINDOWS_PENDING; OFFICE_G1_GATED**.

## Authority

- Canonical branch: `main`; owner-designated successor Control Tower coordinates on Issue #1.
- Current handoff: `docs/control-tower/handoffs/GRL-20260925-D-SOURCE-REREVIEW-READY-WINDOWS-PENDING-V31.md`.
- Required sentinel: `END_OF_GRL_HANDOFF key=GRL-20260925-D-SOURCE-REREVIEW-READY-WINDOWS-PENDING-V31 sections=7`.

## Source status

C and E are independently reviewed and merged (C `451d3ab889f9f63f45ebf90ba54ddece7b60e6eb`, E `a296f3f9dfa362f6a53d20ff81a46eef7a519819`). D is **NOT merged**. SAME DRAFT PR #17 branch `feat/grl-d-portable-lifecycle` advanced by normal fast-forward from `b14ff5fa108a4a1891efbc03f700bbffaaa3cfcf` to **`d68fc60e29726df25733acb8f00f3ea3ce104ac6`**. Original source base `be6a34872db8c059de060fafbb4188a0e39e1ce1`. Correction commit changed 14 allowlisted paths; full PR has 21 paths.

Worker Issue #16 handoff `5829540017`, Issue #1 release `5829553937`: `D_SOURCE_CORRECTION_REVIEW_READY_WINDOWS_PENDING`. Linux .NET 10.0.401 restore/cross-build passed warnings-as-errors; two filtered solution test runs each **354/354** (Core 192, Presentation 56, Integration 106), 0 failures/skips among executed cases; ten Windows batch-boundary tests were excluded. This is worker evidence only, not independent review or Windows acceptance.

## Active task

Issue #16 CT packet **`5832173544`** dispatches ONE different independent source reviewer of exact PR head `d68fc60e29726df25733acb8f00f3ea3ce104ac6` and all 21 paths. Reviewer must recheck R-D-1/R-D-2 under revised fail-closed contract plus product gaps from `5828855949`, report `D_SOURCE_REVIEW_PASS_WINDOWS_PENDING` or `NEEDS_D_CORRECTION`, release claim, then stop. No self-review or merge.

## Subsequent gates

D33: no cloud Windows environment exists for this project in foreseeable future. After exact-head source PASS, the current office Windows laptop supplies a **separate read-only, non-live** new-head restore/build/twice-full-tests/runner CLI witness before CT/owner merge. Only after reviewed D merge may Issue #15 run live G1. Office `grl-office` is approved by OD-7/D29 for the private same-repo harmless fixture; G1 is not yet run. G1 packet `5828862963` requires four template variables, Git PATH/App permissions and staged proof. Conditional personal `grl-personal` after G1 PASS remains D30, with one active runner at a time and a separately proved switching procedure in Issue #18. No automatic safe Drain or server DELETE is established.

## Standing boundaries

No GitHub-hosted Actions, live login/runner registration/start or private template activation under D source review; no merge before Windows gate; no Stage-2/cross-repo credentials, service/UAC, personal activation before its conditions, or signing/release.

## Next

Dispatch the independent exact-head source review from Issue #16 `5832173544`; then CT handles its disposition and the office-laptop read-only Windows gate.
