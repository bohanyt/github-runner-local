# github-runner-local — D corrected provenance awaiting source and Windows proofs (V37)

## 1. Phase

**D_PROVENANCE_REREVIEW_AND_SEPARATE_WINDOWS_PENDING; OFFICE_G1_GATED.** Same DRAFT PR #17 has one new source correction, locally built/tested on the office Windows laptop; independent source and separate Windows read-only proofs are pending under Issue #16 CT dispatch `5840393630`.

## 2. Exact refs and worker scope

At CT claim, main `c0495f87a54a5e6b6e54da5640d4d3c886587ba4` selected V36. SAME open/draft/unmerged PR #17 / `feat/grl-d-portable-lifecycle` at `82f1722902e48635bc5e2a55c77a274a1e6e29cc`; old head `25a38842b9665d55fa9c85b3071055149110c1ad`; source base `be6a34872db8c059de060fafbb4188a0e39e1ce1`. GitHub compare: one commit ahead, zero behind, three allowlisted paths only: `src/Grl.Integration/PortableRunnerLifecycle.cs`, `tests/Grl.Integration.Tests/PortableLifecycleTests.cs`, `src/Grl.App.Presentation/LiveErrorCatalog.cs`. Full PR 21 paths. Worker Issue #16 handoff `5840372215`, Issue #1 claim/release `5840304808`/`5840376225`. CT claim `5840388142`.

## 3. Source contract and existing evidence

Previous independent source review Issue #16 `5836285914` found a race in which failed-to-start `config.cmd` could cause an unrelated new same-name runner ID to be persisted and later deleted; the separate old-head Windows witness passed but could not close this. CT revised Issue #16 packet `5836392337`: automatic ID binding only after a successful configuration and bounded unique exact-name lookup; failed, uncertain or ambiguous cases stay ID-less pending for manual repository runner inspection. The worker reports this behavior plus fake lifecycle→durable record→real reopen repeated-Recover regressions for StartupFailed, nonzero, timeout and cancellation; successful unique-ID path remains.

Office Windows worker proof on pushed exact new head: restore PASS; warnings-as-errors solution build PASS with 0 warnings/errors, WPF compiled; two full unfiltered solution test runs each **384/384** (Core 192, Presentation 58, Integration 134), 0 failures/skips, BatchBoundary **11/11**, `git diff --check` PASS. This is implementation `LOCAL_CHECKED`, not independent source review. The official RunnerContractProbe was not rerun on new head; prior probe is historical.

## 4. Independent source review

Issue #16 `5840393630` dispatches ONE reviewer different from worker/CT. Claim Issue #1; inspect all 21 PR files and three-path correction; independently test/trace unrelated same-name actor after initial no-collision with StartupFailed/nonzero/timeout/cancelled config; verify no post-failure ID attribution or remote removal across reopen/repeated Recover; verify successful unique-ID path and R-D-1/R-D-2/R-D-4 guards. Publish exact-head PASS with Windows pending or NEEDS_D_CORRECTION on Issue #16 and release claim. No self-review/source edits.

## 5. Separate office Windows witness and live gates

A separate non-admin Windows tester/session may gather read-only evidence in parallel at exact unchanged `82f1722`: solution restore, warnings-as-errors build, two unfiltered full solution tests with exact counts, official pinned v2.337.0 RunnerContractProbe hash/listener/help, diff/status. Issue #1 claim/release and Issue #16 result. This remains `LOCAL_CHECKED`, not real-integration `WINDOWS_TESTED`. No cloud Windows D33; no hosted Actions until owner re-enables.

C/E merged; D not. Owner OD-7/D29 approves office `grl-office` for later G1 on private `bohanyt/github-runner-local-exec`, only after reviewed D merge. G1 must compare actual runner log version with E metadata for in-process self-update. Conditional personal `grl-personal` D30 after G1 PASS with one active at a time; Issue #18 separately proves switching/request fence. Safe Drain/server DELETE is not established.

## 6. Next

Collect both separate exact-head source and Windows results and CT adjudicate. If both PASS, separately decide merge readiness, then stage Issue #15 live office G1; if either fails, return to minimal same-PR correction. No live login, runner registration/start/removal, GitHub job, private execution-repo activation, Stop Now, hosted Actions, merge, G1 or personal enrollment under V37.

END_OF_GRL_HANDOFF key=GRL-20260926-D-82F1722-SOURCE-WINDOWS-PENDING-V37 sections=6
