namespace Grl.Core;

public interface IClock
{
    DateTimeOffset UtcNow { get; }
}

public enum WizardState
{
    PreflightRunning, PreflightBlocked, PreflightPassed,
    SignInAwaitingCode, SignInPolling, SignInCancelled, SignInDenied,
    SignInExpired, SignInSignedIn, SignInWrongAccount,
    ScopeSelecting, ScopeNoAdmin, ScopeNotPrivate, ScopeSelected,
    LocationLocal, LocationRedirected, LocationNetwork, LocationTooLong,
    LocationConsentFallback,
    InstallingDownloading, InstallingVerifying, InstallingExtracting,
    InstallingConfiguring,
    RunnerOffline, RunnerIdle, RunnerBusy, RunnerDraining, RunnerPaused,
    RunnerDegraded,
    DisconnectPending, DisconnectRemotePending, DisconnectDone
}

public enum WizardEvent
{
    Cancel, Retry, Passed, Blocked, Continue, CodeIssued, SignedIn,
    Denied, Expired, WrongAccount, NoAdmin, NotPrivate, Selected,
    Redirected, Network, TooLong, FallbackConsented, Downloaded,
    Verified, Extracted, Configured, Online, Offline, JobStarted,
    JobFinished, Drain, Drained, Resume, Degraded, Disconnect,
    RemoteUnavailable, RemoteRemoved
}

public enum CompensationAction
{
    None, DiscardDeviceCode, RemoveStaging, CancelJob, PreserveRemoteRemoval
}

public sealed record WizardTransition(
    WizardState From, WizardEvent Event, WizardState To,
    CompensationAction Compensation = CompensationAction.None);

// Only enum values and a timestamp are journaled. No caller-supplied strings enter the journal.
public sealed record WizardJournalEntry(
    WizardState From, WizardEvent Event, WizardState To,
    CompensationAction Compensation, DateTimeOffset AtUtc);

