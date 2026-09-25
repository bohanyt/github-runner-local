# github-runner-local — D failed-config provenance correction (V36)

## 1. Phase

**D_CONFIGURE_FAIL_PROVENANCE_CORRECTION; OFFICE_G1_GATED.** Exact-head independent source review found an unsafe ID binding after a failed `config.cmd`; separate Windows read-only proof passed at the same SHA. Issue #16 CT packet `5836392337` narrows the automatic compensation contract to support safe, bounded G1.

## 2. Exact refs and results

At CT claim main `6641aec7c0b372af880012a0def7436c51b00aa0` selected V35. SAME DRAFT PR #17 / `feat/grl-d-portable-lifecycle` remains open/unmerged at `25a38842b9665d55fa9c85b3071055149110c1ad`; source base `be6a34872db8c059de060fafbb4188a0e39e1ce1`. Independent source review Issue #16 `5836285914`: **NEEDS_D_CORRECTION**, claim released Issue #1 `5836293286`. Separate non-admin office Windows witness Issue #16 `5836279999`: **D_WINDOWS_READONLY_PASS_PROVISIONAL**, claim released Issue #1 `5836284576`: restore and warnings-as-errors build PASS, 0 warnings/errors, two full unfiltered solution runs each 382/382 (Core 192, Presentation 58, Integration 132), 0 failed/skipped, BatchBoundary 11/11, official runner v2.337.0 hash/listener/help probe PASS, clean diff/worktree. This is non-live `LOCAL_CHECKED`, not real-integration `WINDOWS_TESTED`. CT claim Issue #1 `5836382741`. PR body updated to current blocker.

## 3. Safety finding and CT contract

At `PortableRunnerLifecycle.RegisterAndStartAsync`, the name is absent on pre-check, then `registrationMayExist` is set before `process.ExecuteAsync(config.cmd)`. If the config process fails to start and another actor registers the same name before the next list, current code persists that other numeric ID and makes later exact-ID removal possible after reopen. Numeric ID/name/offline/status rechecks prove present identity, not installation provenance. Previous ID-less reopen refusal and R-D-1/R-D-2/R-D-4 safeguards remain source intact, but this pre-start error path is blocking.

Issue #16 `5836392337` revises the bounded contract: after any failed-start, nonzero, timed-out, cancelled or uncertain `config.cmd`, do not automatically bind ID from a new name match or remove remotely. If successful config cannot promptly establish one unique exact ID under the same bounded operation, manual inspection in repo Actions runner settings is the safe outcome. Successful configure plus verified ID stays the G1 path. Error-path automatic cleanup is deferred; manual reconciliation is accepted rather than risking another runner. No new broad recovery subsystem or request switching in D.

## 4. One worker on office Windows

One worker different from CT/reviewer claims on Issue #1, fresh-reads main authority and Issue #16 full packet, starts from exact `25a38842b9665d55fa9c85b3071055149110c1ad` on SAME branch/PR, and changes minimally within Issue #16 Integration/tests plus necessary copy. Regression must cover unrelated same-name registration after failed-start, nonzero/timeout/cancellation, ID-less persisted reopen/repeated Recover, and normal successful unique ID. Use real composition to prove no auto DELETE/removal token/CLI on failure. No Core/schema/template/.github/runner-pin/dependency/authority edits. Local Windows SDK 10.0.401 restore, warnings-as-errors build, twice-full unfiltered solution tests and diff check must PASS before fast-forward push; otherwise BLOCKED without push. Publish exact-head handoff Issue #16, release claim Issue #1, stop. Different independent source review and separate new-head Windows witness still follow.

## 5. Product objective and later gates

C/E merged; D not. Office `grl-office` approved OD-7/D29 for later Issue #15 G1 on private `bohanyt/github-runner-local-exec` once D reviewed and merged. G1 checks actual log runner version versus E result metadata to detect in-process update. No hosted Actions until owner re-enables; no cloud Windows (D33). Personal `grl-personal` D30 conditional after G1 PASS, one active at a time, with switching/request fence separately proved under Issue #18; no safe automatic Drain or server DELETE established.

## 6. Next

Dispatch one bounded local Windows D correction from Issue #16 `5836392337`; CT adjudicates worker handoff, different source rereview and separate new-head office read-only proof. Do not merge, run live login/runner registration/start/removal, activate execution repo, perform Stop Now, G1 or personal enrollment now.

END_OF_GRL_HANDOFF key=GRL-20260926-D-CONFIGURE-FAIL-PROVENANCE-CORRECTION-V36 sections=6
