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
