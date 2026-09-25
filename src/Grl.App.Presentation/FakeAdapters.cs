using Grl.Core;

namespace Grl.App.Presentation;

public interface IPreviewDelay
{
    Task WaitAsync(CancellationToken cancellationToken = default);
}

public sealed class PreviewDelay(TimeSpan duration) : IPreviewDelay
{
    public Task WaitAsync(CancellationToken cancellationToken = default) =>
        duration == TimeSpan.Zero ? Task.CompletedTask : Task.Delay(duration, cancellationToken);
}

public sealed class FakeClock : IClock
{
    public DateTimeOffset UtcNow { get; } =
        new(2026, 9, 24, 0, 0, 0, TimeSpan.Zero);
}

public sealed record AdapterResult(WizardEvent? Event, string? ErrorCode = null, string? DisplayData = null);
public sealed record DeviceCodeDisplay(string UserCode, Uri VerificationUri);
public sealed record TargetSelection(string RepositoryName, WizardEvent Event);

public interface IPreflightAdapter
{
    Task<WizardEvent> CheckAsync();
}

public interface IDeviceSignInAdapter
{
    Task<DeviceCodeDisplay> IssueCodeAsync();
    Task<AdapterResult> PollAsync();
    void CancelSignIn();
}

public interface IExecutionTargetAdapter
{
    Task<TargetSelection> SelectAsync();
}

public interface ILocationAdapter
{
    Task<WizardEvent?> EvaluateAsync();
    WindowsDeletionPlan PreviewRemoval();
}

public interface IRunnerPackageAdapter
{
    Task<AdapterResult> AdvanceAsync(WizardState state);
}

public interface IRunnerControllerAdapter
{
    bool HasOwnedProcess { get; }
    Task<WizardEvent> StartAsync();
    Task DrainAsync();
    Task ResumeAsync();
    Task StopNowAsync();
    Task<AdapterResult> RefreshAsync(WizardState state);
}

public interface IDisconnectAdapter
{
    bool HasPendingRecovery { get; }
    Task<WizardEvent> RemoveAsync();
}

public sealed record WizardAdapters(
    IPreflightAdapter Preflight,
    IDeviceSignInAdapter DeviceSignIn,
    IExecutionTargetAdapter ExecutionTarget,
    ILocationAdapter Location,
    IRunnerPackageAdapter RunnerPackage,
    IRunnerControllerAdapter RunnerController,
    IDisconnectAdapter Disconnect)
{
    public static WizardAdapters CreateFake(FakeScenario scenario = FakeScenario.HappyPath) => new(
        new FakePreflightAdapter(scenario),
        new FakeDeviceSignInAdapter(scenario),
        new FakeExecutionTargetAdapter(scenario),
        new FakeLocationAdapter(scenario),
        new FakeRunnerPackageAdapter(scenario),
        new FakeRunnerControllerAdapter(scenario),
        new FakeDisconnectAdapter(scenario));
}

public sealed class FakePreflightAdapter(FakeScenario scenario) : IPreflightAdapter
{
    public Task<WizardEvent> CheckAsync()
    {
        // The settings and disk figures are invented and remain in memory.
        var settings = SettingsV1.Parse("{\"schema_version\":\"grl.settings.v1\",\"mode\":\"portable\"}");
        var disk = DiskAdmission.Evaluate(32 * DiskAdmission.BytesPerGiB, settings.DiskReserveGiB);
        return Task.FromResult(scenario == FakeScenario.PreflightBlocked || !disk.Admitted
            ? WizardEvent.Blocked : WizardEvent.Passed);
    }
}

public sealed class FakeDeviceSignInAdapter(FakeScenario scenario) : IDeviceSignInAdapter
{
    public Task<DeviceCodeDisplay> IssueCodeAsync() => Task.FromResult(
        new DeviceCodeDisplay(FakeDataCatalog.DeviceCode, new Uri("https://github.com/login/device")));

    public Task<AdapterResult> PollAsync() => Task.FromResult(scenario switch
    {
        FakeScenario.SignInDenied => new AdapterResult(WizardEvent.Denied),
        FakeScenario.SignInExpired => new AdapterResult(WizardEvent.Expired),
        FakeScenario.SignInWrongAccount => new AdapterResult(WizardEvent.WrongAccount),
        FakeScenario.SignInNetworkError => new AdapterResult(null, "SIGNIN_NETWORK_ERROR"),
        _ => new AdapterResult(WizardEvent.SignedIn)
    });
    public void CancelSignIn() { }
}

public sealed class FakeExecutionTargetAdapter(FakeScenario scenario) : IExecutionTargetAdapter
{
    public Task<TargetSelection> SelectAsync()
    {
        var repository = scenario switch
        {
            FakeScenario.ScopeNoAdmin => FakeDataCatalog.Repositories[2],
            FakeScenario.ScopeNotPrivate => FakeDataCatalog.Repositories[1],
            _ => FakeDataCatalog.Repositories[0]
        };
        var selection = !repository.IsPrivate ? WizardEvent.NotPrivate
            : !repository.HasAdminAccess ? WizardEvent.NoAdmin : WizardEvent.Selected;
        return Task.FromResult(new TargetSelection(repository.Name, selection));
    }
}

