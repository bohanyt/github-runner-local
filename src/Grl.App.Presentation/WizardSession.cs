using System.ComponentModel;
using System.Windows.Input;
using Grl.Core;

namespace Grl.App.Presentation;

public sealed record UiAction(
    string Label, string AutomationName, ICommand Command,
    bool IsPrimary = false, bool IsCancel = false);

public sealed class AsyncUiCommand(Func<Task> execute, Func<bool> canExecute, Action onError) : ICommand
{
    public event EventHandler? CanExecuteChanged;

    public bool CanExecute(object? parameter) => canExecute();

    public async void Execute(object? parameter)
    {
        if (!CanExecute(parameter)) return;
        try { await execute(); }
        catch { onError(); }
    }

    public void Refresh() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
}

public sealed class WizardSession : INotifyPropertyChanged
{
    private static readonly WizardEvent[] UserEvents =
    [
        WizardEvent.Cancel, WizardEvent.Retry, WizardEvent.Continue,
        WizardEvent.FallbackConsented, WizardEvent.Drain,
        WizardEvent.Resume, WizardEvent.Disconnect
    ];

    private static readonly WizardEvent[] RunnerSimulationEvents =
    [
        WizardEvent.Online, WizardEvent.JobStarted, WizardEvent.JobFinished,
        WizardEvent.Offline, WizardEvent.Degraded, WizardEvent.Drained
    ];

    private WizardStateMachine _machine;
    private readonly IClock _clock;
    private readonly WizardAdapters _adapters;
    private readonly IPreviewDelay _delay;
    private readonly bool _live;
    private readonly string _liveLocation;
    private readonly List<WizardJournalEntry> _journal = [];
    private readonly AsyncUiCommand _welcomeCommand;
    private readonly AsyncUiCommand _removalCommand;
    private readonly AsyncUiCommand _stopNowCommand;
    private readonly AsyncUiCommand _recoveryCommand;
    private bool _acknowledged;
    private bool _isWelcome;
    private bool _accountConfirmed;
    private bool _targetConfirmed;
    private bool _refreshing;
    private string _statusText = "Preview waiting for acknowledgement. No action has run.";
    private string _deviceCodeText = string.Empty;
    private string _signedInAccountText = string.Empty;
    private string _selectedRepositoryText = string.Empty;
    private string _deletionPlanText = string.Empty;
    private UiError? _activeError;

