using System.Net;
using System.Diagnostics;
using System.Text;
using Grl.Integration;
using Grl.App.Presentation;
using Grl.Core;

namespace Grl.Integration.Tests;

public sealed class PortableLifecycleTests
{
    private static readonly GitHubRepository Repository = new(7, "owner/exec", true, true);
    private static readonly IRunnerCli Cli = RunnerCliContract.Verify("2.337.0",
        string.Join(' ', RunnerCliContract.RequiredHelpOptions));

    [Fact]
    public async Task RunnerAdminListsExactRepositoryRunnersAndUsesBearer()
    {
        var handler = new QueueHandler(Json("""{"total_count":1,"runners":[{"id":42,"name":"runner-1","status":"online","busy":false}]}"""));
        using var token = await AccessTokenAsync();
        var admin = new GitHubRunnerAdministration(new HttpClient(handler), token, Repository);
        var runners = await admin.ListAsync(default);
        Assert.Equal([new RepositoryRunner(42, "runner-1", true, false)], runners);
        Assert.Equal("https://api.github.com/repos/owner/exec/actions/runners?per_page=100&page=1", handler.Uris.Single());
        Assert.Equal("Bearer", handler.Schemes.Single());
    }

    [Fact]
    public void LiveModeRequiresExplicitSwitchAndRejectsSecretInputs()
    {
        using var root = new FakeRoot();
        string[] valid = ["--live", "--client-id", "Iv1.1234567890abcdef", "--repo", "owner/exec",
            "--runner-name", "runner-1", "--root", root.Path];
        var options = LiveRuntimeOptions.Parse(valid);
        using var composition = LiveWizardComposition.Create(options);
        Assert.True(composition.Session.IsLive);
        Assert.False(composition.Session.IsPreview);
        Assert.Throws<ArgumentException>(() => LiveRuntimeOptions.Parse(valid[1..]));
        Assert.Throws<ArgumentException>(() => LiveRuntimeOptions.Parse([.. valid, "--token", "SECRET"]));
        Assert.DoesNotContain("SECRET", composition.Session.PreviewBanner);
    }

    [Theory]
    [InlineData("registration-token")]
    [InlineData("remove-token")]
    public async Task RunnerAdminTokensAreShortLivedAndRedacted(string suffix)
    {
        var handler = new QueueHandler(Json("""{"token":"SECRET123","expires_at":"2026-09-25T05:00:00Z"}""", HttpStatusCode.Created));
        using var access = await AccessTokenAsync();
        var admin = new GitHubRunnerAdministration(new HttpClient(handler), access, Repository, new FixedTimeProvider());
        using var oneHour = suffix == "registration-token"
            ? await admin.CreateRegistrationTokenAsync(default) : await admin.CreateRemoveTokenAsync(default);
        Assert.Equal("SECRET123", oneHour.Value);
        Assert.DoesNotContain("SECRET123", oneHour.ToString());
        Assert.EndsWith("/" + suffix, handler.Uris.Single());
        oneHour.Dispose();
        Assert.Throws<ObjectDisposedException>(() => oneHour.Value);
    }

    [Fact]
    public async Task StaleDeleteRefetchesExactOfflineIdAndName()
    {
        var handler = new QueueHandler(
            Json("""{"total_count":1,"runners":[{"id":42,"name":"runner-1","status":"offline","busy":false}]}"""),
            new HttpResponseMessage(HttpStatusCode.NoContent));
        using var token = await AccessTokenAsync();
        var admin = new GitHubRunnerAdministration(new HttpClient(handler), token, Repository);
        await admin.DeleteStaleAsync(42, "runner-1", default);
        Assert.Equal(HttpMethod.Delete, handler.Methods.Last());
        Assert.EndsWith("/42", handler.Uris.Last());
    }

    [Theory]
    [InlineData("other", "offline", false)]
    [InlineData("runner-1", "online", false)]
    [InlineData("runner-1", "offline", true)]
    public async Task StaleDeleteRejectsChangedIdentityOrActiveRunner(string name, string status, bool busy)
    {
        var handler = new QueueHandler(Json($$"""{"total_count":1,"runners":[{"id":42,"name":"{{name}}","status":"{{status}}","busy":{{busy.ToString().ToLowerInvariant()}}}]}"""));
        using var token = await AccessTokenAsync();
        var admin = new GitHubRunnerAdministration(new HttpClient(handler), token, Repository);
        var error = await Assert.ThrowsAsync<RunnerAdminException>(() => admin.DeleteStaleAsync(42, "runner-1", default));
        Assert.Equal(RunnerAdminFailure.IdentityChanged, error.Failure);
        Assert.Single(handler.Methods);
    }

    [Fact]
    public async Task InvalidRepositoryAndUnexpectedHostAreRejectedBeforeHttp()
    {
        using var token = await AccessTokenAsync();
        using var http = new HttpClient(new QueueHandler());
        foreach (var name in new[] { "owner/../exec", "owner/exec?x=1", "https://evil/exec" })
            Assert.Equal(RunnerAdminFailure.InvalidRepository,
                Assert.Throws<RunnerAdminException>(() => new GitHubRunnerAdministration(http, token,
                    Repository with { FullName = name })).Failure);
        Assert.Equal(RunnerAdminFailure.InvalidRepository,
            Assert.Throws<RunnerAdminException>(() => new GitHubRunnerAdministration(http, token,
                Repository with { IsPrivate = false })).Failure);
    }

    [Theory]
    [InlineData(HttpStatusCode.TooManyRequests, RunnerAdminFailure.Transient)]
    [InlineData(HttpStatusCode.BadGateway, RunnerAdminFailure.Transient)]
    [InlineData(HttpStatusCode.Forbidden, RunnerAdminFailure.Unexpected)]
    public async Task RunnerAdminClassifiesHttpFailure(HttpStatusCode status, RunnerAdminFailure expected)
    {
        using var token = await AccessTokenAsync();
        var admin = new GitHubRunnerAdministration(new HttpClient(new QueueHandler(Json("{}", status))), token, Repository);
        var error = await Assert.ThrowsAsync<RunnerAdminException>(() => admin.ListAsync(default));
        Assert.Equal(expected, error.Failure);
    }

