using System.Text.RegularExpressions;
using Grl.App.Presentation;
using Grl.Core;

namespace Grl.Integration;

public sealed record LiveRuntimeOptions(string ClientId, string Repository, string RunnerName, string OwnedRoot)
{
    public static LiveRuntimeOptions Parse(string[] args)
    {
        if (args.Length != 9 || args[0] != "--live" || args[1] != "--client-id" ||
            args[3] != "--repo" || args[5] != "--runner-name" || args[7] != "--root" ||
            !Regex.IsMatch(args[2], @"\A[A-Za-z0-9.]{8,128}\z") ||
            !Regex.IsMatch(args[4], @"\A[A-Za-z0-9-]{1,39}/[A-Za-z0-9_.-]{1,100}\z") ||
            args[4].Contains("..", StringComparison.Ordinal) ||
            !Regex.IsMatch(args[6], @"\A[A-Za-z0-9._-]{1,128}\z") ||
            !Path.IsPathFullyQualified(args[8]))
            throw new ArgumentException("LIVE mode requires --client-id, --repo, --runner-name and --root with safe values. No tokens are accepted.");
        return new LiveRuntimeOptions(args[2], args[4], args[6], Path.GetFullPath(args[8]));
    }
}

public sealed class LiveWizardComposition : IDisposable
{
    private readonly LiveWizardAdapters adapter;
    private LiveWizardComposition(LiveWizardAdapters adapter, WizardSession session)
    {
        this.adapter = adapter;
        Session = session;
    }

    public WizardSession Session { get; }

    public static LiveWizardComposition Create(LiveRuntimeOptions options)
    {
        var adapter = new LiveWizardAdapters(options);
        var wiring = new WizardAdapters(adapter, adapter, adapter, adapter, adapter, adapter, adapter);
        var session = new WizardSession(new ScenarioSelection(FakeScenario.HappyPath, string.Empty),
            new LiveClock(), new PreviewDelay(TimeSpan.Zero), wiring,
            live: true, liveLocation: options.OwnedRoot);
        return new LiveWizardComposition(adapter, session);
    }

    public void Dispose() => adapter.Dispose();

    private sealed class LiveClock : IClock
    {
        public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
    }
}

