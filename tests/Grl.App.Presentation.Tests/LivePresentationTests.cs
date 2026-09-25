using Grl.App.Presentation;
using Grl.Core;

namespace Grl.App.Presentation.Tests;

public sealed class LivePresentationTests
{
    [Fact]
    public async Task LiveSeamAdvancesThroughConfirmationInstallationOnlineAndDisconnect()
    {
        var adapter = new SafeLiveFake();
        var session = NewLive(adapter);
        Assert.True(session.IsLive);
        Assert.Contains("LIVE", session.PreviewBanner);
        Assert.Empty(session.SimulationActions);
        Assert.Empty(session.FailureSimulationActions);
        session.Acknowledged = true;
        await session.ContinueFromWelcomeAsync();
        Assert.Equal(WizardState.PreflightPassed, session.State);
        await session.ExecuteUserCommandAsync(WizardEvent.Continue);
        Assert.Equal(WizardState.SignInSignedIn, session.State);
        Assert.Empty(session.DeviceCodeText);
        Assert.Contains("owner", session.SignedInAccountText);
        Assert.DoesNotContain(WizardEvent.Continue, session.EnabledUserCommands);
        session.AccountConfirmed = true;
        await session.ExecuteUserCommandAsync(WizardEvent.Continue);
        Assert.Equal(WizardState.ScopeSelected, session.State);
        Assert.Equal("Selected private target: owner/exec", session.SelectedRepositoryText);
        Assert.DoesNotContain(WizardEvent.Continue, session.EnabledUserCommands);
        session.TargetConfirmed = true;
        await session.ExecuteUserCommandAsync(WizardEvent.Continue);
        await session.ExecuteUserCommandAsync(WizardEvent.Continue);
        Assert.Equal(WizardState.RunnerIdle, session.State);
        Assert.Equal([WizardState.InstallingDownloading, WizardState.InstallingVerifying,
            WizardState.InstallingExtracting, WizardState.InstallingConfiguring], adapter.InstallStates);
        Assert.Equal(1, adapter.StartCount);
        await session.ExecuteUserCommandAsync(WizardEvent.Disconnect);
        Assert.Equal(WizardState.DisconnectDone, session.State);
        Assert.Equal(1, adapter.RemoveCount);
        Assert.DoesNotContain("SECRET", session.StatusText + string.Join(' ', session.Journal));
    }

    [Theory]
    [InlineData(WizardEvent.WrongAccount, WizardState.SignInWrongAccount)]
    [InlineData(WizardEvent.Denied, WizardState.SignInDenied)]
    public async Task LiveWrongAccountAndDeniedStayBlocked(WizardEvent result, WizardState expected)
    {
        var adapter = new SafeLiveFake { LoginEvent = result };
        var session = NewLive(adapter);
        session.Acknowledged = true;
        await session.ContinueFromWelcomeAsync();
        await session.ExecuteUserCommandAsync(WizardEvent.Continue);
        Assert.Equal(expected, session.State);
        Assert.Equal(0, adapter.SelectCount);
    }

    [Theory]
    [InlineData(WizardEvent.NotPrivate, WizardState.ScopeNotPrivate)]
    [InlineData(WizardEvent.NoAdmin, WizardState.ScopeNoAdmin)]
    public async Task LivePublicAndNoAdminTargetsStayBlocked(WizardEvent result, WizardState expected)
    {
        var adapter = new SafeLiveFake { TargetEvent = result };
        var session = NewLive(adapter);
        session.Acknowledged = true;
        await session.ContinueFromWelcomeAsync();
        await session.ExecuteUserCommandAsync(WizardEvent.Continue);
        session.AccountConfirmed = true;
        await session.ExecuteUserCommandAsync(WizardEvent.Continue);
        Assert.Equal(expected, session.State);
        Assert.Empty(adapter.InstallStates);
    }

    [Fact]
    public async Task LivePauseIsUnavailableAndStopNowIsExplicit()
    {
        var adapter = new SafeLiveFake { Owned = true };
        var session = NewLive(adapter, WizardState.RunnerIdle);
        Assert.DoesNotContain(WizardEvent.Drain, session.EnabledUserCommands);
        Assert.DoesNotContain(session.Actions, x => x.Label.Contains("Drain", StringComparison.OrdinalIgnoreCase));
        await session.ExecuteUserCommandAsync(WizardEvent.Drain);
        Assert.Equal(WizardState.RunnerIdle, session.State);
        Assert.Equal(0, adapter.DrainCount);
        Assert.Contains("may cancel", session.RunnerPresenceText);
        await session.StopNowAsync();
        Assert.Equal(WizardState.RunnerPaused, session.State);
        Assert.Equal(1, adapter.StopCount);
        Assert.Contains("may have been cancelled", session.StatusText);
        Assert.Empty(session.SimulationActions);
    }

