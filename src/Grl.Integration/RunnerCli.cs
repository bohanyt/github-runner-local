using System.Text.RegularExpressions;

namespace Grl.Integration;

public enum RunnerCliFailure { UnsupportedVersion, MissingCapability, InvalidArgument }

public sealed class RunnerCliException(RunnerCliFailure failure, string message) : Exception(message)
{
    public RunnerCliFailure Failure { get; } = failure;
}

public sealed class RunnerCommand : IDisposable
{
    private readonly string[] arguments;
    public RunnerCommand(string entryPoint, IReadOnlyList<string> arguments)
    {
        EntryPoint = entryPoint;
        this.arguments = arguments.ToArray();
        Arguments = Array.AsReadOnly(this.arguments);
    }
    public string EntryPoint { get; }
    public IReadOnlyList<string> Arguments { get; }
    // Argument vectors may contain a one-hour registration/removal token.
    public override string ToString() => $"{EntryPoint} [arguments redacted]";
    public void Dispose() => Array.Fill(arguments, string.Empty);
}

public sealed record RunnerCapabilities(string Version, IReadOnlySet<string> Options);

public sealed record RunnerConfiguration(
    Uri RepositoryUrl, string RegistrationToken, string Name, string Labels, string WorkDirectory,
    bool Ephemeral = false, bool NoDefaultLabels = false)
{
    public override string ToString() => "RunnerConfiguration [registration token redacted]";
}

public interface IRunnerCli
{
    RunnerCapabilities Capabilities { get; }
    RunnerCommand BuildConfigure(RunnerConfiguration configuration);
    RunnerCommand BuildRemove(string removalToken);
    RunnerCommand BuildRun();
}

public sealed class RunnerCliContract : IRunnerCli
{
    private static readonly string[] RequiredOptions =
    [
        "--unattended", "--url", "--token", "--name", "--labels", "--work",
        "--ephemeral", "--no-default-labels", "--replace", "--runasservice",
        "--windowslogonaccount", "--windowslogonpassword"
    ];

    public RunnerCapabilities Capabilities { get; }
    private RunnerCliContract(RunnerCapabilities capabilities) => Capabilities = capabilities;

    public static RunnerCliContract Verify(string listenerVersionOutput, string configHelpOutput)
    {
        var version = listenerVersionOutput.Trim().TrimStart('v');
        if (version != RunnerPin.ReviewedVersion)
            throw new RunnerCliException(RunnerCliFailure.UnsupportedVersion, "Runner version is unsupported; update the wizard.");
        var options = Regex.Matches(configHelpOutput, @"(?<![\w-])--[a-z][a-z-]*(?![\w-])", RegexOptions.IgnoreCase)
            .Select(x => x.Value.ToLowerInvariant())
            .ToHashSet(StringComparer.Ordinal);
        if (RequiredOptions.Any(option => !options.Contains(option)))
            throw new RunnerCliException(RunnerCliFailure.MissingCapability, "The runner CLI lacks a required option; update the wizard.");
        return new RunnerCliContract(new RunnerCapabilities(version, options));
    }

    public static IReadOnlyList<string> RequiredHelpOptions => RequiredOptions;

    public RunnerCommand BuildConfigure(RunnerConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        if (configuration.RepositoryUrl.Scheme != Uri.UriSchemeHttps ||
            configuration.RepositoryUrl.Host != "github.com" ||
            configuration.RepositoryUrl.Query.Length != 0 ||
            configuration.RepositoryUrl.Fragment.Length != 0 ||
            configuration.RepositoryUrl.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries).Length != 2 ||
            !Simple(configuration.Name) || !Simple(configuration.WorkDirectory) ||
            string.IsNullOrWhiteSpace(configuration.RegistrationToken) ||
            !configuration.Labels.Split(',').All(Simple))
            throw new RunnerCliException(RunnerCliFailure.InvalidArgument, "Runner configuration arguments are invalid.");
        var argv = new List<string>
        {
            "--unattended", "--url", configuration.RepositoryUrl.ToString().TrimEnd('/'),
            "--token", configuration.RegistrationToken,
            "--name", configuration.Name, "--labels", configuration.Labels,
            "--work", configuration.WorkDirectory
        };
        if (configuration.Ephemeral) argv.Add("--ephemeral");
        if (configuration.NoDefaultLabels) argv.Add("--no-default-labels");
        return new RunnerCommand("config.cmd", argv.AsReadOnly());
    }

    public RunnerCommand BuildRemove(string removalToken)
    {
        if (string.IsNullOrWhiteSpace(removalToken))
            throw new RunnerCliException(RunnerCliFailure.InvalidArgument, "A removal token is required.");
        return new RunnerCommand("config.cmd", ["remove", "--token", removalToken]);
    }

    public RunnerCommand BuildRun() => new("run.cmd", Array.Empty<string>());

    private static bool Simple(string value) =>
        !string.IsNullOrWhiteSpace(value) && value.Length <= 128 &&
        value.All(c => char.IsAsciiLetterOrDigit(c) || c is '-' or '_' or '.');
}