    [Theory]
    [InlineData("{")]
    [InlineData("{}")]
    [InlineData("{\"total_count\":1,\"runners\":[]}")]
    public async Task RunnerAdminRejectsMalformedOrInconsistentJson(string body)
    {
        using var token = await AccessTokenAsync();
        var admin = new GitHubRunnerAdministration(new HttpClient(new QueueHandler(Json(body))), token, Repository);
        var error = await Assert.ThrowsAsync<RunnerAdminException>(() => admin.ListAsync(default));
        Assert.Equal(RunnerAdminFailure.Unexpected, error.Failure);
    }

    [Fact]
    public async Task MalformedTokenResponseNeverLeaksBody()
    {
        using var token = await AccessTokenAsync();
        var admin = new GitHubRunnerAdministration(new HttpClient(new QueueHandler(
            Json("""{"token":"SECRET123","expires_at":"bad"}""", HttpStatusCode.Created))), token, Repository);
        var error = await Assert.ThrowsAsync<RunnerAdminException>(() => admin.CreateRegistrationTokenAsync(default));
        Assert.Equal(RunnerAdminFailure.Unexpected, error.Failure);
        Assert.DoesNotContain("SECRET123", error.ToString());
    }

    [Fact]
    public async Task RunnerAdminRejectsNonShortLivedToken()
    {
        using var token = await AccessTokenAsync();
        var admin = new GitHubRunnerAdministration(new HttpClient(new QueueHandler(
            Json("""{"token":"SECRET123","expires_at":"2099-01-01T00:00:00Z"}""", HttpStatusCode.Created))),
            token, Repository, new FixedTimeProvider());
        var error = await Assert.ThrowsAsync<RunnerAdminException>(() => admin.CreateRemoveTokenAsync(default));
        Assert.Equal(RunnerAdminFailure.Unexpected, error.Failure);
        Assert.DoesNotContain("SECRET123", error.ToString());
    }

    [Fact]
    public void BatchBoundaryUsesFixedInterpreterWorkingRootAndStructuredArgs()
    {
        using var root = new FakeRoot();
        var command = Cli.BuildConfigure(new RunnerConfiguration(new Uri("https://github.com/owner/exec"),
            "TOKEN123", "runner-1", "grl-exec", "_work"));
        var info = RunnerBatchBoundary.Build(root.Path, command, Path.Combine(Environment.SystemDirectory, "cmd.exe"));
        Assert.False(info.UseShellExecute);
        Assert.Equal(root.Path, info.WorkingDirectory);
        Assert.Equal(["/d", "/c", "config.cmd", .. command.Arguments], info.ArgumentList);
        Assert.DoesNotContain("TOKEN123", command.ToString());
    }

    [Theory]
    [InlineData("BAD%PATH%")]
    [InlineData("A&B")]
    [InlineData("A|B")]
    [InlineData("A^B")]
    [InlineData("A!B")]
    [InlineData("A\"B")]
    [InlineData("A B")]
    [InlineData("A\nB")]
    public void BatchBoundaryRejectsTokenExpansionAndShellMetacharacters(string hostile)
    {
        using var root = new FakeRoot();
        var command = Cli.BuildRemove(hostile);
        var error = Assert.Throws<RunnerProcessException>(() =>
            RunnerBatchBoundary.Build(root.Path, command, Path.Combine(Environment.SystemDirectory, "cmd.exe")));
        Assert.Equal(RunnerProcessFailure.UnsafeCommand, error.Failure);
        Assert.DoesNotContain(hostile, error.ToString());
    }

    [Fact]
    public void BatchBoundaryRefusesUnreviewedFlagsAndEntrypoints()
    {
        using var root = new FakeRoot();
        var cmd = Path.Combine(Environment.SystemDirectory, "cmd.exe");
        foreach (var command in new[]
        {
            new RunnerCommand("config.cmd", ["--replace"]),
            new RunnerCommand("config.cmd", ["remove", "--token", "TOKEN", "--force"]),
            new RunnerCommand("run.cmd", ["--once"]),
            new RunnerCommand("evil.cmd", [])
        })
            Assert.Throws<RunnerProcessException>(() => RunnerBatchBoundary.Build(root.Path, command, cmd));
    }

    [Fact]
    public void BatchBoundaryRefusesMetacharactersInWorkingRoot()
    {
        using var root = new FakeRoot();
        var hostile = Path.Combine(root.Path, "bad&root");
        Directory.CreateDirectory(hostile);
        File.WriteAllText(Path.Combine(hostile, "config.cmd"), "@echo off");
        var error = Assert.Throws<RunnerProcessException>(() => RunnerBatchBoundary.Build(
            hostile, Cli.BuildRemove("TOKEN"), Path.Combine(Environment.SystemDirectory, "cmd.exe")));
        Assert.Equal(RunnerProcessFailure.InvalidRoot, error.Failure);
    }

    [Fact]
    public async Task ElevatedProcessRefusesBeforeStartingAnything()
    {
        using var root = new FakeRoot();
        var adapter = new PortableRunnerProcess(new RunnerInstallResult(root.Path, RunnerPin.ReviewedSha256, 1, 1, 1), () => true);
        var error = await Assert.ThrowsAsync<RunnerProcessException>(() => adapter.ExecuteAsync(Cli.BuildRemove("TOKEN"), default));
        Assert.Equal(RunnerProcessFailure.Elevated, error.Failure);
    }

    [Fact]
    public async Task NameCollisionRefusesTokenAndProcess()
    {
        var admin = new FakeAdmin { Lists = Pages([Runner(1, online: false)]) };
        var process = new FakeProcess();
        using var lifecycle = NewLifecycle(admin, process);
        var error = await Assert.ThrowsAsync<PortableRunnerException>(() => lifecycle.RegisterAndStartAsync(2, TimeSpan.Zero, default));
        Assert.Equal(PortableRunnerFailure.NameCollision, error.Failure);
        Assert.Equal(0, admin.RegistrationTokenRequests);
        Assert.Empty(process.Commands);
    }

    [Fact]
    public async Task ElevatedLifecycleRefusesBeforeAnyAdminCall()
    {
        var admin = new FakeAdmin();
        using var lifecycle = new PortableRunnerLifecycle(admin, Cli, new FakeProcess(), "runner-1", "grl-exec",
            new NoDelay(), isElevated: () => true);
        var error = await Assert.ThrowsAsync<RunnerProcessException>(() =>
            lifecycle.RegisterAndStartAsync(1, TimeSpan.Zero, default));
        Assert.Equal(RunnerProcessFailure.Elevated, error.Failure);
        Assert.Equal(0, admin.RegistrationTokenRequests);
    }

