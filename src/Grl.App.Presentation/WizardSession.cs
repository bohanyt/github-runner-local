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

    private readonly WizardStateMachine _machine;
    private readonly WizardAdapters _adapters;
    private readonly IPreviewDelay _delay;
    private readonly List<WizardJournalEntry> _journal = [];
    private readonly AsyncUiCommand _welcomeCommand;
    private readonly AsyncUiCommand _removalCommand;
    private bool _acknowledged;
    private bool _isWelcome;
    private string _statusText = "Preview waiting for acknowledgement. No action has run.";
    private string _deviceCodeText = string.Empty;
    private string _selectedRepositoryText = string.Empty;
    private string _deletionPlanText = string.Empty;
    private UiError? _activeError;

    public WizardSession(
        ScenarioSelection selection,
        IClock clock,
        IPreviewDelay delay,
        WizardAdapters adapters,
        WizardState? initialState = null)
    {
        Scenario = selection.Scenario;
        Notice = selection.Notice;
        _machine = new WizardStateMachine(clock, initialState ?? WizardState.PreflightRunning);
        _delay = delay;
        _adapters = adapters;
        _isWelcome = initialState is null;
        _welcomeCommand = new AsyncUiCommand(ContinueFromWelcomeAsync,
            () => IsWelcome && Acknowledged, ShowUnexpectedError);
        _removalCommand = new AsyncUiCommand(PreviewRemovalAsync,
            () => !IsWelcome && State is WizardState.RunnerIdle or WizardState.RunnerPaused,
            ShowUnexpectedError);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public FakeScenario Scenario { get; }
    public string Notice { get; }
    public string PreviewBanner =>
        $"Preview build — simulated data. No GitHub, runner, or system changes are made. Scenario: {Scenario}.";
    public bool IsWelcome => _isWelcome;
    public bool IsInWizard => !_isWelcome;
    public WizardState State => _machine.State;
    public WizardPageViewModel CurrentPage => _isWelcome ? WizardPages.Welcome : WizardPages.For(State);
    public IReadOnlyList<WizardJournalEntry> Journal => _journal.AsReadOnly();
    public string StatusText => _statusText;
    public string DeviceCodeText => _deviceCodeText;
    public string SelectedRepositoryText => _selectedRepositoryText;
    public string RepositoryListText => string.Join("; ", FakeDataCatalog.Repositories.Select(item =>
        $"{item.Name} ({(item.IsPrivate ? "private" : "public")}, {(item.HasAdminAccess ? "admin" : "no admin")})"));
    public string LocationText => FakeDataCatalog.PreferredPath + " (fictional)";
    public string PreflightReasonText => State == WizardState.PreflightBlocked
        ? "Simulated blockers: fictional capability unavailable; no real machine check ran."
        : string.Empty;
    public string DeletionPlanText => _deletionPlanText;
    public UiError? ActiveError => _activeError;
    public string ErrorTitle => _activeError?.Title ?? string.Empty;
    public string ErrorWhatHappened => _activeError?.WhatHappened ?? string.Empty;
    public string ErrorNextStep => _activeError?.NextStep ?? string.Empty;
    public ICommand WelcomeContinueCommand => _welcomeCommand;
    public ICommand PreviewRemovalCommand => _removalCommand;

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
            .Where(row => row.From == State && UserEvents.Contains(row.Event))
            .Select(row => row.Event)
            .ToArray();

    public IReadOnlyList<WizardEvent> EnabledSimulationEvents => _isWelcome ? [] :
        WizardStateMachine.Transitions
            .Where(row => row.From == State && RunnerSimulationEvents.Contains(row.Event))
            .Select(row => row.Event)
            .ToArray();

    public IReadOnlyList<UiAction> Actions => BuildActions();
    public IReadOnlyList<UiAction> SimulationActions => BuildSimulationActions();
    public IReadOnlyList<UiAction> FailureSimulationActions => BuildFailureSimulationActions();

    public void ShowPreviewFailure(string code)
    {
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
        _statusText = "Preview started. All data and actions are simulated.";
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
        if (!Apply(action)) return;
        if (action == WizardEvent.Cancel)
            _deviceCodeText = string.Empty;
        NotifyAll();
        await AdvanceAutomaticAsync();
    }

    public Task SimulateRunnerEventAsync(WizardEvent action)
    {
        if (_isWelcome || !EnabledSimulationEvents.Contains(action))
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
            FakeAdapterResult? result = null;
            switch (expected)
            {
                case WizardState.PreflightRunning:
                    await _delay.WaitAsync();
                    if (State != expected || _activeError is not null) return;
                    result = new(await _adapters.Preflight.CheckAsync(Scenario));
                    break;
                case WizardState.SignInAwaitingCode:
                    await _delay.WaitAsync();
                    if (State != expected || _activeError is not null) return;
                    _deviceCodeText = "Simulated device code: " +
                        await _adapters.DeviceSignIn.IssueCodeAsync();
                    result = new(WizardEvent.CodeIssued);
                    break;
                case WizardState.SignInPolling:
                    await _delay.WaitAsync();
                    if (State != expected || _activeError is not null) return;
                    result = await _adapters.DeviceSignIn.PollAsync(Scenario);
                    break;
                case WizardState.ScopeSelecting:
                    await _delay.WaitAsync();
                    if (State != expected || _activeError is not null) return;
                    var selection = await _adapters.ExecutionTarget.SelectAsync(Scenario);
                    _selectedRepositoryText = "Fictional target: " + selection.Repository.Name;
                    result = new(selection.Event);
                    break;
                case WizardState.LocationLocal:
                    result = new(await _adapters.Location.EvaluateAsync(Scenario));
                    break;
                case WizardState.InstallingDownloading:
                case WizardState.InstallingVerifying:
                case WizardState.InstallingExtracting:
                case WizardState.InstallingConfiguring:
                    await _delay.WaitAsync();
                    if (State != expected || _activeError is not null) return;
                    result = await _adapters.RunnerPackage.AdvanceAsync(expected, Scenario);
                    break;
                case WizardState.RunnerOffline:
                    await _delay.WaitAsync();
                    if (State != expected || _activeError is not null) return;
                    result = new(await _adapters.RunnerController.StartAsync(Scenario));
                    break;
                case WizardState.DisconnectPending:
                    await _delay.WaitAsync();
                    if (State != expected) return;
                    result = new(await _adapters.Disconnect.RemoveAsync(Scenario));
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
            _statusText = entry.Compensation == CompensationAction.None
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
        return enabled.Select(action => new UiAction(
            LabelFor(action), CurrentPage.Title + " " + action,
            new AsyncUiCommand(() => ExecuteUserCommandAsync(action),
                () => !_isWelcome && EnabledUserCommands.Contains(action), ShowUnexpectedError),
            IsPrimary: action == primary && action != WizardEvent.Cancel,
            IsCancel: action == WizardEvent.Cancel)).ToArray();
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
        _activeError = ErrorCatalog.For(code);
        _statusText = "Preview alert: " + _activeError.Title + ".";
        NotifyAll();
    }

    private void ShowUnexpectedError() => SetError("PREVIEW_ERROR");

    private void NotifyAll()
    {
        foreach (var name in new[]
        {
            nameof(IsWelcome), nameof(IsInWizard), nameof(State), nameof(CurrentPage),
            nameof(Journal), nameof(StatusText), nameof(DeviceCodeText), nameof(PreflightReasonText),
            nameof(SelectedRepositoryText), nameof(DeletionPlanText),
            nameof(ActiveError), nameof(ErrorTitle), nameof(ErrorWhatHappened),
            nameof(ErrorNextStep), nameof(EnabledUserCommands),
            nameof(EnabledSimulationEvents), nameof(Actions), nameof(SimulationActions),
            nameof(FailureSimulationActions)
        }) Changed(name);
        _welcomeCommand.Refresh();
        _removalCommand.Refresh();
    }

    private void Changed(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
