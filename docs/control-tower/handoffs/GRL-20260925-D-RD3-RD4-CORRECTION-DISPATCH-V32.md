# github-runner-local — D source needs R-D-3/R-D-4 correction (V32)

## 1. Phase

**D_SOURCE_NEEDS_RD3_RD4_CORRECTION; OFFICE_G1_GATED.** Successor Control Tower accepted independent source review `NEEDS_D_CORRECTION` on Issue #16 `5834704379` and dispatched one bounded correction on Issue #16 `5834758700`. This handoff does not authorize merge or live acceptance.

## 2. Exact refs and evidence

At CT adjudication, main `51118cf6375f8226b137acc64bcb4ce09371da00` selected V31. SAME DRAFT PR #17 is open/unmerged on `feat/grl-d-portable-lifecycle` at `d68fc60e29726df25733acb8f00f3ea3ce104ac6`; original source base `be6a34872db8c059de060fafbb4188a0e39e1ce1`. Earlier correction from `b14ff5fa108a4a1891efbc03f700bbffaaa3cfcf` touched 14 allowlisted paths; full PR has 21 paths. Source reviewer claim Issue #1 `5834559234` was released `5834706488`. CT claim `5834748295`.

Separate office Windows read-only witness Issue #16 `5834244169`: non-elevated .NET 10.0.401 restore and warnings-as-errors build PASS; two unfiltered solution runs 365/365 each, 0 failures/skips; 11/11 BatchBoundary; official runner v2.337.0 hash, listener version and `config.cmd --help` PASS; no live action. This witness belongs only to `d68fc60` and must be renewed at a changed head before merge. The worker's previous "10 excluded" was an arithmetic error; 11 is correct and no coverage is lost.

## 3. Independent review disposition

R-D-1 closed under revised fail-closed contract: Drain unavailable; no implicit runner-process kill; explicit Stop Now only with warning. R-D-2 main recovery/reopen paths closed. Two new blockers remain: R-D-3 unbound runner ID after partial configure or transient list failure can permanently strand recovery, with misleading retry copy; R-D-4 Resume reuses an install-time version despite auto-update, risking incorrect E result metadata. The independent reviewer demonstrated both with source trace/harness, but cloud NuGet 403 prevented its solution tests; Windows evidence is separate. Nonblocking inaccurate close copy N-2 and in-process version drift N-3 are recorded in the review.

## 4. Active correction

Issue #16 CT packet `5834758700` is the complete bounded implementation contract. ONE worker different from CT/reviewer should claim on Issue #1, reread authority, correct only R-D-3/R-D-4 inside Issue #16 allowlisted source/tests on the SAME branch/PR, run source proof and publish a handoff, release, then stop. No force push, new PR, authority/Core/schema/template/.github/runner-pin/dependency change. Current expected old head `d68fc60e29726df25733acb8f00f3ea3ce104ac6`. Independent rereview of the resulting exact head and fresh office Windows read-only full proof are both required before any separate merge decision.

## 5. Product and live gates

C and E are merged; D is not. Owner OD-7/D29 authorized office `grl-office` as portable trusted-code-only target for later G1 on private `bohanyt/github-runner-local-exec`, but G1 Issue #15 stays gated until reviewed D merge. D33: no cloud Windows environment; use current office laptop only for the later read-only exact-head proof. No GitHub-hosted Actions until owner re-enables. After G1 PASS, conditional personal `grl-personal` remains D30; only one active runner at a time, and Issue #18 owns a separately proved switching contract. Do not infer safe Drain, safe server DELETE, or request-level admission fence from current evidence.

## 6. Next

Dispatch ONE same-branch bounded worker using Issue #16 `5834758700`; on handoff, CT checks exact scope/results, dispatches a different source reviewer, then asks for fresh read-only Windows witness at reviewed unchanged head. No live login, registration/start, execution-repo activation, merge, G1, or personal enrollment now.

END_OF_GRL_HANDOFF key=GRL-20260925-D-RD3-RD4-CORRECTION-DISPATCH-V32 sections=6
