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

public sealed record FakeAdapterResult(WizardEvent? Event, string? ErrorCode = null);

public interface IPreflightAdapter
{
    Task<WizardEvent> CheckAsync(FakeScenario scenario);
}

public interface IDeviceSignInAdapter
{
    Task<string> IssueCodeAsync();
    Task<FakeAdapterResult> PollAsync(FakeScenario scenario);
}

public interface IExecutionTargetAdapter
{
    Task<(FakeRepository Repository, WizardEvent Event)> SelectAsync(FakeScenario scenario);
}

public interface ILocationAdapter
{
    Task<WizardEvent?> EvaluateAsync(FakeScenario scenario);
    WindowsDeletionPlan PreviewRemoval();
}

public interface IRunnerPackageAdapter
{
    Task<FakeAdapterResult> AdvanceAsync(WizardState state, FakeScenario scenario);
}

public interface IRunnerControllerAdapter
{
    Task<WizardEvent> StartAsync(FakeScenario scenario);
}

public interface IDisconnectAdapter
{
    Task<WizardEvent> RemoveAsync(FakeScenario scenario);
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
    public static WizardAdapters CreateFake() => new(
        new FakePreflightAdapter(),
        new FakeDeviceSignInAdapter(),
        new FakeExecutionTargetAdapter(),
        new FakeLocationAdapter(),
        new FakeRunnerPackageAdapter(),
        new FakeRunnerControllerAdapter(),
        new FakeDisconnectAdapter());
}

public sealed class FakePreflightAdapter : IPreflightAdapter
{
    public Task<WizardEvent> CheckAsync(FakeScenario scenario)
    {
        // The settings and disk figures are invented and remain in memory.
        var settings = SettingsV1.Parse("{\"schema_version\":\"grl.settings.v1\",\"mode\":\"portable\"}");
        var disk = DiskAdmission.Evaluate(32 * DiskAdmission.BytesPerGiB, settings.DiskReserveGiB);
        return Task.FromResult(scenario == FakeScenario.PreflightBlocked || !disk.Admitted
            ? WizardEvent.Blocked : WizardEvent.Passed);
    }
}

public sealed class FakeDeviceSignInAdapter : IDeviceSignInAdapter
{
    public Task<string> IssueCodeAsync() => Task.FromResult(FakeDataCatalog.DeviceCode);

    public Task<FakeAdapterResult> PollAsync(FakeScenario scenario) => Task.FromResult(scenario switch
    {
        FakeScenario.SignInDenied => new FakeAdapterResult(WizardEvent.Denied),
        FakeScenario.SignInExpired => new FakeAdapterResult(WizardEvent.Expired),
        FakeScenario.SignInWrongAccount => new FakeAdapterResult(WizardEvent.WrongAccount),
        FakeScenario.SignInNetworkError => new FakeAdapterResult(null, "SIGNIN_NETWORK_ERROR"),
        _ => new FakeAdapterResult(WizardEvent.SignedIn)
    });
}

public sealed class FakeExecutionTargetAdapter : IExecutionTargetAdapter
{
    public Task<(FakeRepository Repository, WizardEvent Event)> SelectAsync(FakeScenario scenario)
    {
        var repository = scenario switch
        {
            FakeScenario.ScopeNoAdmin => FakeDataCatalog.Repositories[2],
            FakeScenario.ScopeNotPrivate => FakeDataCatalog.Repositories[1],
            _ => FakeDataCatalog.Repositories[0]
        };
        var selection = !repository.IsPrivate ? WizardEvent.NotPrivate
            : !repository.HasAdminAccess ? WizardEvent.NoAdmin : WizardEvent.Selected;
        return Task.FromResult((repository, selection));
    }
}

public sealed class FakeLocationAdapter : ILocationAdapter
{
    private readonly FakeFileSystemView _view = new();
    private readonly WindowsPathPolicy _policy;

    public FakeLocationAdapter()
    {
        _view.AddAncestors(FakeDataCatalog.PreferredPath);
        _view.AddAncestors(FakeDataCatalog.FallbackPath);
        var root = FakeDataCatalog.PreferredPath;
        _view.Add(root + @"\r");
        _view.Add(root + @"\r\w");
        _view.SetChildren(root, root + @"\r");
        _view.SetChildren(root + @"\r", root + @"\r\w");
        _policy = new WindowsPathPolicy(_view);
    }

    public Task<WizardEvent?> EvaluateAsync(FakeScenario scenario)
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

public sealed class FakeRunnerPackageAdapter : IRunnerPackageAdapter
{
    public Task<FakeAdapterResult> AdvanceAsync(WizardState state, FakeScenario scenario) =>
        Task.FromResult(state switch
        {
            WizardState.InstallingDownloading => new FakeAdapterResult(WizardEvent.Downloaded),
            WizardState.InstallingVerifying when scenario == FakeScenario.InstallVerifyFailed =>
                new FakeAdapterResult(null, "INSTALL_VERIFY_FAILED"),
            WizardState.InstallingVerifying => new FakeAdapterResult(WizardEvent.Verified),
            WizardState.InstallingExtracting => new FakeAdapterResult(WizardEvent.Extracted),
            WizardState.InstallingConfiguring => new FakeAdapterResult(WizardEvent.Configured),
            _ => new FakeAdapterResult(null, "INVALID_TRANSITION")
        });
}

public sealed class FakeRunnerControllerAdapter : IRunnerControllerAdapter
{
    public Task<WizardEvent> StartAsync(FakeScenario scenario) =>
        Task.FromResult(scenario == FakeScenario.RunnerDegraded
            ? WizardEvent.Degraded : WizardEvent.Online);
}

public sealed class FakeDisconnectAdapter : IDisconnectAdapter
{
    public Task<WizardEvent> RemoveAsync(FakeScenario scenario) =>
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
