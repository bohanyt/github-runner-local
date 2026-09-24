using Grl.Core;

namespace Grl.App.Presentation;

public sealed record WizardPageViewModel(WizardState? State, string Title, string Body);

public static class WizardPages
{
    public static WizardPageViewModel Welcome { get; } = new(null,
        "Welcome — trusted-code preview",
        "Use only trusted code. In portable mode, future jobs would run with your Windows account permissions. " +
        "Workplace authorization is your responsibility. This preview uses simulated data and makes no real changes. " +
        "Acknowledge this disclosure to continue.");

    public static IReadOnlyDictionary<WizardState, WizardPageViewModel> All { get; } =
        Enum.GetValues<WizardState>().ToDictionary(state => state, Create);

    public static WizardPageViewModel For(WizardState state) => All[state];

    private static WizardPageViewModel Create(WizardState state)
    {
        var (title, body) = state switch
        {
            WizardState.PreflightRunning => ("Preflight running", "Simulated checks inspect only fictional capability data."),
            WizardState.PreflightBlocked => ("Preflight blocked", "The simulated checks found a blocker. Review the reason and retry."),
            WizardState.PreflightPassed => ("Preflight passed", "The simulated capability checks passed. Continue to simulated sign-in."),
            WizardState.SignInAwaitingCode => ("Sign-in awaiting code", "A simulated device code will be shown. No browser opens."),
            WizardState.SignInPolling => ("Sign-in polling", "The simulated device code is awaiting a fake response."),
            WizardState.SignInCancelled => ("Sign-in cancelled", "The simulated code was discarded. You can retry."),
            WizardState.SignInDenied => ("Sign-in denied", "The fake sign-in was denied. You can retry."),
            WizardState.SignInExpired => ("Sign-in expired", "The fake code expired. You can retry."),
            WizardState.SignInSignedIn => ("Signed in — simulated", "The fake account is example-user. No credential exists."),
            WizardState.SignInWrongAccount => ("Wrong account", "The simulated account was not the expected account. You can retry."),
            WizardState.ScopeSelecting => ("Choose execution target", "Review the fictional repository list. Nothing is contacted."),
            WizardState.ScopeNoAdmin => ("Target needs admin access", "The fictional selection lacks admin access. Choose again."),
            WizardState.ScopeNotPrivate => ("Target must be private", "The fictional selection is public. Choose again."),
            WizardState.ScopeSelected => ("Execution target selected", "A fictional private repository with admin access is selected."),
            WizardState.LocationLocal => ("Local location", "The proposed path is fictional and is checked using fake metadata."),
            WizardState.LocationRedirected => ("Redirected location", "A simulated OneDrive-style redirect needs explicit fallback consent."),
            WizardState.LocationNetwork => ("Network location", "A simulated network path is not accepted. Consent to the fallback."),
            WizardState.LocationTooLong => ("Path too long", "The simulated path budget is too long. Consent to the fallback."),
            WizardState.LocationConsentFallback => ("Fallback consented", "The fictional C:\\grl fallback is selected for this preview."),
            WizardState.InstallingDownloading => ("Installing — download", "Simulated progress only. No package is downloaded."),
            WizardState.InstallingVerifying => ("Installing — verify", "Simulated progress only. No file is verified."),
            WizardState.InstallingExtracting => ("Installing — extract", "Simulated progress only. No archive is extracted."),
            WizardState.InstallingConfiguring => ("Installing — configure", "Simulated progress only. No runner is configured."),
            WizardState.RunnerOffline => ("Runner offline — simulated", "A fictional runner is offline. No runner process exists."),
            WizardState.RunnerIdle => ("Runner idle — simulated", "A fictional runner is idle. Preview controls can simulate events."),
            WizardState.RunnerBusy => ("Runner busy — simulated", "A fictional job is running. No code is executing."),
            WizardState.RunnerDraining => ("Runner draining — simulated", "The fictional runner is finishing a job."),
            WizardState.RunnerPaused => ("Runner paused — simulated", "The fictional runner is paused."),
            WizardState.RunnerDegraded => ("Runner degraded — simulated", "A fictional runner status needs attention."),
            WizardState.DisconnectPending => ("Disconnect pending", "A fictional remote removal is pending."),
            WizardState.DisconnectRemotePending => ("Remote removal pending", "The simulated remote service is unavailable. Removal remains pending."),
            WizardState.DisconnectDone => ("Disconnect complete", "The fictional runner was removed from the fake target."),
            _ => throw new ArgumentOutOfRangeException(nameof(state))
        };
        return new(state, title, body);
    }
}
