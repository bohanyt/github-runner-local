# github-runner-local — Checkpoint B implemented, independent review pending (V7)

## 1. Phase

**B_IMPLEMENTED / AWAITING_INDEPENDENT_REVIEW.** GRL-005 has one published fake-only candidate on one DRAFT PR. No independent B review or merge authorization exists. Evidence remains `LOCAL_CHECKED`.

## 2. Authority

Read main `AGENTS.md` → main `docs/CURRENT.md` → this entire handoff → Issue #1 latest comments → Issue #8 body and implementation comment `5810381568`. Owner authorization is Issue #1 comment `5809860108`; bounded implementation claim is `5809865391`. The Opus plan is Issue #2 comment `5807784901`, and CT review is `5807941253`. T1 continuity base is `31d823604fa21fc755a1cfbf28e9db90ab8df8ec`. A1 was merged via PR #5 at merge commit `49569022b7773c12ebd3681f2ddc7f40f9826f2e` from independently reviewed head `e21599eda1d4c40c391c2d34546e616e59645144`. Issues #4, #6 and #7 are closed. PR #3 remains open/draft and proposed.

## 3. B candidate refs

- Task: Issue #8, GRL-005. Implementation handoff: comment `5810381568`, ending `END_OF_GRL_B_IMPLEMENTATION key=GRL-005-B-IMPLEMENTATION-20260924 sections=7`.
- Branch: `feat/grl-b-wpf-shell`; base T1 `31d823604fa21fc755a1cfbf28e9db90ab8df8ec`; exact head H `bbceeeeef111d1e5dcc62690c263a43f97b93ac6`.
- DRAFT PR #9 targets main, is open and unmerged, and lists exactly 19 changed paths. The implementer did not review it. PR #9 is not merge-authorized.
- Implementation commits used the instructed public identity per command without changing global git config.

## 4. Scope summary and exact changed files

A .NET 10 x64 WPF preview renders Welcome and all A1 states through one Core state machine. User commands follow Core transitions. The WPF-free presentation library uses only fake adapters and fictional data; a separate UIA smoke console project is outside the solution. The changed paths are:

1. `GitHubRunnerLocal.sln`
2. `docs/dev/BUILD.md`
3. `docs/dev/UI-SMOKE.md`
4. `src/Grl.App.Presentation/ErrorCatalog.cs`
5. `src/Grl.App.Presentation/FakeAdapters.cs`
6. `src/Grl.App.Presentation/FakeScenario.cs`
7. `src/Grl.App.Presentation/Grl.App.Presentation.csproj`
8. `src/Grl.App.Presentation/WizardPages.cs`
9. `src/Grl.App.Presentation/WizardSession.cs`
10. `src/Grl.App/App.xaml`
11. `src/Grl.App/App.xaml.cs`
12. `src/Grl.App/Grl.App.csproj`
13. `src/Grl.App/MainWindow.xaml`
14. `src/Grl.App/MainWindow.xaml.cs`
15. `src/Grl.App/app.manifest`
16. `tests/Grl.App.Presentation.Tests/Grl.App.Presentation.Tests.csproj`
17. `tests/Grl.App.Presentation.Tests/PresentationTests.cs`
18. `tests/Grl.App.UiSmoke/Grl.App.UiSmoke.csproj`
19. `tests/Grl.App.UiSmoke/Program.cs`

## 5. Evidence

Non-elevated local Windows shell; SDK 10.0.401 and Microsoft.WindowsDesktop.App 10.0.12. `dotnet restore GitHubRunnerLocal.sln` exit 0 (package restore may contact NuGet). `dotnet build GitHubRunnerLocal.sln -warnaserror` exit 0, 0 warnings/errors. Both `dotnet test GitHubRunnerLocal.sln --no-restore` and `dotnet test GitHubRunnerLocal.sln --no-restore --no-build` exit 0: Grl.Core.Tests 192/192 and Grl.App.Presentation.Tests 42/42, with 0 failed/skipped in each. `dotnet build tests/Grl.App.UiSmoke/Grl.App.UiSmoke.csproj -warnaserror` exit 0, 0 warnings/errors. Direct Debug smoke executable exit 0 in 11.5 seconds:

| Case | Result |
| --- | --- |
| S1 launch/banner/no-UAC/clean close | PASS |
| S2 keyboard happy path with UIA focus checks | PASS |
| S3 blocked/error paths and Cancel cleanup | PASS |
| S4 accessible names on visited focusable controls | PASS |
| S5 relaunch at Welcome, no product data directories | PASS |

`git diff --name-only <T1>..HEAD` listed exactly the 19 allowed paths above; `git diff --check <T1>..HEAD` exit 0; product-source forbidden-token grep had 0 hits; no app process remained. Label: **LOCAL_CHECKED**, qualified as fake-mode UIA smoke, worker Windows desktop, single DPI. Multi-DPI, High Contrast, screen readers, other machines, and real integration are **NOT TESTED**.

## 6. Known A1 core gaps and open Checkpoint C questions

Eight state-model gaps remain: Welcome is a UI-only gate; sign-in network error overlays SignInPolling; download, verify, extract and configure failures overlay their current Installing states; account mismatch after SignedIn overlays SignInSignedIn; local disconnect failure overlays DisconnectDone. All overlays keep the current Core transition commands. A1 orders Preflight → SignIn → Scope → Location → Installing, while plan §D1 placed location before preflight. Any Core additions or ordering change need a separately authorized checkpoint.

## 7. Constraints

No GitHub-hosted Actions or workflow changes. Product code has no live GitHub/auth/runner integration, network, child-process, registry, or file writes. No Grl.Core, schemas, fixtures, or authority-document edits were made on the B branch. No App/OAuth/execution-repo, UAC/service, machine-policy/security, release, or later-checkpoint work occurred. PR #3 remains untouched. The bounded implementation lease is concluding with this continuity transaction; Issue #1 holds its release record. `CONTRIBUTING.md` phase wording and the final paragraph of `docs/control-tower/ROLE.md` remain stale outside this transaction's allowed files.

## 8. Next

Owner/CT creates ONE independent exact-head review packet for PR #9 head `bbceeeeef111d1e5dcc62690c263a43f97b93ac6`. The implementing session must not review. PR #9 is not merge-authorized. Checkpoint C remains unauthorized.

END_OF_GRL_HANDOFF key=GRL-20260924-B-IMPLEMENTED-REVIEW-PENDING-V7 sections=8