    [Theory]
    [InlineData(WizardState.InstallingConfiguring, "INSTALL_CONFIGURE_FAILED")]
    [InlineData(WizardState.InstallingDownloading, "RECOVERY_REQUIRED")]
    public async Task FailedConfigureAndReopenKeepExactRecoveryVisible(WizardState failureAt, string code)
    {
        var adapter = new SafeLiveFake { PendingRecovery = true, InstallErrorState = failureAt, InstallErrorCode = code,
            RemoveEvent = WizardEvent.RemoteUnavailable };
        var session = NewLive(adapter);
        session.Acknowledged = true;
        await session.ContinueFromWelcomeAsync();
        await session.ExecuteUserCommandAsync(WizardEvent.Continue);
        session.AccountConfirmed = true;
        await session.ExecuteUserCommandAsync(WizardEvent.Continue);
        session.TargetConfirmed = true;
        await session.ExecuteUserCommandAsync(WizardEvent.Continue);
        await session.ExecuteUserCommandAsync(WizardEvent.Continue);
        Assert.Equal(failureAt, session.State);
        Assert.Equal(code, session.ActiveError?.Code);
        Assert.True(session.CanRecover);
        Assert.Contains(session.Actions, x => x.AutomationName == "Recover exact runner registration");
        await session.RecoverAsync();
        Assert.Equal(WizardState.DisconnectRemotePending, session.State);
        Assert.Equal(1, adapter.RemoveCount);
        Assert.Contains("pending", session.StatusText);
        Assert.DoesNotContain("SECRET", session.StatusText + string.Join(' ', session.Journal));
    }

    [Fact]
    public async Task ActiveProcessAfterConfigureFailureRequiresSeparateStopNow()
    {
        var adapter = new SafeLiveFake { PendingRecovery = true, Owned = true,
            InstallErrorState = WizardState.InstallingConfiguring, InstallErrorCode = "INSTALL_CONFIGURE_FAILED" };
        var session = NewLive(adapter);
        session.Acknowledged = true;
        await session.ContinueFromWelcomeAsync();
        await session.ExecuteUserCommandAsync(WizardEvent.Continue);
        session.AccountConfirmed = true;
        await session.ExecuteUserCommandAsync(WizardEvent.Continue);
        session.TargetConfirmed = true;
        await session.ExecuteUserCommandAsync(WizardEvent.Continue);
        await session.ExecuteUserCommandAsync(WizardEvent.Continue);
        Assert.False(session.CanRecover);
        Assert.True(session.StopNowCommand.CanExecute(null));
        await session.StopNowAsync();
        Assert.Equal(1, adapter.StopCount);
        Assert.True(session.CanRecover);
        Assert.Equal(0, adapter.RemoveCount);
    }

    [Fact]
    public async Task ResumeVersionDriftShowsSpecificFailClosedErrorWithoutStateAdvance()
    {
        var adapter = new SafeLiveFake { ResumeErrorCode = "RUNNER_VERSION_UNSUPPORTED" };
        var session = NewLive(adapter, WizardState.RunnerPaused);

        await session.ExecuteUserCommandAsync(WizardEvent.Resume);

        Assert.Equal(WizardState.RunnerPaused, session.State);
        Assert.Equal("RUNNER_VERSION_UNSUPPORTED", session.ActiveError?.Code);
        Assert.True(session.ErrorWhatHappened.Contains("self-updated", StringComparison.OrdinalIgnoreCase));
        Assert.Equal(1, adapter.ResumeCount);
    }

    [Fact]
    public async Task EvidenceBlockedRecoveryDoesNotSuggestBlindRetry()
    {
        var adapter = new SafeLiveFake
        {
            PendingRecovery = true,
            RemoveErrorCode = "DISCONNECT_IDENTITY_BLOCKED"
        };
        var session = NewLive(adapter, WizardState.RunnerDegraded);

        await session.RecoverAsync();

        Assert.Equal(WizardState.DisconnectRemotePending, session.State);
        Assert.Equal("DISCONNECT_IDENTITY_BLOCKED", session.ActiveError?.Code);
        Assert.True(session.ErrorNextStep.Contains("Do not keep retrying", StringComparison.OrdinalIgnoreCase));
        Assert.True(session.ErrorNextStep.Contains("Settings > Actions > Runners", StringComparison.Ordinal));
        Assert.Equal(1, adapter.RemoveCount);
    }

    [Fact]
    public async Task LiveStatusRefreshShowsBusyAndIdleWithoutSimulationControls()
    {
        var adapter = new SafeLiveFake { RefreshEvent = WizardEvent.JobStarted };
        var session = NewLive(adapter, WizardState.RunnerIdle);
        await session.RefreshRunnerStatusAsync();
        Assert.Equal(WizardState.RunnerBusy, session.State);
        Assert.DoesNotContain(WizardEvent.Cancel, session.EnabledUserCommands);
        adapter.RefreshEvent = WizardEvent.JobFinished;
        await session.RefreshRunnerStatusAsync();
        Assert.Equal(WizardState.RunnerIdle, session.State);
    }

