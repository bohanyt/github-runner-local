# github-runner-local — D R-D-3/R-D-4 correction review and Windows proof pending (V33)

## 1. Phase

**D_CORRECTION_REREVIEW_AND_WINDOWS_PENDING; OFFICE_G1_GATED.** The same draft PR has a new source correction, not yet independently reviewed or .NET-tested on the new head. Issue #16 CT packet `5835191415` authorizes two separate read-only proofs, source review and current-office Windows, in parallel. Their results remain provisional until CT combines them at unchanged head.

## 2. Exact refs and worker evidence

At previous CT dispatch, main `0dbb98e2dd87fbbc5be26e31a04d2b930d577f62` selected V32. SAME DRAFT PR #17 / `feat/grl-d-portable-lifecycle` is open/unmerged at `69c0b9e267819e11366729805cb61de456b53524`; old head `d68fc60e29726df25733acb8f00f3ea3ce104ac6`; original source base `be6a34872db8c059de060fafbb4188a0e39e1ce1`. GitHub compare confirms ahead 17, behind 0, nine correction paths inside Issue #16 source/test allowlist and full PR 21 paths. Worker handoff Issue #16 `5835153272`, Issue #1 claim/release `5834812433`/`5835155758`.

Worker reports bounded R-D-3 numeric-ID recovery and R-D-4 pre-start CLI version verification, plus N-2 close-warning copy. These are source implementation claims, not independent verdict. Worker environment lacked dotnet/C# compiler and shell DNS: restore, warnings-as-errors build, full tests twice and literal `git diff --check` **NOT RUN**. Structural/whitespace checks do not substitute for compilation.

## 3. Prior and current proof boundaries

Previous independent review Issue #16 `5834704379` found R-D-3/R-D-4 blocking at `d68fc60`, with R-D-1 closed under the fail-closed no-Drain contract and R-D-2 main paths closed. Prior office Windows read-only witness `5834244169` passed restore/build, 365/365 full tests twice, 11/11 BatchBoundary and pinned v2.337.0 CLI probe, only on `d68fc60`. The earlier Linux claim of ten excluded batch tests was an arithmetic error: eleven are present. Nothing on old head is a new-head test PASS. The PR description now states the current gates and no-Drain contract rather than stale old-head results.

## 4. Active source review

Issue #16 `5835191415` dispatches ONE independent reviewer different from worker and CT for exact `69c0b9e`, with Issue #1 claim/release. Review all 21 PR paths and nine-path correction, especially R-D-3/R-D-4/fail-closed recovery and version metadata, regression tests, and previous R-D-1/R-D-2 safeguards. Publish `D_SOURCE_REVIEW_PASS_WINDOWS_PENDING` or `NEEDS_D_CORRECTION`; report .NET/NuGet limits honestly. No implementation edit, self-review, merge or live operation.

## 5. Active office Windows proof and live gates

A separate tester on the current office Windows laptop may gather read-only, non-live, clean-checkout evidence in parallel: .NET 10 restore, warnings-as-errors solution build, two full unfiltered solution tests with exact counts, pinned RunnerContractProbe and `git diff --check`. Claim/release on Issue #1 and publish Issue #16 result at exact `69c0b9e`. A source proof PASS is `LOCAL_CHECKED`, **not** `WINDOWS_TESTED` real-integration acceptance. No cloud Windows environment (D33); no GitHub-hosted Actions until owner re-enables.

C and E are merged; D is not. Owner OD-7/D29 authorized office `grl-office` later for G1 on private `bohanyt/github-runner-local-exec`, but Issue #15 is gated until reviewed D merge. G1 must cross-check actual runner log version versus E result metadata because auto-update may happen inside one running child. Conditional post-G1 personal `grl-personal` remains D30, one active at a time, with Issue #18 separately proving a safe switching/request fence. Safe Drain/server DELETE is not established.

## 6. Next

Collect independent source review and separate Windows read-only witness on the same unchanged new head. CT adjudicates both; if one fails, issue one bounded same-PR correction, if both pass, decide separate merge readiness and only then dispatch Issue #15 office live G1. No live Device Flow, runner registration/start, execution-repo activation, Stop Now, personal enrollment or merge under this V33 handoff.

END_OF_GRL_HANDOFF key=GRL-20260925-D-RD3-RD4-REREVIEW-WINDOWS-PENDING-V33 sections=6
