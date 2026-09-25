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

public static class LiveWizardPages
{
    public static WizardPageViewModel Welcome { get; } = new(null, "LIVE portable runner setup",
        "Trusted jobs run with your current Windows account permissions. Confirm workplace authorization and the exact private repository before continuing.");

    public static WizardPageViewModel For(WizardState state) => state switch
    {
        WizardState.PreflightRunning => new(state, "LIVE preflight", "Checking unelevated portable mode and the selected local root."),
        WizardState.PreflightBlocked => new(state, "LIVE setup blocked", "Resolve the local safety check before retrying."),
        WizardState.SignInAwaitingCode or WizardState.SignInPolling => new(state, "GitHub device sign-in",
            "Enter the displayed code at the GitHub verification URI. Confirm the signed-in account before continuing."),
        WizardState.SignInSignedIn => new(state, "GitHub account confirmed", "Continue only if this is the intended account."),
        WizardState.SignInWrongAccount => new(state, "Wrong account", "The signed-in account does not match the target owner."),
        WizardState.ScopeSelecting => new(state, "Checking exact repository", "Verifying private visibility and administrator permission."),
        WizardState.ScopeSelected => new(state, "Exact repository selected", "Confirm the displayed private target before installation."),
        WizardState.ScopeNoAdmin or WizardState.ScopeNotPrivate => new(state, "Repository blocked", "A private repository with administrator permission is required."),
        WizardState.LocationLocal => new(state, "Local runner folder", "Review the owned local folder before downloading the pinned runner."),
        WizardState.InstallingDownloading or WizardState.InstallingVerifying or WizardState.InstallingExtracting =>
            new(state, "Installing reviewed runner", "Downloading, hash checking, and extracting the reviewed official package."),
        WizardState.InstallingConfiguring => new(state, "Registering portable runner", "Checking name collision and starting the reviewed runner CLI."),
        WizardState.RunnerOffline => new(state, "Checking runner status", "Waiting for the exact runner to appear online."),
        WizardState.RunnerIdle => new(state, "Runner online", "The portable runner is online and idle."),
        WizardState.RunnerBusy => new(state, "Runner busy", "Safe Pause is unavailable. Explicit Stop Now can cancel this or a newly assigned job."),
        WizardState.RunnerDraining => new(state, "Runner state pending", "Safe automatic Drain is unavailable; the process was not stopped."),
        WizardState.RunnerPaused => new(state, "Runner stopped explicitly", "The owned process was stopped with Stop Now. Remote registration may remain."),
        WizardState.RunnerDegraded => new(state, "Runner degraded", "A process or registration may remain. Use exact-identity recovery when available."),
        WizardState.DisconnectPending or WizardState.DisconnectRemotePending => new(state, "Unregister pending",
            "Remote removal is pending or unverified. A process from an earlier app session may still be running."),
        WizardState.DisconnectDone => new(state, "Runner unregistered", "The exact runner is absent from the target repository."),
        _ => new(state, state.ToString(), "Review the current live state before continuing.")
    };
}