    [Fact]
    public async Task ConfigureFailureRecordsSafeCompensation()
    {
        var admin = new FakeAdmin { Lists = Pages(Array.Empty<RepositoryRunner>()) };
        var process = new FakeProcess { FailConfigure = true };
        using var lifecycle = NewLifecycle(admin, process);
        await Assert.ThrowsAsync<RunnerProcessException>(() => lifecycle.RegisterAndStartAsync(2, TimeSpan.Zero, default));
        Assert.Equal(PortableRunnerState.RemoteRemovalPending, lifecycle.State);
        Assert.DoesNotContain("SECRET", string.Join(' ', lifecycle.Journal));
        Assert.Equal(0, process.StartCount);
    }

    [Fact]
    public async Task RunStartupFailureLeavesConfiguredEvidenceWithoutToken()
    {
        var admin = new FakeAdmin { Lists = Pages([], [Runner(1, false)], [Runner(1, false)], []) };
        var process = new FakeProcess { FailStart = true };
        using var lifecycle = NewLifecycle(admin, process);
        await Assert.ThrowsAsync<RunnerProcessException>(() => lifecycle.RegisterAndStartAsync(2, TimeSpan.Zero, default));
        Assert.Contains(lifecycle.Journal, x => x.State == PortableRunnerState.Configured);
        Assert.Equal(PortableRunnerState.RemoteRemovalPending, lifecycle.State);
        Assert.DoesNotContain("SECRET", string.Join(' ', lifecycle.Journal));
        Assert.False(lifecycle.HasOwnedProcess);
        await lifecycle.UnregisterAsync(1, TimeSpan.Zero, default);
        Assert.Equal(PortableRunnerState.Removed, lifecycle.State);
        Assert.Equal(0, process.Owned.StopCount);
    }

    [Fact]
    public async Task OnlinePollingSucceedsAndTimeoutLeavesOwnedProcessRunning()
    {
        var admin = new FakeAdmin { Lists = Pages([], [Runner(1, false)], [Runner(1, false)], [Runner(1, true)]) };
        var process = new FakeProcess();
        using var lifecycle = NewLifecycle(admin, process);
        await lifecycle.RegisterAndStartAsync(3, TimeSpan.Zero, default);
        Assert.Equal(PortableRunnerState.Online, lifecycle.State);
        Assert.Equal(0, process.Owned.StopCount);

        var timeoutAdmin = new FakeAdmin { Lists = Pages([], [Runner(1, false)], [Runner(1, false)],
            [Runner(1, true, true)]) };
        var timeoutProcess = new FakeProcess();
        using var timed = NewLifecycle(timeoutAdmin, timeoutProcess);
        var error = await Assert.ThrowsAsync<PortableRunnerException>(() => timed.RegisterAndStartAsync(1, TimeSpan.Zero, default));
        Assert.Equal(PortableRunnerFailure.OnlineTimeout, error.Failure);
        Assert.Equal(0, timeoutProcess.Owned.StopCount);
        Assert.True(timed.HasOwnedProcess);
        Assert.Single(timeoutAdmin.Lists); // A late assignment must not become a kill trigger.
        Assert.Equal(PortableRunnerState.Degraded, timed.State);
    }