internal sealed class LiveWizardAdapters : IPreflightAdapter, IDeviceSignInAdapter,
    IExecutionTargetAdapter, ILocationAdapter, IRunnerPackageAdapter, IRunnerControllerAdapter,
    IDisconnectAdapter, IDisposable
{
    private readonly LiveRuntimeOptions options;
    private readonly HttpClient apiHttp = new(new HttpClientHandler { AllowAutoRedirect = false })
        { Timeout = TimeSpan.FromMinutes(2) };
    private readonly HttpClient downloadHttp = new() { Timeout = TimeSpan.FromMinutes(10) };
    private readonly GitHubDeviceFlow flow;
    private DeviceAuthorization? authorization;
    private CancellationTokenSource? signInCancellation;
    private GitHubAccessToken? access;
    private GitHubRepository? selected;
    private RunnerInstallResult? installed;
    private PortableRunnerProcess? process;
    private IRunnerCli? cli;
    private PortableRunnerLifecycle? lifecycle;
    private PortableRecoveryStore? recoveryStore;
    private PortableRecoveryEvidence? reopened;

    public LiveWizardAdapters(LiveRuntimeOptions options)
    {
        this.options = options;
        flow = new GitHubDeviceFlow(apiHttp, options.ClientId);
    }

    public Task<WizardEvent> CheckAsync() => Task.FromResult(
        PortableRunnerProcess.IsElevated() || !SafeRoot(options.OwnedRoot)
            ? WizardEvent.Blocked : WizardEvent.Passed);

    public async Task<DeviceCodeDisplay> IssueCodeAsync()
    {
        signInCancellation?.Dispose();
        signInCancellation = new CancellationTokenSource();
        authorization = await flow.RequestDeviceCodeAsync(signInCancellation.Token);
        return new DeviceCodeDisplay(authorization.UserCode, authorization.VerificationUri);
    }

    public async Task<AdapterResult> PollAsync()
    {
        if (authorization is null || signInCancellation is null)
            return new AdapterResult(null, "SIGNIN_NETWORK_ERROR");
        try
        {
            access = await flow.PollForTokenAsync(authorization, signInCancellation.Token);
            authorization = null;
            var user = await flow.GetCurrentUserAsync(access, signInCancellation.Token);
            var owner = options.Repository.Split('/')[0];
            if (!Regex.IsMatch(user.Login, @"\A[A-Za-z0-9-]{1,39}\z") ||
                !string.Equals(user.Login, owner, StringComparison.OrdinalIgnoreCase))
            {
                access.Dispose();
                access = null;
                return new AdapterResult(WizardEvent.WrongAccount);
            }
            return new AdapterResult(WizardEvent.SignedIn, DisplayData: user.Login);
        }
        catch (OperationCanceledException) when (signInCancellation.IsCancellationRequested)
        {
            access?.Dispose();
            access = null;
            return new AdapterResult(null);
        }
        catch (GitHubIntegrationException e)
        {
            access?.Dispose();
            access = null;
            return e.Failure switch
            {
                GitHubFailure.Denied => new AdapterResult(WizardEvent.Denied),
                GitHubFailure.Expired => new AdapterResult(WizardEvent.Expired),
                _ => new AdapterResult(null, "SIGNIN_NETWORK_ERROR")
            };
        }
    }

    public void CancelSignIn()
    {
        signInCancellation?.Cancel();
        authorization?.Dispose();
        authorization = null;
    }

    public async Task<TargetSelection> SelectAsync()
    {
        if (access is null) return new TargetSelection(options.Repository, WizardEvent.NoAdmin);
        if (selected is not null) return new TargetSelection(selected.FullName, WizardEvent.Selected);
        var parts = options.Repository.Split('/');
        try
        {
            var candidate = await flow.RequirePrivateAdminRepositoryAsync(parts[0], parts[1], access, CancellationToken.None);
            if (!string.Equals(candidate.FullName, options.Repository, StringComparison.OrdinalIgnoreCase))
                return new TargetSelection(options.Repository, WizardEvent.NoAdmin);
            selected = candidate;
            return new TargetSelection(candidate.FullName, WizardEvent.Selected);
        }
        catch (GitHubIntegrationException e)
        {
            return new TargetSelection(options.Repository,
                e.Failure == GitHubFailure.PublicRepository ? WizardEvent.NotPrivate : WizardEvent.NoAdmin);
        }
    }

    public Task<WizardEvent?> EvaluateAsync() => Task.FromResult<WizardEvent?>(
        SafeRoot(options.OwnedRoot) ? null : WizardEvent.Network);

    public WindowsDeletionPlan PreviewRemoval() => new(options.OwnedRoot, []);

    public async Task<AdapterResult> AdvanceAsync(WizardState state)
    {
        try
        {
            switch (state)
            {
                case WizardState.InstallingDownloading:
                    if (selected is null || access is null) return new AdapterResult(null, "INSTALL_DOWNLOAD_FAILED");
                    var finalRoot = Path.Combine(options.OwnedRoot, "runner");
                    if (ExistingFinalRoot(finalRoot))
                    {
                        recoveryStore ??= PortableRecoveryStore.Open(finalRoot, selected.FullName, options.RunnerName);
                        reopened = recoveryStore.Read();
                        return new AdapterResult(null, reopened is null ? "RECOVERY_STATE_UNAVAILABLE" :
                            reopened.State == PortableRunnerState.Removed ? "RECOVERY_COMPLETED" : "RECOVERY_REQUIRED");
                    }
                    var pin = RunnerPin.LoadReviewed(Path.Combine(AppContext.BaseDirectory, "runner-pins.json"));
                    installed = await new RunnerPackageInstaller().DownloadAndInstallAsync(
                        downloadHttp, pin, options.OwnedRoot, "runner", CancellationToken.None);
                    recoveryStore = PortableRecoveryStore.Open(installed.FinalRoot, selected.FullName, options.RunnerName);
                    recoveryStore.Write(null, PortableRunnerState.Ready);
                    return new AdapterResult(WizardEvent.Downloaded);
                case WizardState.InstallingVerifying:
                    return installed is not null && installed.Sha256 == RunnerPin.ReviewedSha256
                        ? new AdapterResult(WizardEvent.Verified) : new AdapterResult(null, "INSTALL_VERIFY_FAILED");
                case WizardState.InstallingExtracting:
                    if (installed is null) return new AdapterResult(null, "INSTALL_EXTRACT_FAILED");
                    process = new PortableRunnerProcess(installed);
                    cli = await process.VerifyCliAsync(CancellationToken.None);
                    return new AdapterResult(WizardEvent.Extracted);
                case WizardState.InstallingConfiguring:
                    if (selected is null || access is null || process is null || cli is null)
                        return new AdapterResult(null, "INSTALL_CONFIGURE_FAILED");
                    var admin = new GitHubRunnerAdministration(apiHttp, access, selected);
                    lifecycle = new PortableRunnerLifecycle(admin, cli, process, options.RunnerName, "grl-exec",
                        recovery: recoveryStore);
                    await lifecycle.RegisterAndStartAsync(20, TimeSpan.FromSeconds(3), CancellationToken.None);
                    return new AdapterResult(WizardEvent.Configured);
                default:
                    return new AdapterResult(null, "INVALID_TRANSITION");
            }
        }
        catch
        {
            return new AdapterResult(null, state switch
            {
                WizardState.InstallingDownloading => ExistingFinalRoot(Path.Combine(options.OwnedRoot, "runner"))
                    ? "RECOVERY_STATE_UNAVAILABLE" : "INSTALL_DOWNLOAD_FAILED",
                WizardState.InstallingVerifying => "INSTALL_VERIFY_FAILED",
                WizardState.InstallingExtracting => "INSTALL_EXTRACT_FAILED",
                _ => "INSTALL_CONFIGURE_FAILED"
            });
        }
    }

    public async Task<WizardEvent> StartAsync()
    {
        if (lifecycle?.State != PortableRunnerState.Online || !lifecycle.HasOwnedProcess)
            return WizardEvent.Degraded;
        try
        {
            var remote = await lifecycle.GetRemoteStatusAsync(CancellationToken.None);
            return remote?.Online == true ? WizardEvent.Online : WizardEvent.Degraded;
        }
        catch { return WizardEvent.Degraded; }
    }

    public bool HasOwnedProcess => lifecycle?.HasOwnedProcess == true;
    public bool HasPendingRecovery => recoveryStore is not null &&
        (lifecycle is { State: not PortableRunnerState.Removed } ||
            reopened is { State: not PortableRunnerState.Removed });

    public async Task DrainAsync()
    {
        if (lifecycle is null) throw new InvalidOperationException("Runner lifecycle unavailable.");
        await lifecycle.DrainAsync(120, TimeSpan.FromSeconds(5), CancellationToken.None);
    }

    public async Task ResumeAsync()
    {
        if (lifecycle is null) throw new InvalidOperationException("Runner lifecycle unavailable.");
        await lifecycle.ResumeAsync(20, TimeSpan.FromSeconds(3), CancellationToken.None);
    }

    public async Task StopNowAsync()
    {
        if (lifecycle is null) throw new InvalidOperationException("Runner lifecycle unavailable.");
        await lifecycle.StopNowAsync(CancellationToken.None);
    }

    public async Task<AdapterResult> RefreshAsync(WizardState state)
    {
        if (lifecycle is null) return new AdapterResult(null, "RUNNER_DEGRADED");
        try
        {
            var remote = await lifecycle.GetRemoteStatusAsync(CancellationToken.None);
            if (state == WizardState.RunnerIdle)
                return remote is null || !remote.Online ? new AdapterResult(WizardEvent.Degraded)
                    : remote.Busy ? new AdapterResult(WizardEvent.JobStarted) : new AdapterResult(null);
            if (state == WizardState.RunnerBusy)
                return remote is { Online: true, Busy: false } ? new AdapterResult(WizardEvent.JobFinished)
                    : remote is { Online: true, Busy: true } ? new AdapterResult(null)
                    : new AdapterResult(null, "RUNNER_DEGRADED");
            return new AdapterResult(null);
        }
        catch { return state == WizardState.RunnerIdle ? new AdapterResult(WizardEvent.Degraded)
            : new AdapterResult(null, "RUNNER_DEGRADED"); }
    }

    public async Task<WizardEvent> RemoveAsync()
    {
        if (HasOwnedProcess) return WizardEvent.RemoteUnavailable;
        if (selected is null || access is null || recoveryStore is null) return WizardEvent.RemoteUnavailable;
        try
        {
            if (lifecycle is not null)
            {
                await lifecycle.UnregisterAsync(120, TimeSpan.FromSeconds(5), CancellationToken.None);
                return lifecycle.State == PortableRunnerState.Removed
                    ? WizardEvent.RemoteRemoved : WizardEvent.RemoteUnavailable;
            }
            // Reopen is recovery-only: no process is adopted and no local CLI is executed.
            if (reopened is not { CanAttemptExactRemoteRemoval: true, RunnerId: long id })
                return WizardEvent.RemoteUnavailable;
            var admin = new GitHubRunnerAdministration(apiHttp, access, selected);
            await admin.DeleteStaleAsync(id, options.RunnerName, CancellationToken.None);
            var remaining = await admin.ListAsync(CancellationToken.None);
            if (remaining.Any(x => x.Id == id || x.Name.Equals(options.RunnerName, StringComparison.OrdinalIgnoreCase)))
            {
                recoveryStore.Write(id, PortableRunnerState.RemoteRemovalPending);
                return WizardEvent.RemoteUnavailable;
            }
            recoveryStore.Write(id, PortableRunnerState.Removed);
            reopened = reopened with { State = PortableRunnerState.Removed };
            return WizardEvent.RemoteRemoved;
        }
        catch
        {
            try
            {
                if (lifecycle is null)
                    recoveryStore.Write(reopened?.RunnerId, PortableRunnerState.RemoteRemovalPending,
                        reopened?.MayHaveUnownedProcess ?? true);
            }
            catch { /* preserve existing evidence when storage is unavailable */ }
            return WizardEvent.RemoteUnavailable;
        }
    }

    private static bool SafeRoot(string root)
    {
        try
        {
            if (!OperatingSystem.IsWindows() || !Path.IsPathFullyQualified(root) ||
                !root.StartsWith("C:\\", StringComparison.OrdinalIgnoreCase) ||
                root.Any(c => char.IsControl(c) || c is '%' or '!' or '&' or '|' or '<' or '>' or '^' or '"' or '(' or ')') ||
                !Directory.Exists(root)) return false;
            for (var cursor = root; cursor is not null; cursor = Directory.GetParent(cursor)?.FullName)
                if ((File.GetAttributes(cursor) & FileAttributes.ReparsePoint) != 0) return false;
            return true;
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException or ArgumentException)
        { return false; }
    }

    private static bool ExistingFinalRoot(string path)
    {
        try { return Path.Exists(path) || new FileInfo(path).LinkTarget is not null ||
            new DirectoryInfo(path).LinkTarget is not null; }
        catch (IOException) { return true; }
        catch (UnauthorizedAccessException) { return true; }
    }

    public void Dispose()
    {
        CancelSignIn();
        signInCancellation?.Dispose();
        access?.Dispose();
        lifecycle?.Dispose();
        recoveryStore?.Dispose();
        apiHttp.Dispose();
        downloadHttp.Dispose();
    }
}
