# A1 core contracts: local build

Use the official .NET 10 SDK selected by root global.json and run from the repository root:

1. dotnet --info
2. dotnet build -warnaserror
3. dotnet test

The first build may restore the xUnit test packages from NuGet. The test assembly itself uses only in-memory models, fake clock/filesystem metadata, and the checked-in contract fixtures; test execution makes no network calls. To demonstrate that separation after restore, dotnet test --no-restore must also pass.

## Contract conventions

- Request comments start with the exact grl-request v1 HTML marker and contain one JSON fence. The SHA-256 digest covers the UTF-8 bytes of the opening fence, JSON, closing fence, and intervening line endings, excluding the marker and trailing whitespace.
- Request admission needs caller-supplied allowlisted repositories and profiles, the authenticated comment creation timestamp, and an injected clock. The schema covers payload shape; the validator checks duplicate keys, envelope bytes, allowlists, expiry and per-profile timeout.
- Result JSON keeps the workflow commit SHA and profile-definition blob SHA in separate fields. They may happen to have equal values. A normal PASS also requires non-elevated runner evidence, positive required tests, zero failed/errored counts, and zero check exits. Reporting completeness comes from a separate observer record matched to the request, ACK and run.
- The files under tests/fixtures/contracts are invented contract examples. They do not represent a GitHub run, an installed runner, or machine acceptance.
- The valid request fixture is base64-encoded to preserve its exact LF envelope bytes and body digest across Windows Git line-ending conversion.

The Windows path policy applies lexical Windows rules and consumes fake metadata through IFileSystemView; it performs no host filesystem operations or deletion. These A1 checks establish LOCAL_CHECKED core behavior only.

## Checkpoint B

The solution adds a framework-dependent x64 WPF preview app, a WPF-free presentation library, and presentation tests. The separate UI smoke console project is intentionally outside the solution. Build and test from the repository root with the .NET 10 SDK and WindowsDesktop runtime:

```text
dotnet --list-sdks
dotnet --list-runtimes
dotnet restore GitHubRunnerLocal.sln
dotnet build GitHubRunnerLocal.sln -warnaserror
dotnet test GitHubRunnerLocal.sln --no-restore
dotnet test GitHubRunnerLocal.sln --no-restore --no-build
dotnet build tests/Grl.App.UiSmoke/Grl.App.UiSmoke.csproj -warnaserror
```

The app is a preview with fake adapters and fictional data only. To inspect a scenario, run the built `GitHubRunnerLocal.exe` with `--scenario <Name>`; the default is `HappyPath`. The named scenarios and the UI smoke procedure are in [UI-SMOKE.md](UI-SMOKE.md). If .NET 10 was installed to a private tool directory, set both `DOTNET_ROOT` and `DOTNET_ROOT_X64` in the launching shell so the framework-dependent x64 app host finds that runtime. These checks establish `LOCAL_CHECKED` evidence only.

## Checkpoint C

The solution includes the plain `net10.0` Integration library and deterministic fake-HTTP/synthetic-archive tests. It does not connect these adapters to the WPF app. Run the solution commands above in order; the Integration tests use no live GitHub network.

The separate `tests/Grl.RunnerContractProbe` project is intentionally outside the solution. On a non-elevated Windows machine with .NET 10, build it with `dotnet build tests/Grl.RunnerContractProbe/Grl.RunnerContractProbe.csproj -warnaserror`, then from the repository root run `dotnet run --project tests/Grl.RunnerContractProbe/Grl.RunnerContractProbe.csproj --no-build -- runner-pins.json`. It downloads only the reviewed official Windows x64 runner, checks its SHA-256 before extraction, and invokes only `Runner.Listener.exe --version` and `config.cmd --help`. It creates and removes one unique temporary directory. It never registers or starts a runner. The pin is initial-install metadata; GitHub's normal runner auto-update remains enabled for later live work.