    [Fact]
    public async Task DrainFailsClosedAcrossIdleAbsentBusyAndCancellation()
    {
        var admin = new FakeAdmin { Lists = Pages([], [Runner(1, false)], [Runner(1, true)],
            [Runner(1, false)], [], [Runner(1, true, true)]) };
        var process = new FakeProcess();
        using var lifecycle = NewLifecycle(admin, process);
        await lifecycle.RegisterAndStartAsync(1, TimeSpan.Zero, default);
        var remaining = admin.Lists.Count;
        for (var i = 0; i < 3; i++)
        {
            var error = await Assert.ThrowsAsync<PortableRunnerException>(() => lifecycle.DrainAsync(1, TimeSpan.Zero, default));
            Assert.Equal(PortableRunnerFailure.DrainUnavailable, error.Failure);
            Assert.Equal(remaining, admin.Lists.Count);
            Assert.Equal(0, process.Owned.StopCount);
            Assert.True(lifecycle.HasOwnedProcess);
            Assert.Equal(PortableRunnerState.Online, lifecycle.State);
        }
        using var canceled = new CancellationTokenSource();
        canceled.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => lifecycle.DrainAsync(2, TimeSpan.Zero, canceled.Token));
        Assert.Equal(0, process.Owned.StopCount);
    }

    [Fact]
    public async Task StopNowTargetsOnlyOwnedProcessAndUnregisterCanRemainPending()
    {
        var admin = new FakeAdmin { Lists = Pages([], [Runner(1, false)], [Runner(1, true)],
            [Runner(1, false)], [Runner(1, false)]) };
        var process = new FakeProcess();
        using var lifecycle = NewLifecycle(admin, process);
        await lifecycle.RegisterAndStartAsync(1, TimeSpan.Zero, default);
        await lifecycle.StopNowAsync(default);
        Assert.Equal(1, process.Owned.StopCount);
        Assert.Equal(PortableRunnerState.Paused, lifecycle.State);
        await lifecycle.UnregisterAsync(1, TimeSpan.Zero, default);
        Assert.Equal(PortableRunnerState.RemoteRemovalPending, lifecycle.State);
        Assert.Equal(1, admin.RemoveTokenRequests);
        Assert.Equal("remove", process.Operations.Last());
    }

    [Fact]
    public async Task UnregisterRefusesLiveProcessEvenIfNextStatusIsAbsentOrIdle()
    {
        var admin = new FakeAdmin { Lists = Pages([], [Runner(1, false)], [Runner(1, true)], [], [Runner(1, false)]) };
        var process = new FakeProcess();
        using var lifecycle = NewLifecycle(admin, process);
        await lifecycle.RegisterAndStartAsync(1, TimeSpan.Zero, default);
        var remaining = admin.Lists.Count;
        var error = await Assert.ThrowsAsync<PortableRunnerException>(() => lifecycle.UnregisterAsync(1, TimeSpan.Zero, default));
        Assert.Equal(PortableRunnerFailure.ActiveProcess, error.Failure);
        Assert.Equal(0, process.Owned.StopCount);
        Assert.Equal(0, admin.RemoveTokenRequests);
        Assert.Equal(remaining, admin.Lists.Count);
    }

    [Fact]
    public async Task UnregisterSuccessVerifiesRemoteDisappearance()
    {
        var admin = new FakeAdmin { Lists = Pages([], [Runner(1, false)], [Runner(1, true)],
            [Runner(1, false)], []) };
        var process = new FakeProcess();
        using var lifecycle = NewLifecycle(admin, process);
        await lifecycle.RegisterAndStartAsync(1, TimeSpan.Zero, default);
        await lifecycle.StopNowAsync(default);
        await lifecycle.UnregisterAsync(1, TimeSpan.Zero, default);
        Assert.Equal(PortableRunnerState.Removed, lifecycle.State);
        Assert.Equal(1, process.Owned.StopCount);
    }

    [Fact]
    public async Task UnregisterRemoteUnavailablePreservesPendingEvidence()
    {
        var admin = new FakeAdmin { Lists = Pages([], [Runner(1, false)], [Runner(1, true)], [Runner(1, false)]) };
        var process = new FakeProcess();
        using var lifecycle = NewLifecycle(admin, process);
        await lifecycle.RegisterAndStartAsync(1, TimeSpan.Zero, default);
        await lifecycle.StopNowAsync(default);
        await Assert.ThrowsAsync<InvalidOperationException>(() => lifecycle.UnregisterAsync(1, TimeSpan.Zero, default));
        Assert.Equal(PortableRunnerState.RemoteRemovalPending, lifecycle.State);
        Assert.Equal(1, process.Owned.StopCount);
        Assert.DoesNotContain("SECRET", string.Join(' ', lifecycle.Journal));
    }

    [Fact]
    public async Task UnregisterRefusesUnknownOnlineProcessAfterOwnedExit()
    {
        var admin = new FakeAdmin { Lists = Pages([], [Runner(1, false)], [Runner(1, true)], [Runner(1, true)]) };
        var process = new FakeProcess();
        using var lifecycle = NewLifecycle(admin, process);
        await lifecycle.RegisterAndStartAsync(1, TimeSpan.Zero, default);
        process.Owned.ExitIndependently();
        var error = await Assert.ThrowsAsync<PortableRunnerException>(() =>
            lifecycle.UnregisterAsync(1, TimeSpan.Zero, default));
        Assert.Equal(PortableRunnerFailure.IdentityUncertain, error.Failure);
        Assert.Equal(0, process.Owned.StopCount);
        Assert.Equal(0, admin.RemoveTokenRequests);
        Assert.Equal(PortableRunnerState.RemoteRemovalPending, lifecycle.State);
    }

    [Fact]
    public async Task CancelAfterStartNeverStopsOwnedProcessOrLosesRecoveryState()
    {
        using var cancellation = new CancellationTokenSource();
        var admin = new FakeAdmin { Lists = Pages([], [Runner(1, false)]) };
        var process = new FakeProcess { OnStarted = cancellation.Cancel };
        using var lifecycle = NewLifecycle(admin, process);
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            lifecycle.RegisterAndStartAsync(2, TimeSpan.Zero, cancellation.Token));
        Assert.True(lifecycle.HasOwnedProcess);
        Assert.Equal(0, process.Owned.StopCount);
        Assert.Equal(PortableRunnerState.Degraded, lifecycle.State);
        Assert.Equal(1, lifecycle.RunnerId);
        Assert.DoesNotContain("SECRET", string.Join(' ', lifecycle.Journal));
    }

    [Fact]
    public async Task DisposingCoordinatorDoesNotStopAnActiveRunner()
    {
        var admin = new FakeAdmin { Lists = Pages([], [Runner(1, false)], [Runner(1, true)]) };
        var process = new FakeProcess();
        var lifecycle = NewLifecycle(admin, process);
        await lifecycle.RegisterAndStartAsync(1, TimeSpan.Zero, default);
        lifecycle.Dispose();
        Assert.Equal(0, process.Owned.StopCount);
        Assert.False(process.Owned.HasExited);
    }

    [Fact]
    public async Task ResumeTimeoutDoesNotStopNewProcess()
    {
        var admin = new FakeAdmin { Lists = Pages([], [Runner(1, false)], [Runner(1, true)], [Runner(1, false)]) };
        var process = new FakeProcess();
        using var lifecycle = NewLifecycle(admin, process);
        await lifecycle.RegisterAndStartAsync(1, TimeSpan.Zero, default);
        await lifecycle.StopNowAsync(default);
        var first = process.Owned;
        await Assert.ThrowsAsync<PortableRunnerException>(() => lifecycle.ResumeAsync(1, TimeSpan.Zero, default));
        Assert.Equal(1, first.StopCount);
        Assert.Equal(0, process.Owned.StopCount);
        Assert.True(lifecycle.HasOwnedProcess);
        Assert.Equal(PortableRunnerState.Degraded, lifecycle.State);
    }

    [Fact]
    public async Task ResumeCancellationDoesNotStopNewProcess()
    {
        var admin = new FakeAdmin { Lists = Pages([], [Runner(1, false)], [Runner(1, true)]) };
        var process = new FakeProcess();
        using var lifecycle = NewLifecycle(admin, process);
        await lifecycle.RegisterAndStartAsync(1, TimeSpan.Zero, default);
        await lifecycle.StopNowAsync(default);
        using var cancellation = new CancellationTokenSource();
        process.OnStarted = cancellation.Cancel;
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            lifecycle.ResumeAsync(2, TimeSpan.Zero, cancellation.Token));
        Assert.Equal(0, process.Owned.StopCount);
        Assert.True(lifecycle.HasOwnedProcess);
        Assert.Equal(PortableRunnerState.Degraded, lifecycle.State);
    }

    [Fact]
    public async Task PostConfigureIdentityPollRetriesTransientAndAbsenceBeforeStarting()
    {
        var admin = new FakeAdmin
        {
            Lists = Pages([], [], [Runner(1, false)], [Runner(1, true)])
        };
        admin.FailListCalls.Add(2);
        var process = new FakeProcess();
        using var lifecycle = NewLifecycle(admin, process);

        await lifecycle.RegisterAndStartAsync(3, TimeSpan.Zero, default);

        Assert.Equal(PortableRunnerState.Online, lifecycle.State);
        Assert.Equal(1, lifecycle.RunnerId);
        Assert.Equal(1, process.StartCount);
        Assert.Equal(1, process.VerifyCount);
        Assert.True(admin.ListCalls >= 4);
    }

    [Fact]
    public async Task PostConfigureTransientFailureCanRecoverByExplicitReresolution()
    {
        var admin = new FakeAdmin
        {
            Lists = Pages([], [Runner(42, false)], [Runner(42, false)], [])
        };
        admin.FailListCalls.Add(2);
        var process = new FakeProcess();
        using var lifecycle = NewLifecycle(admin, process);

        var initial = await Assert.ThrowsAsync<PortableRunnerException>(() =>
            lifecycle.RegisterAndStartAsync(1, TimeSpan.Zero, default));
        Assert.Equal(PortableRunnerFailure.RemoteUnavailable, initial.Failure);
        Assert.Null(lifecycle.RunnerId);
        Assert.Equal(PortableRunnerState.RemoteRemovalPending, lifecycle.State);
        Assert.Equal(0, process.StartCount);

        await lifecycle.UnregisterAsync(1, TimeSpan.Zero, default);

        Assert.Equal(42, lifecycle.RunnerId);
        Assert.Equal(PortableRunnerState.Removed, lifecycle.State);
        Assert.Equal(1, admin.RemoveTokenRequests);
    }

    [Fact]
    public async Task ConfigureFailureWithUnknownIdCanReresolveAndRecover()
    {
        var admin = new FakeAdmin { Lists = Pages([], [], [Runner(42, false)], [Runner(42, false)], []) };
        var process = new FakeProcess { FailConfigure = true };
        using var lifecycle = NewLifecycle(admin, process);

        await Assert.ThrowsAsync<RunnerProcessException>(() =>
            lifecycle.RegisterAndStartAsync(1, TimeSpan.Zero, default));
        Assert.Null(lifecycle.RunnerId);
        Assert.Equal(PortableRunnerState.RemoteRemovalPending, lifecycle.State);

        await lifecycle.UnregisterAsync(1, TimeSpan.Zero, default);

        Assert.Equal(42, lifecycle.RunnerId);
        Assert.Equal(PortableRunnerState.Removed, lifecycle.State);
        Assert.Equal(1, admin.RemoveTokenRequests);
        Assert.Equal(0, process.StartCount);
    }

    [Fact]
    public async Task ConfigureFailurePersistsUniqueIdBeforeRecovery()
    {
        var admin = new FakeAdmin
        {
            Lists = Pages([], [Runner(42, false)], [Runner(42, false)], [])
        };
        var process = new FakeProcess { FailConfigure = true };
        using var lifecycle = NewLifecycle(admin, process);

        await Assert.ThrowsAsync<RunnerProcessException>(() =>
            lifecycle.RegisterAndStartAsync(1, TimeSpan.Zero, default));

        Assert.Equal(42, lifecycle.RunnerId);
        Assert.Equal(PortableRunnerState.RemoteRemovalPending, lifecycle.State);
        await lifecycle.UnregisterAsync(1, TimeSpan.Zero, default);
        Assert.Equal(PortableRunnerState.Removed, lifecycle.State);
        Assert.Equal(1, admin.RemoveTokenRequests);
    }

    [Fact]
    public async Task ConfigureFailureWithDuplicateExactNamesStaysEvidenceBlocked()
    {
        var duplicates = new[] { Runner(41, false), Runner(42, false) };
        var admin = new FakeAdmin { Lists = Pages([], duplicates, duplicates) };
        var process = new FakeProcess { FailConfigure = true };
        using var lifecycle = NewLifecycle(admin, process);

        await Assert.ThrowsAsync<RunnerProcessException>(() =>
            lifecycle.RegisterAndStartAsync(1, TimeSpan.Zero, default));
        Assert.Null(lifecycle.RunnerId);

        var error = await Assert.ThrowsAsync<PortableRunnerException>(() =>
            lifecycle.UnregisterAsync(1, TimeSpan.Zero, default));

        Assert.Equal(PortableRunnerFailure.IdentityUncertain, error.Failure);
        Assert.Equal(0, admin.RemoveTokenRequests);
        Assert.Equal(0, process.StartCount);
    }

    [Fact]
    public async Task ConfigureTimeoutStillPersistsResolvableIdWithoutStartingRunner()
    {
        var admin = new FakeAdmin { Lists = Pages([], [Runner(7, false)]) };
        var process = new FakeProcess { ConfigureException = new OperationCanceledException("bounded configure timeout") };
        using var lifecycle = NewLifecycle(admin, process);

        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            lifecycle.RegisterAndStartAsync(1, TimeSpan.Zero, default));

        Assert.Equal(7, lifecycle.RunnerId);
        Assert.Equal(PortableRunnerState.RemoteRemovalPending, lifecycle.State);
        Assert.Equal(0, process.StartCount);
        Assert.Equal(0, process.VerifyCount);
    }

    [Fact]
    public async Task UnknownIdRecoveryRefusesBusyRunnerAndPossibleSurvivor()
    {
        var busyAdmin = new FakeAdmin { Lists = Pages([], [], [Runner(8, true, true)]) };
        var busyProcess = new FakeProcess { FailConfigure = true };
        using var busy = NewLifecycle(busyAdmin, busyProcess);
        await Assert.ThrowsAsync<RunnerProcessException>(() =>
            busy.RegisterAndStartAsync(1, TimeSpan.Zero, default));
        var busyError = await Assert.ThrowsAsync<PortableRunnerException>(() =>
            busy.UnregisterAsync(1, TimeSpan.Zero, default));
        Assert.Equal(PortableRunnerFailure.IdentityUncertain, busyError.Failure);
        Assert.Equal(0, busyAdmin.RemoveTokenRequests);

        var survivorAdmin = new FakeAdmin { Lists = Pages([], [Runner(9, false)]) };
        var survivorProcess = new FakeProcess { StartException = new InvalidOperationException("unknown start outcome") };
        using var survivor = NewLifecycle(survivorAdmin, survivorProcess);
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            survivor.RegisterAndStartAsync(1, TimeSpan.Zero, default));
        Assert.False(survivor.HasOwnedProcess);
        var survivorError = await Assert.ThrowsAsync<PortableRunnerException>(() =>
            survivor.UnregisterAsync(1, TimeSpan.Zero, default));
        Assert.Equal(PortableRunnerFailure.PossibleSurvivingProcess, survivorError.Failure);
        Assert.Equal(0, survivorAdmin.RemoveTokenRequests);
    }

    [Fact]
    public async Task ReopenedPersistedIdRemovesThroughRealCompositionOnlyAfterRecheck()
    {
        using var root = new FakeRoot();
        using var store = PortableRecoveryStore.Open(root.Path, "owner/exec", "runner-1");
        store.Write(42, PortableRunnerState.Configured);
        var evidence = Assert.IsType<PortableRecoveryEvidence>(store.Read());
        var admin = new FakeAdmin { Lists = Pages([Runner(42, false)], []) };
        using var adapter = new LiveWizardAdapters(
            new LiveRuntimeOptions("clientidxx", "owner/exec", "runner-1", root.Path),
            admin, store, evidence, new NoDelay());

        var result = await adapter.RemoveAsync();

        Assert.Equal(WizardEvent.RemoteRemoved, result);
        Assert.Equal(2, admin.ListCalls); // one identity/state recheck, then post-delete absence verification
        Assert.Equal(1, admin.DeleteStaleRequests);
        Assert.Equal(PortableRunnerState.Removed, store.Read()!.State);
    }

    [Fact]
    public async Task ReadyRecordRepeatedWizardRecoverNeverPromotesNameMatch()
    {
        using var root = new FakeRoot();
        using var store = PortableRecoveryStore.Open(root.Path, "owner/exec", "runner-1");
        store.Write(null, PortableRunnerState.Ready);
        var evidence = Assert.IsType<PortableRecoveryEvidence>(store.Read());
        var original = File.ReadAllBytes(Path.Combine(root.Path, ".grl-recovery.json"));
        var admin = new FakeAdmin { Lists = Pages([Runner(55, false)]) };
        using var adapter = new LiveWizardAdapters(
            new LiveRuntimeOptions("clientidxx", "owner/exec", "runner-1", root.Path),
            admin, store, evidence, new NoDelay());
        var session = NewRecoverySession(adapter, root.Path);

        await session.RecoverAsync();
        await session.RecoverAsync();

        Assert.Equal(WizardState.DisconnectRemotePending, session.State);
        Assert.Equal("DISCONNECT_IDENTITY_BLOCKED", session.ActiveError?.Code);
        Assert.Contains("Settings > Actions > Runners", session.ErrorNextStep);
        Assert.Equal(evidence, store.Read());
        Assert.Equal(original, File.ReadAllBytes(Path.Combine(root.Path, ".grl-recovery.json")));
        Assert.Equal(0, admin.ListCalls);
        Assert.Equal(0, admin.DeleteStaleRequests);
        Assert.Equal(0, admin.RemoveTokenRequests);
    }

    [Theory]
    [InlineData(PortableRunnerState.Configuring)] // persisted before config.cmd ExecuteAsync
    [InlineData(PortableRunnerState.RemoteRemovalPending)]
    public async Task ReopenedIdlessPreExecutionOrPendingRecordCannotDeleteByName(PortableRunnerState state)
    {
        using var root = new FakeRoot();
        using var store = PortableRecoveryStore.Open(root.Path, "owner/exec", "runner-1");
        store.Write(null, state);
        var evidence = Assert.IsType<PortableRecoveryEvidence>(store.Read());
        var original = File.ReadAllBytes(Path.Combine(root.Path, ".grl-recovery.json"));
        var admin = new FakeAdmin { Lists = Pages([Runner(55, false)]) };
        using var adapter = new LiveWizardAdapters(
            new LiveRuntimeOptions("clientidxx", "owner/exec", "runner-1", root.Path),
            admin, store, evidence, new NoDelay());

        for (var attempt = 0; attempt < 2; attempt++)
        {
            var error = await Assert.ThrowsAsync<AdapterOperationException>(() => adapter.RemoveAsync());
            Assert.Equal("DISCONNECT_IDENTITY_BLOCKED", error.ErrorCode);
        }

        Assert.Equal(evidence, store.Read());
        Assert.Equal(original, File.ReadAllBytes(Path.Combine(root.Path, ".grl-recovery.json")));
        Assert.Equal(0, admin.ListCalls);
        Assert.Equal(0, admin.DeleteStaleRequests);
        Assert.Equal(0, admin.RemoveTokenRequests);
    }

    [Fact]
    public async Task ReopenedPersistedIdMismatchRefusesRepeatedRecoverWithoutMutation()
    {
        using var root = new FakeRoot();
        using var store = PortableRecoveryStore.Open(root.Path, "owner/exec", "runner-1");
        store.Write(42, PortableRunnerState.Configured);
        var evidence = Assert.IsType<PortableRecoveryEvidence>(store.Read());
        var original = File.ReadAllBytes(Path.Combine(root.Path, ".grl-recovery.json"));
        var admin = new FakeAdmin { Lists = Pages([Runner(55, false)], [Runner(55, false)]) };
        using var adapter = new LiveWizardAdapters(
            new LiveRuntimeOptions("clientidxx", "owner/exec", "runner-1", root.Path),
            admin, store, evidence, new NoDelay());

        for (var attempt = 0; attempt < 2; attempt++)
        {
            var error = await Assert.ThrowsAsync<AdapterOperationException>(() => adapter.RemoveAsync());
            Assert.Equal("DISCONNECT_IDENTITY_BLOCKED", error.ErrorCode);
        }

        Assert.Equal(evidence, store.Read());
        Assert.Equal(original, File.ReadAllBytes(Path.Combine(root.Path, ".grl-recovery.json")));
        Assert.Equal(2, admin.ListCalls);
        Assert.Equal(0, admin.DeleteStaleRequests);
        Assert.Equal(0, admin.RemoveTokenRequests);
    }

    [Fact]
    public async Task ReopenedDeleteFailurePreservesPersistedIdAndOriginalState()
    {
        using var root = new FakeRoot();
        using var store = PortableRecoveryStore.Open(root.Path, "owner/exec", "runner-1");
        store.Write(42, PortableRunnerState.Configured);
        var evidence = Assert.IsType<PortableRecoveryEvidence>(store.Read());
        var original = File.ReadAllBytes(Path.Combine(root.Path, ".grl-recovery.json"));
        var admin = new FakeAdmin
        {
            Lists = Pages([Runner(42, false)]),
            DeleteException = new InvalidOperationException("remote delete unavailable")
        };
        using var adapter = new LiveWizardAdapters(
            new LiveRuntimeOptions("clientidxx", "owner/exec", "runner-1", root.Path),
            admin, store, evidence, new NoDelay());

        var error = await Assert.ThrowsAsync<AdapterOperationException>(() => adapter.RemoveAsync());

        Assert.Equal("DISCONNECT_REMOTE_UNAVAILABLE", error.ErrorCode);
        var pending = Assert.IsType<PortableRecoveryEvidence>(store.Read());
        Assert.Equal(evidence, pending);
        Assert.Equal(original, File.ReadAllBytes(Path.Combine(root.Path, ".grl-recovery.json")));
        Assert.Equal(42, pending.RunnerId);
        Assert.Equal(PortableRunnerState.Configured, pending.State);
        Assert.False(pending.MayHaveUnownedProcess);
        Assert.Equal(1, admin.DeleteStaleRequests);
    }

    [Fact]
    public async Task ReopenedSurvivorEvidenceRefusesRemoteDeleteWithoutListing()
    {
        using var root = new FakeRoot();
        using var store = PortableRecoveryStore.Open(root.Path, "owner/exec", "runner-1");
        store.Write(42, PortableRunnerState.Degraded, mayHaveUnownedProcess: true);
        var evidence = Assert.IsType<PortableRecoveryEvidence>(store.Read());
        var original = File.ReadAllBytes(Path.Combine(root.Path, ".grl-recovery.json"));
        var admin = new FakeAdmin();
        using var adapter = new LiveWizardAdapters(
            new LiveRuntimeOptions("clientidxx", "owner/exec", "runner-1", root.Path),
            admin, store, evidence, new NoDelay());

        for (var attempt = 0; attempt < 2; attempt++)
        {
            var error = await Assert.ThrowsAsync<AdapterOperationException>(() => adapter.RemoveAsync());
            Assert.Equal("DISCONNECT_PROCESS_UNCERTAIN", error.ErrorCode);
        }
        Assert.Equal(evidence, store.Read());
        Assert.Equal(original, File.ReadAllBytes(Path.Combine(root.Path, ".grl-recovery.json")));
        Assert.Equal(0, admin.ListCalls);
        Assert.Equal(0, admin.DeleteStaleRequests);
        Assert.Equal(0, admin.RemoveTokenRequests);
    }

    [Fact]
    public async Task ResumeVersionDriftFailsBeforeSecondRunStartOrMetadataInjection()
    {
        var admin = new FakeAdmin { Lists = Pages([], [Runner(1, false)], [Runner(1, true)]) };
        var process = new FakeProcess();
        process.VerifiedClis.Enqueue(Cli);
        process.VerifiedClis.Enqueue(new FakeCli("9.9.9"));
        using var lifecycle = NewLifecycle(admin, process);

        await lifecycle.RegisterAndStartAsync(1, TimeSpan.Zero, default);
        await lifecycle.StopNowAsync(default);

        var error = await Assert.ThrowsAsync<PortableRunnerException>(() =>
            lifecycle.ResumeAsync(1, TimeSpan.Zero, default));

        Assert.Equal(PortableRunnerFailure.UnsupportedVersion, error.Failure);
        Assert.Equal(PortableRunnerState.Paused, lifecycle.State);
        Assert.Equal(2, process.VerifyCount);
        Assert.Equal(1, process.StartCount);
        Assert.Single(process.StartedCommands);
        Assert.Single(process.StartVersions);
        Assert.Equal(RunnerPin.ReviewedVersion, process.StartVersions[0]);
    }

    [Fact]
    public void RunEnvironmentContainsVerifiedNonSecretMetadataOnly()
    {
        var info = new ProcessStartInfo("unused") { UseShellExecute = false };
        RunnerBatchBoundary.SetRunEnvironment(info, Cli.Capabilities.Version);
        Assert.Equal("2.337.0", info.Environment["GRL_RUNNER_VERSION"]);
        Assert.Equal("portable-user", info.Environment["GRL_IDENTITY_CLASS"]);
        Assert.DoesNotContain("SECRET", info.Environment["GRL_RUNNER_VERSION"] + info.Environment["GRL_IDENTITY_CLASS"]);
        Assert.Throws<RunnerProcessException>(() => RunnerBatchBoundary.SetRunEnvironment(info, "unknown"));
    }

    private static PortableRunnerLifecycle NewLifecycle(FakeAdmin admin, FakeProcess process) =>
        new(admin, Cli, process, "runner-1", "grl-exec", new NoDelay(), isElevated: () => false);

    private static WizardSession NewRecoverySession(LiveWizardAdapters adapter, string root) =>
        new(new ScenarioSelection(FakeScenario.HappyPath, string.Empty), new TestClock(),
            new PreviewDelay(TimeSpan.Zero),
            new WizardAdapters(adapter, adapter, adapter, adapter, adapter, adapter, adapter),
            WizardState.DisconnectRemotePending, live: true, liveLocation: root);

    private sealed class TestClock : IClock
    {
        public DateTimeOffset UtcNow => new(2026, 9, 25, 0, 0, 0, TimeSpan.Zero);
    }

    private static Queue<IReadOnlyList<RepositoryRunner>> Pages(params IReadOnlyList<RepositoryRunner>[] pages) => new(pages);

    private static RepositoryRunner Runner(long id, bool online, bool busy = false) =>
        new(id, "runner-1", online, busy);

    private static HttpResponseMessage Json(string body, HttpStatusCode status = HttpStatusCode.OK) =>
        new(status) { Content = new StringContent(body, Encoding.UTF8, "application/json") };

    private static async Task<GitHubAccessToken> AccessTokenAsync()
    {
        var flow = new GitHubDeviceFlow(new HttpClient(new QueueHandler(
            Json("""{"device_code":"dev","user_code":"ABCD","verification_uri":"https://github.com/login/device","expires_in":900,"interval":1}"""),
            Json("""{"access_token":"ACCESS","token_type":"bearer"}"""))), "client", new NoDelay());
        using var code = await flow.RequestDeviceCodeAsync(default);
        return await flow.PollForTokenAsync(code, default);
    }

    private sealed class QueueHandler(params HttpResponseMessage[] responses) : HttpMessageHandler
    {
        private readonly Queue<HttpResponseMessage> queue = new(responses);
        public List<string> Uris { get; } = [];
        public List<HttpMethod> Methods { get; } = [];
        public List<string?> Schemes { get; } = [];
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            Uris.Add(request.RequestUri!.ToString());
            Methods.Add(request.Method);
            Schemes.Add(request.Headers.Authorization?.Scheme);
            return Task.FromResult(queue.Dequeue());
        }
    }

    private sealed class NoDelay : IAsyncDelay
    {
        public Task WaitAsync(TimeSpan delay, CancellationToken ct) { ct.ThrowIfCancellationRequested(); return Task.CompletedTask; }
    }

    private sealed class FixedTimeProvider : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => new(2026, 9, 25, 4, 30, 0, TimeSpan.Zero);
    }

    private sealed class FakeAdmin : IRunnerAdministration
    {
        public GitHubRepository Repository => PortableLifecycleTests.Repository;
        public Queue<IReadOnlyList<RepositoryRunner>> Lists { get; set; } = new();
        public HashSet<int> FailListCalls { get; } = [];
        public int ListCalls { get; private set; }
        public int RegistrationTokenRequests { get; private set; }
        public int RemoveTokenRequests { get; private set; }
        public int DeleteStaleRequests { get; private set; }
        public Exception? DeleteException { get; set; }
        public Task<IReadOnlyList<RepositoryRunner>> ListAsync(CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            ListCalls++;
            if (FailListCalls.Contains(ListCalls))
                throw new InvalidOperationException("transient list failure");
            return Task.FromResult(Lists.Dequeue());
        }
        public Task<RunnerOneHourToken> CreateRegistrationTokenAsync(CancellationToken ct)
        {
            RegistrationTokenRequests++;
            return Task.FromResult(new RunnerOneHourToken("SECRETREG"));
        }
        public Task<RunnerOneHourToken> CreateRemoveTokenAsync(CancellationToken ct)
        {
            RemoveTokenRequests++;
            return Task.FromResult(new RunnerOneHourToken("SECRETREMOVE"));
        }
        public Task DeleteStaleAsync(long id, string confirmedName, CancellationToken ct)
        {
            DeleteStaleRequests++;
            if (DeleteException is not null) throw DeleteException;
            return Task.CompletedTask;
        }
    }

    private sealed class FakeProcess : IRunnerProcessAdapter
    {
        public List<RunnerCommand> Commands { get; } = [];
        public List<string> Operations { get; } = [];
        public List<RunnerCommand> StartedCommands { get; } = [];
        public List<string> StartVersions { get; } = [];
        public Queue<IRunnerCli> VerifiedClis { get; } = new();
        public FakeOwned Owned { get; private set; } = new();
        public Action? OnStarted { get; set; }
        public bool FailConfigure { get; set; }
        public bool FailStart { get; set; }
        public Exception? ConfigureException { get; set; }
        public Exception? StartException { get; set; }
        public int StartCount { get; private set; }
        public int VerifyCount { get; private set; }
        public Task ExecuteAsync(RunnerCommand command, CancellationToken ct)
        {
            Commands.Add(command);
            Operations.Add(command.Arguments[0]);
            var isConfigure = command.Arguments.FirstOrDefault() == "--unattended";
            if (isConfigure && ConfigureException is not null) throw ConfigureException;
            if (isConfigure && FailConfigure)
                throw new RunnerProcessException(RunnerProcessFailure.CommandFailed, "Configuration failed.");
            return Task.CompletedTask;
        }
        public Task<IRunnerCli> VerifyCliAsync(CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            VerifyCount++;
            return Task.FromResult(VerifiedClis.Count > 0 ? VerifiedClis.Dequeue() : Cli);
        }
        public Task<IOwnedRunnerProcess> StartAsync(RunnerCommand command, string verifiedVersion, CancellationToken ct)
        {
            StartCount++;
            StartedCommands.Add(command);
            StartVersions.Add(verifiedVersion);
            if (StartException is not null) throw StartException;
            if (FailStart) throw new RunnerProcessException(RunnerProcessFailure.StartupFailed, "Start failed.");
            Assert.Equal(RunnerPin.ReviewedVersion, verifiedVersion);
            Owned = new FakeOwned();
            OnStarted?.Invoke();
            return Task.FromResult<IOwnedRunnerProcess>(Owned);
        }
    }

    private sealed class FakeCli(string version) : IRunnerCli
    {
        public RunnerCapabilities Capabilities { get; } = new(version, Cli.Capabilities.Options);
        public RunnerCommand BuildConfigure(RunnerConfiguration configuration) => Cli.BuildConfigure(configuration);
        public RunnerCommand BuildRemove(string removalToken) => Cli.BuildRemove(removalToken);
        public RunnerCommand BuildRun() => Cli.BuildRun();
    }

    private sealed class FakeOwned : IOwnedRunnerProcess
    {
        public bool HasExited { get; private set; }
        public int StopCount { get; private set; }
        public void ExitIndependently() => HasExited = true;
        public Task StopAsync(CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            StopCount++;
            HasExited = true;
            return Task.CompletedTask;
        }
        public void Dispose() { }
    }

    private sealed class FakeRoot : IDisposable
    {
        public string Path { get; } = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "grl-d-boundary-" + Guid.NewGuid().ToString("N"));
        public FakeRoot()
        {
            Directory.CreateDirectory(Path);
            File.WriteAllText(System.IO.Path.Combine(Path, "config.cmd"), "@echo off");
            File.WriteAllText(System.IO.Path.Combine(Path, "run.cmd"), "@echo off");
        }
        public void Dispose() => Directory.Delete(Path, recursive: true);
    }
}
