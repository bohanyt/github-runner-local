namespace Grl.App.Presentation;

public enum FakeScenario
{
    HappyPath,
    PreflightBlocked,
    SignInDenied,
    SignInExpired,
    SignInWrongAccount,
    SignInNetworkError,
    ScopeNoAdmin,
    ScopeNotPrivate,
    LocationRedirected,
    LocationNetwork,
    LocationTooLong,
    InstallVerifyFailed,
    RunnerDegraded,
    DisconnectRemoteUnavailable
}

public sealed record ScenarioSelection(FakeScenario Scenario, string Notice);

public static class FakeScenarioParser
{
    public const string IgnoredArgumentsNotice =
        "Unrecognized arguments ignored. HappyPath preview is active.";

    public static ScenarioSelection Parse(string[]? args)
    {
        if (args is null || args.Length == 0)
            return new(FakeScenario.HappyPath, string.Empty);

        if (args.Length == 2 && string.Equals(args[0], "--scenario", StringComparison.Ordinal))
        {
            foreach (var scenario in Enum.GetValues<FakeScenario>())
                if (string.Equals(args[1], scenario.ToString(), StringComparison.OrdinalIgnoreCase))
                    return new(scenario, string.Empty);
        }

        return new(FakeScenario.HappyPath, IgnoredArgumentsNotice);
    }
}

public sealed record FakeRepository(string Name, bool IsPrivate, bool HasAdminAccess);

public static class FakeDataCatalog
{
    public const string Account = "example-user";
    public const string DeviceCode = "ABCD-1234";
    public const string PreferredPath = @"C:\Users\example\Documents\github-runner-local";
    public const string FallbackPath = @"C:\grl";
    public const string NetworkPath = @"\\example\share";

    public static IReadOnlyList<FakeRepository> Repositories { get; } =
    [
        new("example-owner/grl-exec-fixture", IsPrivate: true, HasAdminAccess: true),
        new("example-owner/public-demo", IsPrivate: false, HasAdminAccess: true),
        new("example-owner/no-admin-fixture", IsPrivate: true, HasAdminAccess: false)
    ];
}
