# github-runner-local — D corrected head awaiting independent source and Windows proofs (V35)

## 1. Phase

**D_SOURCE_REREVIEW_AND_SEPARATE_WINDOWS_PENDING; OFFICE_G1_GATED.** Same DRAFT PR #17 now has a local Windows-tested source correction. This is implementation evidence, not a different independent review or a separate Windows witness. Issue #16 CT packet `5836048062` dispatches both checks in parallel for the exact unchanged head.

## 2. Exact refs

At CT claim main `57db699dcc8e73e2838ddafbf3b9e07826ff08e3` selected V34. SAME PR #17 / `feat/grl-d-portable-lifecycle` is open/draft/unmerged at `25a38842b9665d55fa9c85b3071055149110c1ad`; old failed head `69c0b9e267819e11366729805cb61de456b53524`, original source base `be6a34872db8c059de060fafbb4188a0e39e1ce1`. GitHub compare old→new: ahead one, behind zero, four allowlisted paths (`src/Grl.Integration/LiveWizardComposition.cs`, `tests/Grl.Integration.Tests/PortableLifecycleTests.cs`, `src/Grl.App.Presentation/LiveErrorCatalog.cs`, `src/Grl.App.Presentation/WizardSession.cs`). Full PR 21 paths. Implementation worker handoff Issue #16 `5835825727` and Issue #1 release `5835830917`. CT claim Issue #1 `5836038930`.

## 3. Worker proof and limits

Worker reports ID-less reopen now blocks remote removal without a stored numeric ID; refused Recover preserves durable state and evidence. Real composition regressions include `Ready → Recover → Recover`, pre-execution `Configuring`, ID-less pending/unrelated same-name runner, identity mismatch/retry, survivor/retry, and safe persisted-ID exact removal. Missing `Grl.Core` test import resolves `WizardEvent` compilation.

On office Windows .NET 10.0.401 at exact pushed head: restore PASS, warnings-as-errors full solution build PASS (0 warnings/errors, WPF compiled), two unfiltered full-solution test runs each **382/382** (Core 192, Presentation 58, Integration 132), 0 failed/skipped; BatchBoundary **11/11**; `git diff --check` PASS. An intermediate candidate failed five new tests, was fixed and never pushed. This is worker `LOCAL_CHECKED`, not independent verification. The official RunnerContractProbe was not rerun on this head. Old-head failures `5835420513`/`5835428859` remain historical; previous old-head Windows passes likewise cannot transfer to new head.

## 4. Independent source rereview

Issue #16 `5836048062` authorizes ONE independent reviewer different from worker and CT to claim Issue #1, read main authority/packet and full PR, inspect all 21 paths/four correction paths, reproduce or trace R-D-3 ID-less reopen and repeated refusal, verify persisted-ID guards plus R-D-1/R-D-2/R-D-4 protections, and publish one exact-head PASS with Windows pending or NEEDS_D_CORRECTION on Issue #16. SDK/NuGet limits are reported, never concealed. Release claim. No source edits or self-review.

## 5. Separate office Windows witness and live gates

A separate local non-admin Windows tester/session may collect read-only evidence in parallel on clean isolated exact-head checkout: solution restore, warnings-as-errors build, two unfiltered full tests with exact counts, official pinned v2.337.0 RunnerContractProbe hash/listener/help, `git diff --check` and final clean worktree. Claim/release on Issue #1 and publish result Issue #16; even a source witness PASS is provisional `LOCAL_CHECKED`, not real-integration `WINDOWS_TESTED`. D33: no cloud Windows environment. No hosted Actions until owner re-enables.

C and E merged; D not. Owner OD-7/D29 approves office `grl-office` for later Issue #15 G1 on private `bohanyt/github-runner-local-exec` only after separate reviewed D merge. In G1 compare actual runner log version to E result metadata because in-process self-update can drift. Conditional post-G1 personal `grl-personal` remains D30 one active at a time, with Issue #18 separately proving switch/request admission. Safe Drain or server DELETE is unproved.

## 6. Next

Collect both independent source review and separate Windows read-only witness on unchanged `25a38842b9665d55fa9c85b3071055149110c1ad`. CT adjudicates both: any blocker returns to same-PR bounded correction; both PASS permit a separate merge decision, then Issue #15 live G1. No merge, live login, runner registration/start, execution-repo activation, Stop Now, G1 or personal enrollment under this V35 handoff.

END_OF_GRL_HANDOFF key=GRL-20260925-D-25A3884-SOURCE-WINDOWS-PENDING-V35 sections=6
