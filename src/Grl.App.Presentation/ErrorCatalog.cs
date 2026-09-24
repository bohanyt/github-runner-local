namespace Grl.App.Presentation;

public sealed record UiError(string Code, string Title, string WhatHappened, string NextStep);

public static class ErrorCatalog
{
    public static IReadOnlyDictionary<string, UiError> All { get; } =
        new Dictionary<string, UiError>(StringComparer.Ordinal)
        {
            ["PREFLIGHT_BLOCKED"] = new("PREFLIGHT_BLOCKED", "Preflight blocked",
                "A simulated capability check did not pass.", "Review the fictional reason list, then choose Retry."),
            ["SIGNIN_DENIED"] = new("SIGNIN_DENIED", "Sign-in denied",
                "The fake device response was denied.", "Choose Retry to issue another simulated code."),
            ["SIGNIN_EXPIRED"] = new("SIGNIN_EXPIRED", "Sign-in expired",
                "The fake device code expired.", "Choose Retry to issue another simulated code."),
            ["SIGNIN_WRONG_ACCOUNT"] = new("SIGNIN_WRONG_ACCOUNT", "Wrong account",
                "The fictional account did not match the expected account.", "Choose Retry and confirm the simulated account."),
            ["SIGNIN_NETWORK_ERROR"] = new("SIGNIN_NETWORK_ERROR", "Simulated network error",
                "The fake sign-in adapter reported a network failure; no request was sent.", "Choose Cancel to discard the simulated code."),
            ["SCOPE_NO_ADMIN"] = new("SCOPE_NO_ADMIN", "Admin access missing",
                "The fictional target does not grant admin access.", "Choose Retry to use another fake target."),
            ["SCOPE_NOT_PRIVATE"] = new("SCOPE_NOT_PRIVATE", "Private target required",
                "The fictional target is public.", "Choose Retry to use a private fake target."),
            ["LOCATION_REDIRECTED"] = new("LOCATION_REDIRECTED", "Redirected location",
                "Fake metadata marks the proposed Documents folder as redirected or synced.", "Consent to the fictional local fallback."),
            ["LOCATION_NETWORK"] = new("LOCATION_NETWORK", "Network location",
                "The fictional location is a network path.", "Consent to the fictional local fallback."),
            ["LOCATION_TOO_LONG"] = new("LOCATION_TOO_LONG", "Path budget exceeded",
                "The simulated worst-case path exceeds the legacy budget.", "Consent to the shorter fictional fallback."),
            ["INSTALL_VERIFY_FAILED"] = new("INSTALL_VERIFY_FAILED", "Simulated verification failed",
                "The fake package verifier reported a failure; no package exists.", "Choose Cancel for simulated staging cleanup."),
            ["INSTALL_DOWNLOAD_FAILED"] = new("INSTALL_DOWNLOAD_FAILED", "Simulated download failed",
                "The fake package download could not finish; no request was sent.", "Choose Cancel for simulated staging cleanup."),
            ["INSTALL_EXTRACT_FAILED"] = new("INSTALL_EXTRACT_FAILED", "Simulated extraction failed",
                "The fake archive extraction could not finish; no file exists.", "Choose Cancel for simulated staging cleanup."),
            ["INSTALL_CONFIGURE_FAILED"] = new("INSTALL_CONFIGURE_FAILED", "Simulated configuration failed",
                "The fake runner setup could not finish; no runner exists.", "Choose Cancel for simulated staging cleanup."),
            ["SIGNIN_POST_SIGNEDIN_MISMATCH"] = new("SIGNIN_POST_SIGNEDIN_MISMATCH", "Simulated account changed",
                "The fictional account did not match after sign-in.", "Review the fake account before continuing."),
            ["DISCONNECT_LOCAL_FAILED"] = new("DISCONNECT_LOCAL_FAILED", "Simulated local removal failed",
                "The fake local removal could not finish; no files were changed.", "The preview remains on its current state."),
            ["RUNNER_DEGRADED"] = new("RUNNER_DEGRADED", "Runner degraded",
                "The fictional runner reports a degraded state.", "Choose Retry or Disconnect."),
            ["DISCONNECT_REMOTE_UNAVAILABLE"] = new("DISCONNECT_REMOTE_UNAVAILABLE", "Remote removal pending",
                "The fake remote target is unavailable.", "Keep the pending-removal state and retry in a later checkpoint."),
            ["INVALID_TRANSITION"] = new("INVALID_TRANSITION", "Action unavailable",
                "This action is not valid from the current preview state.", "Choose one of the currently enabled actions."),
            ["PREVIEW_ERROR"] = new("PREVIEW_ERROR", "Preview action stopped",
                "A simulated action could not finish.", "Return to an enabled action or restart the preview.")
        };

    public static UiError For(string code) => All[code];
}
