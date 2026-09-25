namespace Grl.App.Presentation;

public static class LiveErrorCatalog
{
    public static UiError For(string code) => code switch
    {
        "PREFLIGHT_BLOCKED" => new(code, "LIVE preflight blocked",
            "The app must be unelevated and the owned root must be an ordinary local directory on C:.",
            "Fix the root or launch unelevated, then retry."),
        "SIGNIN_DENIED" => new(code, "GitHub sign-in denied", "The device authorization was denied.",
            "Retry with a new code."),
        "SIGNIN_EXPIRED" => new(code, "GitHub code expired", "The device authorization expired.",
            "Retry with a new code."),
        "SIGNIN_WRONG_ACCOUNT" => new(code, "Wrong GitHub account",
            "The signed-in account does not match the exact target owner.", "Retry with the intended account."),
        "SIGNIN_NETWORK_ERROR" => new(code, "GitHub sign-in unavailable",
            "The sign-in request did not complete.", "Cancel and retry when GitHub is available."),
        "SCOPE_NO_ADMIN" => new(code, "Repository access blocked",
            "The exact target could not be confirmed with administrator permission.",
            "Check the App installation and repository administration permission."),
        "SCOPE_NOT_PRIVATE" => new(code, "Private repository required",
            "The exact target is public.", "Choose the approved private repository."),
        "LOCATION_NETWORK" => new(code, "Local root blocked",
            "The owned root is not an ordinary local C: directory.", "Choose an ordinary local root."),
        "INSTALL_DOWNLOAD_FAILED" => new(code, "Runner download failed",
            "The reviewed runner package was not installed.", "Inspect the safe local state before retrying."),
        "INSTALL_VERIFY_FAILED" => new(code, "Runner verification failed",
            "The reviewed package verification did not pass.", "Do not use the runner files; inspect the local state."),
        "INSTALL_EXTRACT_FAILED" => new(code, "Runner CLI verification failed",
            "The installed runner CLI did not pass the read-only contract probe.", "Inspect the installed package."),
        "INSTALL_CONFIGURE_FAILED" => new(code, "Runner setup incomplete",
            "Configuration or portable start did not complete. Registration may need explicit cleanup.",
            "Inspect exact runner identity and use the bounded unregister action."),
        "RUNNER_DEGRADED" => new(code, "Runner degraded",
            "The runner operation did not complete or its status is uncertain.", "Inspect status and retry or unregister."),
        "DISCONNECT_REMOTE_UNAVAILABLE" => new(code, "Remote removal pending",
            "Remote absence could not be verified.", "Retry explicit unregister after connectivity returns."),
        "INVALID_TRANSITION" => new(code, "Action unavailable",
            "This action is unavailable in the current live state.", "Choose an enabled action."),
        _ => new(code, "LIVE action stopped", "The operation did not complete.",
            "Inspect the safe status and retry explicitly.")
    };
}
