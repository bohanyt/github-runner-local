# Checkpoint B UI smoke

Run only in an ordinary, unlocked Windows desktop session with the .NET 10 WindowsDesktop runtime. Do not elevate, change display settings, or use a keep-awake tool. The app and smoke harness use fictional data only.

From the repository root:

```text
dotnet build GitHubRunnerLocal.sln -warnaserror
dotnet build tests/Grl.App.UiSmoke/Grl.App.UiSmoke.csproj -warnaserror
dotnet run --project tests/Grl.App.UiSmoke/Grl.App.UiSmoke.csproj --no-build
```

If the runtime is installed outside the standard x64 location, set `DOTNET_ROOT` and `DOTNET_ROOT_X64` to that installation in the launching shell. The harness launches only the built `GitHubRunnerLocal.exe` with `UseShellExecute=false`. It uses UI Automation to inspect windows and `SendKeys` for the keyboard happy path. It closes or kills only app processes it started, captures no screenshots, waits no more than 30 seconds per condition, and caps the campaign at 10 minutes. If app control blocks the executable, record `BLOCKED_APP_CONTROL` and stop. If the desktop is unavailable, record `BLOCKED_SESSION` and stop. Never relaunch through a different host or path to work around a block.

The harness reports `PASS`, `FAIL`, `BLOCKED_SESSION`, `BLOCKED_APP_CONTROL`, or `NOT_RUN` for each case:

- S1: launch without a UAC prompt, preview banner, clean close with exit code 0.
- S2: keyboard only from Welcome acknowledgement through RunnerIdle; UIA verifies focus.
- S3: blocked preflight reason and Retry, install verification error with Cancel cleanup, sign-in network error overlay.
- S4: accessible names on focusable controls of visited pages.
- S5: relaunch starts at Welcome, with no product data directories created.

The fourteen scenarios are `HappyPath`, `PreflightBlocked`, `SignInDenied`, `SignInExpired`, `SignInWrongAccount`, `SignInNetworkError`, `ScopeNoAdmin`, `ScopeNotPrivate`, `LocationRedirected`, `LocationNetwork`, `LocationTooLong`, `InstallVerifyFailed`, `RunnerDegraded`, and `DisconnectRemoteUnavailable`. Pass `--scenario <Name>` to select one. Unknown or extra arguments safely select HappyPath and show a notice without echoing input.

## Owner manual keyboard checklist

1. Launch the built exe without elevation. Read the permanent preview banner and Welcome trust disclosure. Confirm Continue is disabled before checking acknowledgement.
2. Use Tab, Shift+Tab, Space and Enter to acknowledge and reach RunnerIdle. Confirm focus moves visibly and the page titles, explanations, simulated device code, fictional target and status remain readable.
3. On a page with Cancel, press Esc and confirm the transition and any simulated cleanup text. Retry the fake blocked scenarios where available.
4. Expand Preview simulation controls by keyboard. Confirm runner event buttons enable only when the current state allows that event. Inspect the failure overlay controls in relevant states.
5. On RunnerIdle, use the local removal preview and confirm it says deletion plan only. Close and relaunch; confirm Welcome returns.

The automated campaign checks one worker desktop at one DPI. Multi-DPI, High Contrast, screen readers, and other machines remain `NOT TESTED` for Checkpoint B.