public sealed class FakeLocationAdapter : ILocationAdapter
{
    private readonly FakeScenario scenario;
    private readonly FakeFileSystemView _view = new();
    private readonly WindowsPathPolicy _policy;

    public FakeLocationAdapter(FakeScenario scenario = FakeScenario.HappyPath)
    {
        this.scenario = scenario;
        _view.AddAncestors(FakeDataCatalog.PreferredPath);
        _view.AddAncestors(FakeDataCatalog.FallbackPath);
        var root = FakeDataCatalog.PreferredPath;
        _view.Add(root + @"\r");
        _view.Add(root + @"\r\w");
        _view.SetChildren(root, root + @"\r");
        _view.SetChildren(root + @"\r", root + @"\r\w");
        _policy = new WindowsPathPolicy(_view);
    }

    public Task<WizardEvent?> EvaluateAsync()
    {
        var result = scenario switch
        {
            FakeScenario.LocationRedirected => Redirected(),
            FakeScenario.LocationNetwork => _policy.ClassifyLocation(FakeDataCatalog.NetworkPath),
            FakeScenario.LocationTooLong =>
                _policy.ClassifyLocation(FakeDataCatalog.PreferredPath, new string('x', 260)),
            _ => _policy.ClassifyLocation(FakeDataCatalog.PreferredPath)
        };
        WizardEvent? action = result switch
        {
            WindowsLocationKind.Redirected or WindowsLocationKind.Synced => WizardEvent.Redirected,
            WindowsLocationKind.Network => WizardEvent.Network,
            WindowsLocationKind.TooLong => WizardEvent.TooLong,
            _ => null
        };
        return Task.FromResult(action);
    }

    public WindowsDeletionPlan PreviewRemoval() =>
        _policy.PlanDeletion(FakeDataCatalog.PreferredPath, FakeDataCatalog.PreferredPath);

    private WindowsLocationKind Redirected()
    {
        _view.Add(@"C:\Users\example\Documents", redirected: true);
        return _policy.ClassifyLocation(FakeDataCatalog.PreferredPath);
    }
}

public sealed class FakeRunnerPackageAdapter(FakeScenario scenario) : IRunnerPackageAdapter
{
    public Task<AdapterResult> AdvanceAsync(WizardState state) =>
        Task.FromResult(state switch
        {
            WizardState.InstallingDownloading => new AdapterResult(WizardEvent.Downloaded),
            WizardState.InstallingVerifying when scenario == FakeScenario.InstallVerifyFailed =>
                new AdapterResult(null, "INSTALL_VERIFY_FAILED"),
            WizardState.InstallingVerifying => new AdapterResult(WizardEvent.Verified),
            WizardState.InstallingExtracting => new AdapterResult(WizardEvent.Extracted),
            WizardState.InstallingConfiguring => new AdapterResult(WizardEvent.Configured),
            _ => new AdapterResult(null, "INVALID_TRANSITION")
        });
}

public sealed class FakeRunnerControllerAdapter(FakeScenario scenario) : IRunnerControllerAdapter
{
    public bool HasOwnedProcess => false;
    public Task<WizardEvent> StartAsync() =>
        Task.FromResult(scenario == FakeScenario.RunnerDegraded
            ? WizardEvent.Degraded : WizardEvent.Online);
    public Task DrainAsync() => Task.CompletedTask;
    public Task ResumeAsync() => Task.CompletedTask;
    public Task StopNowAsync() => Task.CompletedTask;
    public Task<AdapterResult> RefreshAsync(WizardState state) => Task.FromResult(new AdapterResult(null));
}

public sealed class FakeDisconnectAdapter(FakeScenario scenario) : IDisconnectAdapter
{
    public bool HasPendingRecovery => false;
    public Task<WizardEvent> RemoveAsync() =>
        Task.FromResult(scenario == FakeScenario.DisconnectRemoteUnavailable
            ? WizardEvent.RemoteUnavailable : WizardEvent.RemoteRemoved);
}

internal sealed class FakeFileSystemView : IFileSystemView
{
    private readonly Dictionary<string, WindowsEntryMetadata> _metadata =
        new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, IReadOnlyList<string>> _children =
        new(StringComparer.OrdinalIgnoreCase);

    public void Add(string path, bool redirected = false)
    {
        var canonical = WindowsPathPolicy.NormalizeDriveAbsolute(path);
        _metadata[canonical] = new(true, true, IsRedirected: redirected);
    }

    public void AddAncestors(string path)
    {
        var canonical = WindowsPathPolicy.NormalizeDriveAbsolute(path);
        Add(canonical[..3]);
        var current = canonical[..3];
        foreach (var segment in canonical[3..].Split('\\'))
        {
            current = current.TrimEnd('\\') + "\\" + segment;
            Add(current);
        }
    }

    public void SetChildren(string parent, params string[] children) =>
        _children[WindowsPathPolicy.NormalizeDriveAbsolute(parent)] = children;

    public WindowsEntryMetadata? GetMetadata(string canonicalPath) =>
        _metadata.GetValueOrDefault(canonicalPath);

    public IReadOnlyList<string> GetChildren(string canonicalDirectory) =>
        _children.GetValueOrDefault(canonicalDirectory) ?? [];
}