public sealed class WizardStateMachine
{
    private static readonly WizardTransition[] TransitionTable =
    [
        new(WizardState.PreflightRunning, WizardEvent.Passed, WizardState.PreflightPassed),
        new(WizardState.PreflightRunning, WizardEvent.Blocked, WizardState.PreflightBlocked),
        new(WizardState.PreflightBlocked, WizardEvent.Retry, WizardState.PreflightRunning),
        new(WizardState.PreflightPassed, WizardEvent.Continue, WizardState.SignInAwaitingCode),
        new(WizardState.SignInAwaitingCode, WizardEvent.CodeIssued, WizardState.SignInPolling),
        new(WizardState.SignInPolling, WizardEvent.SignedIn, WizardState.SignInSignedIn),
        new(WizardState.SignInPolling, WizardEvent.Denied, WizardState.SignInDenied),
        new(WizardState.SignInPolling, WizardEvent.Expired, WizardState.SignInExpired),
        new(WizardState.SignInPolling, WizardEvent.WrongAccount, WizardState.SignInWrongAccount),
        new(WizardState.SignInCancelled, WizardEvent.Retry, WizardState.SignInAwaitingCode),
        new(WizardState.SignInDenied, WizardEvent.Retry, WizardState.SignInAwaitingCode),
        new(WizardState.SignInExpired, WizardEvent.Retry, WizardState.SignInAwaitingCode),
        new(WizardState.SignInWrongAccount, WizardEvent.Retry, WizardState.SignInAwaitingCode),
        new(WizardState.SignInSignedIn, WizardEvent.Continue, WizardState.ScopeSelecting),
        new(WizardState.ScopeSelecting, WizardEvent.NoAdmin, WizardState.ScopeNoAdmin),
        new(WizardState.ScopeSelecting, WizardEvent.NotPrivate, WizardState.ScopeNotPrivate),
        new(WizardState.ScopeSelecting, WizardEvent.Selected, WizardState.ScopeSelected),
        new(WizardState.ScopeNoAdmin, WizardEvent.Retry, WizardState.ScopeSelecting),
        new(WizardState.ScopeNotPrivate, WizardEvent.Retry, WizardState.ScopeSelecting),
        new(WizardState.ScopeSelected, WizardEvent.Continue, WizardState.LocationLocal),
        new(WizardState.LocationLocal, WizardEvent.Redirected, WizardState.LocationRedirected),
        new(WizardState.LocationLocal, WizardEvent.Network, WizardState.LocationNetwork),
        new(WizardState.LocationLocal, WizardEvent.TooLong, WizardState.LocationTooLong),
        new(WizardState.LocationRedirected, WizardEvent.FallbackConsented, WizardState.LocationConsentFallback),
        new(WizardState.LocationNetwork, WizardEvent.FallbackConsented, WizardState.LocationConsentFallback),
        new(WizardState.LocationTooLong, WizardEvent.FallbackConsented, WizardState.LocationConsentFallback),
        new(WizardState.LocationLocal, WizardEvent.Continue, WizardState.InstallingDownloading),
        new(WizardState.LocationConsentFallback, WizardEvent.Continue, WizardState.InstallingDownloading),
        new(WizardState.InstallingDownloading, WizardEvent.Downloaded, WizardState.InstallingVerifying),
        new(WizardState.InstallingVerifying, WizardEvent.Verified, WizardState.InstallingExtracting),
        new(WizardState.InstallingExtracting, WizardEvent.Extracted, WizardState.InstallingConfiguring),
        new(WizardState.InstallingConfiguring, WizardEvent.Configured, WizardState.RunnerOffline),
        new(WizardState.RunnerOffline, WizardEvent.Online, WizardState.RunnerIdle),
        new(WizardState.RunnerIdle, WizardEvent.JobStarted, WizardState.RunnerBusy),
        new(WizardState.RunnerBusy, WizardEvent.JobFinished, WizardState.RunnerIdle),
        new(WizardState.RunnerIdle, WizardEvent.Drain, WizardState.RunnerDraining),
        new(WizardState.RunnerBusy, WizardEvent.Drain, WizardState.RunnerDraining),
        new(WizardState.RunnerDraining, WizardEvent.Drained, WizardState.RunnerPaused),
        new(WizardState.RunnerPaused, WizardEvent.Resume, WizardState.RunnerIdle),
        new(WizardState.RunnerIdle, WizardEvent.Offline, WizardState.RunnerOffline),
        new(WizardState.RunnerPaused, WizardEvent.Offline, WizardState.RunnerOffline),
        new(WizardState.RunnerOffline, WizardEvent.Degraded, WizardState.RunnerDegraded),
        new(WizardState.RunnerIdle, WizardEvent.Degraded, WizardState.RunnerDegraded),
        new(WizardState.RunnerDegraded, WizardEvent.Retry, WizardState.RunnerOffline),
        new(WizardState.RunnerOffline, WizardEvent.Disconnect, WizardState.DisconnectPending),
        new(WizardState.RunnerIdle, WizardEvent.Disconnect, WizardState.DisconnectPending),
        new(WizardState.RunnerPaused, WizardEvent.Disconnect, WizardState.DisconnectPending),
        new(WizardState.RunnerDegraded, WizardEvent.Disconnect, WizardState.DisconnectPending),
        new(WizardState.DisconnectPending, WizardEvent.RemoteUnavailable, WizardState.DisconnectRemotePending),
        new(WizardState.DisconnectPending, WizardEvent.RemoteRemoved, WizardState.DisconnectDone),
        new(WizardState.DisconnectRemotePending, WizardEvent.RemoteRemoved, WizardState.DisconnectDone),

        // Cancel is an explicit transition for every waiting or running state.
        new(WizardState.PreflightRunning, WizardEvent.Cancel, WizardState.PreflightBlocked),
        new(WizardState.SignInAwaitingCode, WizardEvent.Cancel, WizardState.SignInCancelled, CompensationAction.DiscardDeviceCode),
        new(WizardState.SignInPolling, WizardEvent.Cancel, WizardState.SignInCancelled, CompensationAction.DiscardDeviceCode),
        new(WizardState.InstallingDownloading, WizardEvent.Cancel, WizardState.LocationLocal, CompensationAction.RemoveStaging),
        new(WizardState.InstallingVerifying, WizardEvent.Cancel, WizardState.LocationLocal, CompensationAction.RemoveStaging),
        new(WizardState.InstallingExtracting, WizardEvent.Cancel, WizardState.LocationLocal, CompensationAction.RemoveStaging),
        new(WizardState.InstallingConfiguring, WizardEvent.Cancel, WizardState.LocationLocal, CompensationAction.RemoveStaging),
        new(WizardState.RunnerBusy, WizardEvent.Cancel, WizardState.RunnerDraining, CompensationAction.CancelJob),
        new(WizardState.RunnerDraining, WizardEvent.Cancel, WizardState.RunnerPaused, CompensationAction.CancelJob),
        new(WizardState.DisconnectPending, WizardEvent.Cancel, WizardState.DisconnectRemotePending, CompensationAction.PreserveRemoteRemoval),
        new(WizardState.DisconnectRemotePending, WizardEvent.Cancel, WizardState.DisconnectRemotePending, CompensationAction.PreserveRemoteRemoval)
    ];

    private readonly IClock _clock;
    public WizardState State { get; private set; }
    public static IReadOnlyList<WizardTransition> Transitions => TransitionTable;

    public WizardStateMachine(IClock clock, WizardState initial = WizardState.PreflightRunning)
    {
        _clock = clock ?? throw new ArgumentNullException(nameof(clock));
        State = initial;
    }

    public WizardJournalEntry Apply(WizardEvent action)
    {
        var row = TransitionTable.FirstOrDefault(row => row.From == State && row.Event == action)
            ?? throw new ContractException("INVALID_TRANSITION", $"{action} is invalid from {State}.");
        var entry = new WizardJournalEntry(row.From, action, row.To, row.Compensation, _clock.UtcNow);
        State = row.To;
        return entry;
    }
}