    public WizardSession(
        ScenarioSelection selection,
        IClock clock,
        IPreviewDelay delay,
        WizardAdapters adapters,
        WizardState? initialState = null,
        bool live = false,
        string liveLocation = "")
    {
        Scenario = selection.Scenario;
        Notice = selection.Notice;
        _clock = clock;
        _machine = new WizardStateMachine(clock, initialState ?? WizardState.PreflightRunning);
        _delay = delay;
        _adapters = adapters;
        _live = live;
        _liveLocation = liveLocation;
        if (_live) _statusText = "LIVE setup waiting for trusted-code acknowledgement.";
        _isWelcome = initialState is null;
        _welcomeCommand = new AsyncUiCommand(ContinueFromWelcomeAsync,
            () => IsWelcome && Acknowledged, ShowUnexpectedError);
        _removalCommand = new AsyncUiCommand(PreviewRemovalAsync,
            () => !_live && !IsWelcome && State is WizardState.RunnerIdle or WizardState.RunnerPaused,
            ShowUnexpectedError);
        _stopNowCommand = new AsyncUiCommand(StopNowAsync,
            () => _live && !IsWelcome && HasOwnedRunnerProcess,
            ShowUnexpectedError);
        _recoveryCommand = new AsyncUiCommand(RecoverAsync,
            () => CanRecover, ShowUnexpectedError);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public FakeScenario Scenario { get; }
    public string Notice { get; }
    public string PreviewBanner => _live
        ? "LIVE portable runner setup — actions can contact GitHub and change this computer."
        : $"Preview build — simulated data. No GitHub, runner, or system changes are made. Scenario: {Scenario}.";
    public bool IsPreview => !_live;
    public bool IsLive => _live;
    public string WelcomeContinueLabel => _live ? "_Continue LIVE setup" : "_Continue preview";
    public bool HasOwnedRunnerProcess => _live && _adapters.RunnerController.HasOwnedProcess;
    public bool HasPendingRecovery => _live && _adapters.Disconnect.HasPendingRecovery;
    public bool CanRecover => HasPendingRecovery && !HasOwnedRunnerProcess && !IsWelcome &&
        (State is WizardState.InstallingDownloading or WizardState.InstallingConfiguring or
            WizardState.RunnerDegraded or WizardState.RunnerPaused or WizardState.DisconnectRemotePending);
    public string RunnerPresenceText => !_live ? string.Empty : HasOwnedRunnerProcess
        ? "A product-owned runner process is active. Safe Pause is unavailable. Stop Now may cancel an active or newly assigned job."
        : State == WizardState.DisconnectDone
            ? "Exact remote removal was verified in this session. The local root remains for inspection."
        : HasPendingRecovery
            ? "No runner process is owned by this window. Registration or a process from an earlier app session may remain. Recovery requires a stored numeric runner ID; otherwise inspect repository Settings > Actions > Runners manually."
            : "Safe Pause is unavailable. Closing or crashing the app does not guarantee that a runner process stops.";
    public bool ShowAccountConfirmation => _live && State == WizardState.SignInSignedIn;
    public bool ShowTargetConfirmation => _live && State == WizardState.ScopeSelected;
    public bool AccountConfirmed
    {
        get => _accountConfirmed;
        set { _accountConfirmed = value; NotifyAll(); }
    }
    public bool TargetConfirmed
    {
        get => _targetConfirmed;
        set { _targetConfirmed = value; NotifyAll(); }
    }
    public bool IsWelcome => _isWelcome;
    public bool IsInWizard => !_isWelcome;
    public WizardState State => _machine.State;
    public WizardPageViewModel CurrentPage => _live
        ? (_isWelcome ? LiveWizardPages.Welcome : LiveWizardPages.For(State))
        : (_isWelcome ? WizardPages.Welcome : WizardPages.For(State));
    public IReadOnlyList<WizardJournalEntry> Journal => _journal.AsReadOnly();
    public string StatusText => _statusText;
    public string DeviceCodeText => _deviceCodeText;
    public string SignedInAccountText => _signedInAccountText;
    public string SelectedRepositoryText => _selectedRepositoryText;
    public string RepositoryListText => _live ? string.Empty : string.Join("; ", FakeDataCatalog.Repositories.Select(item =>
        $"{item.Name} ({(item.IsPrivate ? "private" : "public")}, {(item.HasAdminAccess ? "admin" : "no admin")})"));
    public string LocationText => _live ? _liveLocation : FakeDataCatalog.PreferredPath + " (fictional)";
    public string PreflightReasonText => State == WizardState.PreflightBlocked
        ? (_live ? "Portable setup requires an unelevated process and an ordinary local root on C:." :
            "Simulated blockers: fictional capability unavailable; no real machine check ran.")
        : string.Empty;
    public string DeletionPlanText => _deletionPlanText;
    public UiError? ActiveError => _activeError;
    public string ErrorTitle => _activeError?.Title ?? string.Empty;
    public string ErrorWhatHappened => _activeError?.WhatHappened ?? string.Empty;
    public string ErrorNextStep => _activeError?.NextStep ?? string.Empty;
    public ICommand WelcomeContinueCommand => _welcomeCommand;
    public ICommand PreviewRemovalCommand => _removalCommand;
    public ICommand StopNowCommand => _stopNowCommand;
    public ICommand RecoveryCommand => _recoveryCommand;

    public bool Acknowledged
    {
        get => _acknowledged;
        set
        {
            if (_acknowledged == value) return;
            _acknowledged = value;
            Changed(nameof(Acknowledged));
            _welcomeCommand.Refresh();
        }
    }

    public IReadOnlyList<WizardEvent> EnabledUserCommands => _isWelcome ? [] :
        WizardStateMachine.Transitions
            .Where(row => row.From == State && UserEvents.Contains(row.Event) &&
                (!_live || row.Event != WizardEvent.Continue ||
                    (State != WizardState.SignInSignedIn || _accountConfirmed) &&
                    (State != WizardState.ScopeSelected || _targetConfirmed)) &&
                (!_live || !(row.Event == WizardEvent.Cancel && State is
                    WizardState.InstallingDownloading or WizardState.InstallingVerifying or
                    WizardState.InstallingExtracting or WizardState.InstallingConfiguring or
                    WizardState.RunnerBusy or WizardState.RunnerDraining or WizardState.DisconnectPending)) &&
                (!_live || row.Event != WizardEvent.Drain) &&
                (!_live || row.Event != WizardEvent.Disconnect || !HasOwnedRunnerProcess))
            .Select(row => row.Event)
            .ToArray();

    public IReadOnlyList<WizardEvent> EnabledSimulationEvents => _isWelcome || _live ? [] :
        WizardStateMachine.Transitions
            .Where(row => row.From == State && RunnerSimulationEvents.Contains(row.Event))
            .Select(row => row.Event)
            .ToArray();

    public IReadOnlyList<UiAction> Actions => BuildActions();
    public IReadOnlyList<UiAction> SimulationActions => _live ? [] : BuildSimulationActions();
    public IReadOnlyList<UiAction> FailureSimulationActions => _live ? [] : BuildFailureSimulationActions();

    public void ShowPreviewFailure(string code)
    {
        if (_live) { SetError("INVALID_TRANSITION"); return; }
        var expected = code switch
        {
            "SIGNIN_POST_SIGNEDIN_MISMATCH" => WizardState.SignInSignedIn,
            "INSTALL_DOWNLOAD_FAILED" => WizardState.InstallingDownloading,
            "INSTALL_VERIFY_FAILED" => WizardState.InstallingVerifying,
            "INSTALL_EXTRACT_FAILED" => WizardState.InstallingExtracting,
            "INSTALL_CONFIGURE_FAILED" => WizardState.InstallingConfiguring,
            "DISCONNECT_LOCAL_FAILED" => WizardState.DisconnectDone,
            _ => (WizardState?)null
        };
        if (expected != State || _isWelcome)
        {
            SetError("INVALID_TRANSITION");
            return;
        }
        SetError(code);
    }

    public async Task ContinueFromWelcomeAsync()
    {
        if (!_isWelcome || !_acknowledged) return;
        _isWelcome = false;
        _activeError = null;
        _statusText = _live ? "LIVE setup started." : "Preview started. All data and actions are simulated.";
        NotifyAll();
        await AdvanceAutomaticAsync();
    }

    public async Task ExecuteUserCommandAsync(WizardEvent action)
    {
        if (_isWelcome || !EnabledUserCommands.Contains(action))
        {
            SetError("INVALID_TRANSITION");
            return;
        }
        if (_live && action == WizardEvent.Resume)
        {
            try { await _adapters.RunnerController.ResumeAsync(); }
            catch (AdapterOperationException error) { SetError(error.ErrorCode); return; }
            catch { SetError("RUNNER_DEGRADED"); return; }
        }
        if (!Apply(action)) return;
        if (action == WizardEvent.Cancel)
        {
            if (State is WizardState.SignInAwaitingCode or WizardState.SignInPolling or WizardState.SignInCancelled)
                _adapters.DeviceSignIn.CancelSignIn();
            _deviceCodeText = string.Empty;
        }
        NotifyAll();
        await AdvanceAutomaticAsync();
    }

    public async Task StopNowAsync()
    {
        if (!_live || IsWelcome || !HasOwnedRunnerProcess)
        { SetError("INVALID_TRANSITION"); return; }
        try
        {
            await _adapters.RunnerController.StopNowAsync();
            // Stop-now is a separate lifecycle operation absent from the frozen Core table.
            if (State is WizardState.RunnerIdle or WizardState.RunnerBusy or WizardState.RunnerDraining)
                _machine = new WizardStateMachine(_clock, WizardState.RunnerPaused);
            _activeError = null;
            _statusText = "Explicit Stop Now ended the product-owned process. An active or newly assigned job may have been cancelled. Registration may remain.";
            NotifyAll();
        }
        catch { SetError("RUNNER_DEGRADED"); }
    }

    public async Task RecoverAsync()
    {
        if (!CanRecover) { SetError("INVALID_TRANSITION"); return; }
        try
        {
            var result = await _adapters.Disconnect.RemoveAsync();
            _machine = new WizardStateMachine(_clock, result == WizardEvent.RemoteRemoved
                ? WizardState.DisconnectDone : WizardState.DisconnectRemotePending);
            _activeError = result == WizardEvent.RemoteRemoved ? null : LiveErrorCatalog.For("DISCONNECT_REMOTE_UNAVAILABLE");
            _statusText = result == WizardEvent.RemoteRemoved
                ? "Exact remote runner removal verified. Local root remains for inspection."
                : "Removal remains pending. No runner process was stopped or adopted.";
            NotifyAll();
        }
        catch (AdapterOperationException error)
        {
            _machine = new WizardStateMachine(_clock, WizardState.DisconnectRemotePending);
            SetError(error.ErrorCode);
        }
        catch { SetError("DISCONNECT_REMOTE_UNAVAILABLE"); }
    }

    public async Task RefreshRunnerStatusAsync()
    {
        if (!_live || _refreshing || State is not (WizardState.RunnerIdle or WizardState.RunnerBusy)) return;
        _refreshing = true;
        var expected = State;
        try
        {
            var observed = await _adapters.RunnerController.RefreshAsync(expected);
            if (State == expected)
            {
                if (observed.ErrorCode is not null) SetError(observed.ErrorCode);
                else if (observed.Event is not null) Apply(observed.Event.Value);
            }
        }
        catch { if (State == WizardState.RunnerIdle) Apply(WizardEvent.Degraded); }
        finally { _refreshing = false; }
    }

    public Task SimulateRunnerEventAsync(WizardEvent action)
    {
        if (_live || _isWelcome || !EnabledSimulationEvents.Contains(action))
        {
            SetError("INVALID_TRANSITION");
            return Task.CompletedTask;
        }
        Apply(action);
        return Task.CompletedTask;
    }

    public async Task RunScenarioToTerminalAsync()
    {
        Acknowledged = true;
        await ContinueFromWelcomeAsync();
        for (var step = 0; step < 32 && _activeError is null; step++)
        {
            if (State is WizardState.PreflightPassed or WizardState.SignInSignedIn or
                WizardState.ScopeSelected or WizardState.LocationLocal or
                WizardState.LocationConsentFallback)
            {
                await ExecuteUserCommandAsync(WizardEvent.Continue);
                continue;
            }
            if (State == WizardState.RunnerIdle && Scenario == FakeScenario.DisconnectRemoteUnavailable)
            {
                await ExecuteUserCommandAsync(WizardEvent.Disconnect);
                continue;
            }
            break;
        }
    }

    private async Task AdvanceAutomaticAsync()
    {
        for (var step = 0; step < 16; step++)
        {
            var expected = State;
            AdapterResult? result = null;
            switch (expected)
            {
                case WizardState.PreflightRunning:
                    await _delay.WaitAsync();
                    if (State != expected || _activeError is not null) return;
                    result = new(await _adapters.Preflight.CheckAsync());
                    break;
                case WizardState.SignInAwaitingCode:
                    await _delay.WaitAsync();
                    if (State != expected || _activeError is not null) return;
                    var deviceDisplay = await _adapters.DeviceSignIn.IssueCodeAsync();
                    _deviceCodeText = (_live ? "GitHub device code: " : "Simulated device code: ") +
                        deviceDisplay.UserCode + " — " + deviceDisplay.VerificationUri;
                    NotifyAll();
                    result = new(WizardEvent.CodeIssued);
                    break;
                case WizardState.SignInPolling:
                    await _delay.WaitAsync();
                    if (State != expected || _activeError is not null) return;
                    result = await _adapters.DeviceSignIn.PollAsync();
                    if (_live && result.Event is not null) _deviceCodeText = string.Empty;
                    if (_live && result.Event == WizardEvent.SignedIn && result.DisplayData is not null)
                    {
                        _accountConfirmed = false;
                        _signedInAccountText = "Signed in as " + result.DisplayData + ". Confirm this account before continuing.";
                        NotifyAll();
                    }
                    break;
                case WizardState.ScopeSelecting:
                    await _delay.WaitAsync();
                    if (State != expected || _activeError is not null) return;
                    var selection = await _adapters.ExecutionTarget.SelectAsync();
                    _selectedRepositoryText = (_live ? "Selected private target: " : "Fictional target: ") + selection.RepositoryName;
                    if (_live) _targetConfirmed = false;
                    result = new(selection.Event);
                    break;
                case WizardState.LocationLocal:
                    result = new(await _adapters.Location.EvaluateAsync());
                    break;
                case WizardState.InstallingDownloading:
                case WizardState.InstallingVerifying:
                case WizardState.InstallingExtracting:
                case WizardState.InstallingConfiguring:
                    await _delay.WaitAsync();
                    if (State != expected || _activeError is not null) return;
                    result = await _adapters.RunnerPackage.AdvanceAsync(expected);
                    break;
                case WizardState.RunnerOffline:
                    await _delay.WaitAsync();
                    if (State != expected || _activeError is not null) return;
                    result = new(await _adapters.RunnerController.StartAsync());
                    break;
                case WizardState.DisconnectPending:
                    await _delay.WaitAsync();
                    if (State != expected) return;
                    try
                    {
                        result = new(await _adapters.Disconnect.RemoveAsync());
                    }
                    catch (AdapterOperationException error)
                    {
                        result = new AdapterResult(null, error.ErrorCode);
                    }
                    break;
                default:
                    return;
            }

            if (result.ErrorCode is not null)
            {
                SetError(result.ErrorCode);
                return;
            }
            if (result.Event is null) return;
            if (!Apply(result.Event.Value)) return;
            var code = State switch
            {
                WizardState.PreflightBlocked => "PREFLIGHT_BLOCKED",
                WizardState.SignInDenied => "SIGNIN_DENIED",
                WizardState.SignInExpired => "SIGNIN_EXPIRED",
                WizardState.SignInWrongAccount => "SIGNIN_WRONG_ACCOUNT",
                WizardState.ScopeNoAdmin => "SCOPE_NO_ADMIN",
                WizardState.ScopeNotPrivate => "SCOPE_NOT_PRIVATE",
                WizardState.LocationRedirected => "LOCATION_REDIRECTED",
                WizardState.LocationNetwork => "LOCATION_NETWORK",
                WizardState.LocationTooLong => "LOCATION_TOO_LONG",
                WizardState.RunnerDegraded => "RUNNER_DEGRADED",
                WizardState.DisconnectRemotePending => "DISCONNECT_REMOTE_UNAVAILABLE",
                _ => null
            };
            if (code is not null)
            {
                SetError(code);
                return;
            }
        }
    }

    private bool Apply(WizardEvent action)
    {
        try
        {
            var entry = _machine.Apply(action);
            _journal.Add(entry);
            _activeError = null;
            _statusText = _live ? $"LIVE state: {entry.To}." :
                entry.Compensation == CompensationAction.None
                ? $"Preview state: {entry.To}. Simulated event: {action}."
                : $"Simulated cleanup: {entry.Compensation}. Preview state: {entry.To}.";
            NotifyAll();
            return true;
        }
        catch (ContractException)
        {
            SetError("INVALID_TRANSITION");
            return false;
        }
    }

    private Task PreviewRemovalAsync()
    {
        try
        {
            var plan = _adapters.Location.PreviewRemoval();
            _deletionPlanText = "Remove local files preview — deletion plan only, nothing deleted: " +
                string.Join("; ", plan.PathsInDeletionOrder);
            NotifyAll();
        }
        catch (ContractException)
        {
            SetError("PREVIEW_ERROR");
        }
        return Task.CompletedTask;
    }

    private IReadOnlyList<UiAction> BuildActions()
    {
        var enabled = EnabledUserCommands;
        var primary = enabled.FirstOrDefault(action => action != WizardEvent.Cancel);
        var actions = enabled.Select(action => new UiAction(
            LabelFor(action), CurrentPage.Title + " " + action,
            new AsyncUiCommand(() => ExecuteUserCommandAsync(action),
                () => !_isWelcome && EnabledUserCommands.Contains(action), ShowUnexpectedError),
            IsPrimary: action == primary && action != WizardEvent.Cancel,
            IsCancel: action == WizardEvent.Cancel)).ToList();
        if (CanRecover)
            actions.Add(new UiAction("_Recover / remove exact runner", "Recover exact runner registration",
                _recoveryCommand));
        return actions;
    }

    private IReadOnlyList<UiAction> BuildSimulationActions() =>
        RunnerSimulationEvents.Select(action => new UiAction(
            "Simulate " + action, "Preview simulation " + action,
            new AsyncUiCommand(() => SimulateRunnerEventAsync(action),
                () => !_isWelcome && EnabledSimulationEvents.Contains(action), ShowUnexpectedError))).ToArray();

    private IReadOnlyList<UiAction> BuildFailureSimulationActions()
    {
        string[] codes = State switch
        {
            WizardState.SignInSignedIn => ["SIGNIN_POST_SIGNEDIN_MISMATCH"],
            WizardState.InstallingDownloading => ["INSTALL_DOWNLOAD_FAILED"],
            WizardState.InstallingVerifying => ["INSTALL_VERIFY_FAILED"],
            WizardState.InstallingExtracting => ["INSTALL_EXTRACT_FAILED"],
            WizardState.InstallingConfiguring => ["INSTALL_CONFIGURE_FAILED"],
            WizardState.DisconnectDone => ["DISCONNECT_LOCAL_FAILED"],
            _ => Array.Empty<string>()
        };
        return codes.Select(code => new UiAction(
            "Simulate " + ErrorCatalog.For(code).Title,
            CurrentPage.Title + " preview failure " + code,
            new AsyncUiCommand(() =>
            {
                ShowPreviewFailure(code);
                return Task.CompletedTask;
            }, () => !_isWelcome, ShowUnexpectedError))).ToArray();
    }

    private static string LabelFor(WizardEvent action) => action switch
    {
        WizardEvent.Continue => "_Continue",
        WizardEvent.Cancel => "_Cancel",
        WizardEvent.Retry => "_Retry",
        WizardEvent.FallbackConsented => "_Use fallback",
        WizardEvent.Drain => "_Drain",
        WizardEvent.Resume => "_Resume",
        WizardEvent.Disconnect => "_Disconnect",
        _ => action.ToString()
    };

    private void SetError(string code)
    {
        _activeError = _live ? LiveErrorCatalog.For(code) : ErrorCatalog.For(code);
        _statusText = (_live ? "LIVE alert: " : "Preview alert: ") + _activeError.Title + ".";
        NotifyAll();
    }

    private void ShowUnexpectedError() => SetError("PREVIEW_ERROR");

    private void NotifyAll()
    {
        foreach (var name in new[]
        {
            nameof(IsWelcome), nameof(IsInWizard), nameof(State), nameof(CurrentPage),
            nameof(IsLive), nameof(IsPreview), nameof(HasOwnedRunnerProcess), nameof(WelcomeContinueLabel),
            nameof(HasPendingRecovery), nameof(CanRecover), nameof(RunnerPresenceText),
            nameof(ShowAccountConfirmation), nameof(ShowTargetConfirmation),
            nameof(AccountConfirmed), nameof(TargetConfirmed),
            nameof(Journal), nameof(StatusText), nameof(DeviceCodeText), nameof(SignedInAccountText), nameof(PreflightReasonText),
            nameof(SelectedRepositoryText), nameof(DeletionPlanText),
            nameof(ActiveError), nameof(ErrorTitle), nameof(ErrorWhatHappened),
            nameof(ErrorNextStep), nameof(EnabledUserCommands),
            nameof(EnabledSimulationEvents), nameof(Actions), nameof(SimulationActions),
            nameof(FailureSimulationActions)
        }) Changed(name);
        _welcomeCommand.Refresh();
        _removalCommand.Refresh();
        _stopNowCommand.Refresh();
        _recoveryCommand.Refresh();
    }

    private void Changed(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
