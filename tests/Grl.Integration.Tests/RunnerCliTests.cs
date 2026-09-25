using Grl.Integration;

namespace Grl.Integration.Tests;

public sealed class RunnerCliTests
{
    private static readonly string Help = string.Join(' ', RunnerCliContract.RequiredHelpOptions) + " --replace --disableupdate";

    [Fact]
    public void PinnedVersionAndRequiredHelpCapabilitiesAreAccepted()
    {
        var cli = RunnerCliContract.Verify("2.337.0\r\n", Help);
        Assert.Equal("2.337.0", cli.Capabilities.Version);
        Assert.All(RunnerCliContract.RequiredHelpOptions, option => Assert.Contains(option, cli.Capabilities.Options));
    }

    [Theory]
    [InlineData("2.336.0")]
    [InlineData("2.338.0")]
    [InlineData("unknown")]
    public void UnknownVersionRequiresWizardUpdate(string version)
    {
        var error = Assert.Throws<RunnerCliException>(() => RunnerCliContract.Verify(version, Help));
        Assert.Equal(RunnerCliFailure.UnsupportedVersion, error.Failure);
    }

    [Fact]
    public void MissingRequiredCapabilityRefusesCommands()
    {
        var error = Assert.Throws<RunnerCliException>(
            () => RunnerCliContract.Verify("2.337.0", Help.Replace("--work", "")));
        Assert.Equal(RunnerCliFailure.MissingCapability, error.Failure);
    }

    [Fact]
    public void CommandVectorsUseDocumentedEntrypointsWithoutReplacementOrDisabledUpdates()
    {
        IRunnerCli cli = RunnerCliContract.Verify("2.337.0", Help);
        var configure = cli.BuildConfigure(new RunnerConfiguration(
            new Uri("https://github.com/owner/execution"), "one-hour-secret", "runner-1", "grl-exec", "w"));
        var remove = cli.BuildRemove("remove-secret");
        var run = cli.BuildRun();
        Assert.Equal("config.cmd", configure.EntryPoint);
        Assert.Equal("config.cmd", remove.EntryPoint);
        Assert.Equal("run.cmd", run.EntryPoint);
        Assert.Equal("remove", remove.Arguments[0]);
        Assert.DoesNotContain("--replace", configure.Arguments);
        Assert.DoesNotContain("--disableupdate", configure.Arguments);
        Assert.DoesNotContain("one-hour-secret", configure.ToString());
        Assert.DoesNotContain("remove-secret", remove.ToString());
        Assert.DoesNotContain(".runner", configure.ToString());
        Assert.Empty(run.Arguments);
    }

    [Fact]
    public void ConfigurationDisplayRedactsRegistrationToken()
    {
        const string sentinel = "registration-token-sentinel";
        var configuration = new RunnerConfiguration(
            new Uri("https://github.com/owner/execution"), sentinel, "runner-1", "grl-exec", "w");
        var cli = RunnerCliContract.Verify("2.337.0", Help);

        Assert.DoesNotContain(sentinel, configuration.ToString());
        Assert.Contains(sentinel, cli.BuildConfigure(configuration).Arguments);
    }

    [Fact]
    public void InvalidRepositoryOrWorkPathRefusesCommand()
    {
        var cli = RunnerCliContract.Verify("2.337.0", Help);
        var error = Assert.Throws<RunnerCliException>(() => cli.BuildConfigure(new RunnerConfiguration(
            new Uri("https://example.com/owner/repo"), "secret", "name", "label", "../work")));
        Assert.Equal(RunnerCliFailure.InvalidArgument, error.Failure);
    }
}
