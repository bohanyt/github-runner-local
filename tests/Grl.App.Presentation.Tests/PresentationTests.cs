using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Grl.App.Presentation;
using Grl.Core;

namespace Grl.App.Presentation.Tests;

public sealed class PresentationTests
{
    private static readonly WizardEvent[] UserEvents =
    [
        WizardEvent.Cancel, WizardEvent.Retry, WizardEvent.Continue,
        WizardEvent.FallbackConsented, WizardEvent.Drain, WizardEvent.Resume,
        WizardEvent.Disconnect
    ];

    public static IEnumerable<object[]> Scenarios =>
        Enum.GetValues<FakeScenario>().Select(scenario => new object[] { scenario });

    [Fact]
    public void EveryCoreStateHasOneNamedPage()
    {
        var states = Enum.GetValues<WizardState>();
        Assert.Equal(states.Length, WizardPages.All.Count);
        Assert.Equal(states.Length, WizardPages.All.Values.Select(page => page.State).Distinct().Count());
        foreach (var state in states)
        {
            var page = WizardPages.For(state);
            Assert.Equal(state, page.State);
            Assert.False(string.IsNullOrWhiteSpace(page.Title));
            Assert.False(string.IsNullOrWhiteSpace(page.Body));
        }
        Assert.Null(WizardPages.Welcome.State);
        Assert.Contains("trusted code", WizardPages.Welcome.Body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void UserActionsMatchCoreTransitionTableInEveryState()
    {
        foreach (var state in Enum.GetValues<WizardState>())
        {
            var session = NewSession(state);
            var expected = WizardStateMachine.Transitions
                .Where(row => row.From == state && UserEvents.Contains(row.Event))
                .Select(row => row.Event).ToArray();
            Assert.Equal(expected, session.EnabledUserCommands);
            Assert.Equal(expected.Length, session.Actions.Count);
            Assert.All(session.Actions, action => Assert.False(string.IsNullOrWhiteSpace(action.AutomationName)));
        }
    }

    [Theory]
    [MemberData(nameof(Scenarios))]
    public async Task ScenarioReachesDocumentedStateWithFixedClock(FakeScenario scenario)
    {
        var session = NewSession(scenario: scenario);
        await session.RunScenarioToTerminalAsync();
        var expected = scenario switch
        {
            FakeScenario.PreflightBlocked => WizardState.PreflightBlocked,
            FakeScenario.SignInDenied => WizardState.SignInDenied,
            FakeScenario.SignInExpired => WizardState.SignInExpired,
            FakeScenario.SignInWrongAccount => WizardState.SignInWrongAccount,
            FakeScenario.SignInNetworkError => WizardState.SignInPolling,
            FakeScenario.ScopeNoAdmin => WizardState.ScopeNoAdmin,
            FakeScenario.ScopeNotPrivate => WizardState.ScopeNotPrivate,
            FakeScenario.LocationRedirected => WizardState.LocationRedirected,
            FakeScenario.LocationNetwork => WizardState.LocationNetwork,
            FakeScenario.LocationTooLong => WizardState.LocationTooLong,
            FakeScenario.InstallVerifyFailed => WizardState.InstallingVerifying,
            FakeScenario.RunnerDegraded => WizardState.RunnerDegraded,
            FakeScenario.DisconnectRemoteUnavailable => WizardState.DisconnectRemotePending,
            _ => WizardState.RunnerIdle
        };
        Assert.Equal(expected, session.State);
        Assert.NotEmpty(session.Journal);
        Assert.Equal(ExpectedEvents(scenario), session.Journal.Select(entry => entry.Event));
        Assert.All(session.Journal, entry => Assert.Equal(new FakeClock().UtcNow, entry.AtUtc));
        Assert.All(session.Journal, entry =>
            Assert.Contains(WizardStateMachine.Transitions, row =>
                row.From == entry.From && row.Event == entry.Event &&
                row.To == entry.To && row.Compensation == entry.Compensation));
    }

    [Theory]
    [InlineData(WizardState.PreflightRunning, CompensationAction.None)]
    [InlineData(WizardState.SignInAwaitingCode, CompensationAction.DiscardDeviceCode)]
    [InlineData(WizardState.SignInPolling, CompensationAction.DiscardDeviceCode)]
    [InlineData(WizardState.InstallingDownloading, CompensationAction.RemoveStaging)]
    [InlineData(WizardState.InstallingVerifying, CompensationAction.RemoveStaging)]
    [InlineData(WizardState.InstallingExtracting, CompensationAction.RemoveStaging)]
    [InlineData(WizardState.InstallingConfiguring, CompensationAction.RemoveStaging)]
    [InlineData(WizardState.RunnerBusy, CompensationAction.CancelJob)]
    [InlineData(WizardState.RunnerDraining, CompensationAction.CancelJob)]
    [InlineData(WizardState.DisconnectPending, CompensationAction.PreserveRemoteRemoval)]
    [InlineData(WizardState.DisconnectRemotePending, CompensationAction.PreserveRemoteRemoval)]
    public async Task CancelUsesCoreCompensationAndShowsCleanup(
        WizardState state, CompensationAction expected)
    {
        var session = NewSession(state);
        await session.ExecuteUserCommandAsync(WizardEvent.Cancel);
        Assert.Equal(expected, session.Journal[0].Compensation);
        if (expected != CompensationAction.None)
            Assert.Contains(expected.ToString(), session.StatusText);
    }

    [Fact]
    public async Task WelcomeRequiresAcknowledgementBeforeAnyCoreEvent()
    {
        var session = NewSession();
        Assert.True(session.IsWelcome);
        Assert.False(session.WelcomeContinueCommand.CanExecute(null));
        await session.ContinueFromWelcomeAsync();
        Assert.Empty(session.Journal);
        await session.ExecuteUserCommandAsync(WizardEvent.Continue);
        Assert.Empty(session.Journal);
        session.Acknowledged = true;
        Assert.True(session.WelcomeContinueCommand.CanExecute(null));
        await session.ContinueFromWelcomeAsync();
        Assert.False(session.IsWelcome);
        Assert.NotEmpty(session.Journal);
    }

    [Fact]
    public void ErrorCatalogIsCompleteAndUserFacing()
    {
        foreach (var code in new[]
        {
            "PREFLIGHT_BLOCKED", "SIGNIN_DENIED", "SIGNIN_EXPIRED", "SIGNIN_WRONG_ACCOUNT",
            "SIGNIN_NETWORK_ERROR", "SCOPE_NO_ADMIN", "SCOPE_NOT_PRIVATE",
            "LOCATION_REDIRECTED", "LOCATION_NETWORK", "LOCATION_TOO_LONG",
            "INSTALL_DOWNLOAD_FAILED", "INSTALL_VERIFY_FAILED", "INSTALL_EXTRACT_FAILED",
            "INSTALL_CONFIGURE_FAILED", "SIGNIN_POST_SIGNEDIN_MISMATCH",
            "DISCONNECT_LOCAL_FAILED", "RUNNER_DEGRADED", "DISCONNECT_REMOTE_UNAVAILABLE",
            "INVALID_TRANSITION", "PREVIEW_ERROR"
        })
        {
            var error = ErrorCatalog.For(code);
            Assert.False(string.IsNullOrWhiteSpace(error.Title));
            Assert.False(string.IsNullOrWhiteSpace(error.WhatHappened));
            Assert.False(string.IsNullOrWhiteSpace(error.NextStep));
            var userText = error.Title + error.WhatHappened + error.NextStep;
            Assert.DoesNotContain("token=", userText, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("stack trace", userText, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("github.com/", userText, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public void ArgumentsAreFailClosedToHappyPath()
    {
        Assert.Equal(FakeScenario.HappyPath, FakeScenarioParser.Parse([]).Scenario);
        foreach (var scenario in Enum.GetValues<FakeScenario>())
            Assert.Equal(scenario, FakeScenarioParser.Parse(["--scenario", scenario.ToString()]).Scenario);
        foreach (var args in new[]
        {
            new[] { "--scenario" }, new[] { "--scenario", "unknown" },
            new[] { "--scenario", "1" },
            new[] { "--scenario", "HappyPath", "extra" }, new[] { "--url", "https://example.invalid" }
        })
        {
            var result = FakeScenarioParser.Parse(args);
            Assert.Equal(FakeScenario.HappyPath, result.Scenario);
            Assert.Equal(FakeScenarioParser.IgnoredArgumentsNotice, result.Notice);
        }
    }

    [Fact]
    public void CatalogUsesOnlyFictionalIdentity()
    {
        Assert.Equal("example-user", FakeDataCatalog.Account);
        Assert.Equal("ABCD-1234", FakeDataCatalog.DeviceCode);
        Assert.All(FakeDataCatalog.Repositories, repo => Assert.StartsWith("example-owner/", repo.Name));
        Assert.Contains("example", FakeDataCatalog.PreferredPath);
        Assert.Contains("C:\\grl", FakeDataCatalog.FallbackPath);
    }

    [Fact]
    public void PresentationMetadataHasNoDesktopOrLiveDependency()
    {
        using var file = File.OpenRead(typeof(WizardSession).Assembly.Location);
        using var pe = new PEReader(file);
        var metadata = pe.GetMetadataReader();
        var references = metadata.AssemblyReferences.Select(handle =>
            metadata.GetString(metadata.GetAssemblyReference(handle).Name)).ToArray();
        Assert.DoesNotContain(references, name =>
            name is "PresentationFramework" or "PresentationCore" or "WindowsBase" or
                "System.Windows.Forms" or "Microsoft.Win32.Registry" or "System.Management");
        var forbidden = new Regex(@"^(System\.Net|System\.IO|System\.Diagnostics\.Process|Microsoft\.Win32|System\.Security\.Principal|System\.Runtime\.InteropServices|System\.Windows\.(?!Input\.ICommand))");
        foreach (var handle in metadata.TypeReferences)
        {
            var type = metadata.GetTypeReference(handle);
            var qualified = metadata.GetString(type.Namespace) + "." + metadata.GetString(type.Name);
            Assert.DoesNotMatch(forbidden, qualified);
        }
        foreach (var handle in metadata.MemberReferences)
        {
            var member = metadata.GetMemberReference(handle);
            if (member.Parent.Kind != HandleKind.TypeReference) continue;
            var type = metadata.GetTypeReference((TypeReferenceHandle)member.Parent);
            var qualified = metadata.GetString(type.Namespace) + "." + metadata.GetString(type.Name);
            Assert.DoesNotMatch(forbidden, qualified);
        }
    }

    [Fact]
    public void AppSourceHasNoLiveOperationCalls()
    {
        var root = RepositoryRoot();
        var files = Directory.GetFiles(Path.Combine(root, "src", "Grl.App"), "*.*", SearchOption.TopDirectoryOnly)
            .Where(path => path.EndsWith(".cs", StringComparison.OrdinalIgnoreCase) ||
                           path.EndsWith(".xaml", StringComparison.OrdinalIgnoreCase));
        foreach (var file in files)
        {
            var source = File.ReadAllText(file);
            foreach (var forbidden in new[]
            {
                @"\bSystem\.Net\b", @"\bHttpClient\b", @"\bWebClient\b", @"\bSocket\b",
                @"\bProcess\b", @"\bProcessStartInfo\b", @"\bRegistry\b",
                @"\bDllImport\b", @"\bLibraryImport\b", @"\bFile\s*\.",
                @"\bDirectory\s*\.", @"\bFileStream\b", @"\bStreamWriter\b",
                @"\bGetFolderPath\b", @"\bSpecialFolder\b", @"\bClipboard\b",
                @"\bNavigateUri\b", @"\bRequestNavigate\b", @"\bOpenFileDialog\b",
                @"\bSaveFileDialog\b", @"\bOpenFolderDialog\b", @"\bShellExecute\b",
                @"\brunas\b", @"\bWindowsIdentity\b"
            })
                Assert.DoesNotMatch(new Regex(forbidden), source);
        }
    }

    [Fact]
    public void ManifestIsAsInvokerAndPerMonitorV2()
    {
        var doc = XDocument.Load(Path.Combine(RepositoryRoot(), "src", "Grl.App", "app.manifest"));
        var level = doc.Descendants().Single(node => node.Name.LocalName == "requestedExecutionLevel");
        Assert.Equal("asInvoker", (string?)level.Attribute("level"));
        Assert.Equal("false", (string?)level.Attribute("uiAccess"));
        Assert.Equal("PerMonitorV2", doc.Descendants().Single(node =>
            node.Name.LocalName == "dpiAwareness").Value);
    }

    [Fact]
    public void SignInOnlyShowsSimulatedCode()
    {
        var session = NewSession(WizardState.SignInPolling);
        Assert.DoesNotContain(session.Actions, action =>
            action.Label.Contains("browser", StringComparison.OrdinalIgnoreCase) ||
            action.Label.Contains("clipboard", StringComparison.OrdinalIgnoreCase));
        Assert.Contains("simulated device code", WizardPages.For(WizardState.SignInPolling).Body,
            StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData(WizardState.SignInSignedIn, "SIGNIN_POST_SIGNEDIN_MISMATCH")]
    [InlineData(WizardState.InstallingDownloading, "INSTALL_DOWNLOAD_FAILED")]
    [InlineData(WizardState.InstallingVerifying, "INSTALL_VERIFY_FAILED")]
    [InlineData(WizardState.InstallingExtracting, "INSTALL_EXTRACT_FAILED")]
    [InlineData(WizardState.InstallingConfiguring, "INSTALL_CONFIGURE_FAILED")]
    [InlineData(WizardState.DisconnectDone, "DISCONNECT_LOCAL_FAILED")]
    public void CoreGapFailureStaysOnCurrentStateAndKeepsTransitionCommands(
        WizardState state, string code)
    {
        var session = NewSession(state);
        var before = session.EnabledUserCommands.ToArray();
        session.ShowPreviewFailure(code);
        Assert.Equal(state, session.State);
        Assert.Equal(code, session.ActiveError?.Code);
        Assert.Equal(before, session.EnabledUserCommands);
        Assert.Empty(session.Journal);
    }

    [Fact]
    public void BlockedPreflightHasReadableFakeReasons()
    {
        Assert.Contains("Simulated blockers", NewSession(WizardState.PreflightBlocked).PreflightReasonText);
    }

    private static WizardEvent[] ExpectedEvents(FakeScenario scenario)
    {
        var prefix = new[] { WizardEvent.Passed, WizardEvent.Continue, WizardEvent.CodeIssued };
        var signedIn = prefix.Concat([WizardEvent.SignedIn, WizardEvent.Continue]).ToArray();
        var scope = signedIn.Concat([WizardEvent.Selected, WizardEvent.Continue]).ToArray();
        var location = scope.Concat([WizardEvent.Continue]).ToArray();
        var installed = location.Concat([
            WizardEvent.Downloaded, WizardEvent.Verified, WizardEvent.Extracted,
            WizardEvent.Configured]).ToArray();
        return scenario switch
        {
            FakeScenario.PreflightBlocked => [WizardEvent.Blocked],
            FakeScenario.SignInDenied => prefix.Concat([WizardEvent.Denied]).ToArray(),
            FakeScenario.SignInExpired => prefix.Concat([WizardEvent.Expired]).ToArray(),
            FakeScenario.SignInWrongAccount => prefix.Concat([WizardEvent.WrongAccount]).ToArray(),
            FakeScenario.SignInNetworkError => prefix,
            FakeScenario.ScopeNoAdmin => signedIn.Concat([WizardEvent.NoAdmin]).ToArray(),
            FakeScenario.ScopeNotPrivate => signedIn.Concat([WizardEvent.NotPrivate]).ToArray(),
            FakeScenario.LocationRedirected => scope.Concat([WizardEvent.Redirected]).ToArray(),
            FakeScenario.LocationNetwork => scope.Concat([WizardEvent.Network]).ToArray(),
            FakeScenario.LocationTooLong => scope.Concat([WizardEvent.TooLong]).ToArray(),
            FakeScenario.InstallVerifyFailed => location.Concat([WizardEvent.Downloaded]).ToArray(),
            FakeScenario.RunnerDegraded => installed.Concat([WizardEvent.Degraded]).ToArray(),
            FakeScenario.DisconnectRemoteUnavailable => installed.Concat([
                WizardEvent.Online, WizardEvent.Disconnect, WizardEvent.RemoteUnavailable]).ToArray(),
            _ => installed.Concat([WizardEvent.Online]).ToArray()
        };
    }

    private static WizardSession NewSession(WizardState? state = null,
        FakeScenario scenario = FakeScenario.HappyPath) =>
        new(new ScenarioSelection(scenario, string.Empty), new FakeClock(),
            new PreviewDelay(TimeSpan.Zero), WizardAdapters.CreateFake(), state);

    private static string RepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "global.json")))
            directory = directory.Parent;
        return directory?.FullName ?? throw new InvalidOperationException("Repository root unavailable.");
    }
}