    [Fact]
    public async Task LiveDeviceCodeIsVisibleOnlyWhileAuthorizationIsPending()
    {
        var gate = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var adapter = new SafeLiveFake { PollGate = gate };
        var session = NewLive(adapter);
        session.Acknowledged = true;
        await session.ContinueFromWelcomeAsync();
        var signIn = session.ExecuteUserCommandAsync(WizardEvent.Continue);
        await adapter.PollEntered.Task.WaitAsync(TimeSpan.FromSeconds(2));
        Assert.Contains("ABCD", session.DeviceCodeText);
        Assert.Contains("https://github.com/login/device", session.DeviceCodeText);
        Assert.DoesNotContain("ABCD", session.StatusText + string.Join(' ', session.Journal));
        gate.SetResult(true);
        await signIn;
        Assert.Empty(session.DeviceCodeText);
    }

    private static WizardSession NewLive(SafeLiveFake adapter, WizardState? initial = null) =>
        new(new ScenarioSelection(FakeScenario.HappyPath, string.Empty), new FakeClock(),
            new PreviewDelay(TimeSpan.Zero),
            new WizardAdapters(adapter, adapter, adapter, adapter, adapter, adapter, adapter),
            initial, live: true, liveLocation: @"C:\owned");

    private sealed class SafeLiveFake : IPreflightAdapter, IDeviceSignInAdapter, IExecutionTargetAdapter,
        ILocationAdapter, IRunnerPackageAdapter, IRunnerControllerAdapter, IDisconnectAdapter
    {
        public WizardEvent LoginEvent { get; set; } = WizardEvent.SignedIn;
        public WizardEvent TargetEvent { get; set; } = WizardEvent.Selected;
        public WizardEvent? RefreshEvent { get; set; }
        public TaskCompletionSource<bool>? PollGate { get; set; }
        public TaskCompletionSource<bool> PollEntered { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public List<WizardState> InstallStates { get; } = [];
        public int SelectCount { get; private set; }
        public int StartCount { get; private set; }
        public int RemoveCount { get; private set; }
        public int DrainCount { get; private set; }
        public int ResumeCount { get; private set; }
        public int StopCount { get; private set; }
        public bool Owned { get; set; }
        public bool PendingRecovery { get; set; }
        public bool HasOwnedProcess => Owned;
        public bool HasPendingRecovery => PendingRecovery;
        public WizardState? InstallErrorState { get; set; }
        public string InstallErrorCode { get; set; } = "INSTALL_CONFIGURE_FAILED";
        public string? ResumeErrorCode { get; set; }
        public string? RemoveErrorCode { get; set; }
        public WizardEvent RemoveEvent { get; set; } = WizardEvent.RemoteRemoved;
        public Task<WizardEvent> CheckAsync() => Task.FromResult(WizardEvent.Passed);
        public Task<DeviceCodeDisplay> IssueCodeAsync() => Task.FromResult(
            new DeviceCodeDisplay("ABCD", new Uri("https://github.com/login/device")));
        public async Task<AdapterResult> PollAsync()
        {
            PollEntered.TrySetResult(true);
            if (PollGate is not null) await PollGate.Task;
            return new AdapterResult(LoginEvent, DisplayData: LoginEvent == WizardEvent.SignedIn ? "owner" : null);
        }
        public void CancelSignIn() { }
        public Task<TargetSelection> SelectAsync()
        {
            SelectCount++;
            return Task.FromResult(new TargetSelection("owner/exec", TargetEvent));
        }
        public Task<WizardEvent?> EvaluateAsync() => Task.FromResult<WizardEvent?>(null);
        public WindowsDeletionPlan PreviewRemoval() => new(@"C:\owned", []);
        public Task<AdapterResult> AdvanceAsync(WizardState state)
        {
            InstallStates.Add(state);
            if (state == InstallErrorState) return Task.FromResult(new AdapterResult(null, InstallErrorCode));
            return Task.FromResult(new AdapterResult(state switch
            {
                WizardState.InstallingDownloading => WizardEvent.Downloaded,
                WizardState.InstallingVerifying => WizardEvent.Verified,
                WizardState.InstallingExtracting => WizardEvent.Extracted,
                WizardState.InstallingConfiguring => WizardEvent.Configured,
                _ => null
            }));
        }
        public Task<WizardEvent> StartAsync() { StartCount++; return Task.FromResult(WizardEvent.Online); }
        public Task DrainAsync() { DrainCount++; return Task.CompletedTask; }
        public Task ResumeAsync()
        {
            ResumeCount++;
            if (ResumeErrorCode is not null) throw new AdapterOperationException(ResumeErrorCode);
            return Task.CompletedTask;
        }
        public Task StopNowAsync() { StopCount++; Owned = false; return Task.CompletedTask; }
        public Task<AdapterResult> RefreshAsync(WizardState state) => Task.FromResult(new AdapterResult(RefreshEvent));
        public Task<WizardEvent> RemoveAsync()
        {
            RemoveCount++;
            if (RemoveErrorCode is not null) throw new AdapterOperationException(RemoveErrorCode);
            return Task.FromResult(RemoveEvent);
        }
    }
}
