using System.Diagnostics;
using Grl.App.Presentation;
using Grl.Core;
using Grl.Integration;

namespace Grl.Integration.Tests;

// Issue #21 / GRL-015: a fresh app process resumes ONLY a planned pause (explicit Stop Now,
// persisted Paused + numeric ID + no possible survivor) with the existing run.cmd.
public sealed partial class PortableLifecycleTests
{
    [Fact]
    public async Task ReopenedPlannedPauseResumesSameIdWithExistingRunOnly()
    {
        using var root = new FakeRoot();
        using var store = PausedStore(root);
        var admin = new FakeAdmin { Lists = Pages([Runner(42, false)], [Runner(42, false)], [Runner(42, true)]) };
        var process = new FakeProcess();
        using var lifecycle = Restore(admin, process, store);
        Assert.True(lifecycle.IsRestored);
        Assert.Equal(PortableRunnerState.Paused, lifecycle.State);
        Assert.Equal(0, admin.ListCalls); // restoring reads local evidence only

        await lifecycle.ResumeAsync(3, TimeSpan.Zero, default);

        Assert.Equal(PortableRunnerState.Online, lifecycle.State);
        Assert.Equal(42, lifecycle.RunnerId);
        Assert.True(lifecycle.HasOwnedProcess);
        var started = Assert.Single(process.StartedCommands);
        Assert.Equal("run.cmd", started.EntryPoint);
        Assert.Empty(started.Arguments);
        Assert.Empty(process.Commands); // no config.cmd of any kind
        Assert.Equal(0, process.VerifyCount); // no config.cmd --help probe either
        Assert.Equal(1, process.ListenerVerifyCount);
        Assert.Equal(RunnerPin.ReviewedVersion, Assert.Single(process.StartVersions));
        Assert.Equal(0, admin.RegistrationTokenRequests);
        Assert.Equal(0, admin.RemoveTokenRequests);
        Assert.Equal(0, admin.DeleteStaleRequests);
        var online = Assert.IsType<PortableRecoveryEvidence>(store.Read());
        Assert.Equal((42L, PortableRunnerState.Online, true), (online.RunnerId!.Value, online.State, online.MayHaveUnownedProcess));
        Assert.DoesNotContain("SECRET", string.Join(' ', lifecycle.Journal));
    }

    [Fact]
    public async Task ReopenedResumeRepeatedOrConcurrentNeverStartsSecondProcess()
    {
        using var root = new FakeRoot();
        using var store = PausedStore(root);
        var gate = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var admin = new FakeAdmin { Lists = Pages([Runner(42, false)], [Runner(42, true)]), ListGate = gate };
        var process = new FakeProcess();
        using var lifecycle = Restore(admin, process, store);

        var first = lifecycle.ResumeAsync(2, TimeSpan.Zero, default);
        await admin.ListEntered.Task.WaitAsync(TimeSpan.FromSeconds(5));
        // Bounded: without the guard, a second resume would wait on the same gate instead of refusing.
        var concurrent = await Assert.ThrowsAsync<PortableRunnerException>(() =>
            lifecycle.ResumeAsync(2, TimeSpan.Zero, default).WaitAsync(TimeSpan.FromSeconds(5)));
        Assert.Equal(PortableRunnerFailure.InvalidState, concurrent.Failure);
        gate.SetResult(true);
        await first;

        var repeated = await Assert.ThrowsAsync<PortableRunnerException>(() =>
            lifecycle.ResumeAsync(2, TimeSpan.Zero, default));
        Assert.Equal(PortableRunnerFailure.InvalidState, repeated.Failure);
        Assert.Equal(PortableRunnerState.Online, lifecycle.State);
        Assert.Equal(1, process.StartCount);
    }

    [Theory]
    [InlineData("absent")]
    [InlineData("other-id")]
    [InlineData("duplicate-name")]
    [InlineData("id-renamed")]
    public async Task ReopenedResumeRefusesIdOrNameConflictWithoutMutation(string conflict)
    {
        using var root = new FakeRoot();
        using var store = PausedStore(root);
        var original = RecoveryBytes(root);
        IReadOnlyList<RepositoryRunner> listed = conflict switch
        {
            "absent" => [],
            "other-id" => [Runner(55, false)],
            "duplicate-name" => [Runner(42, false), Runner(55, false)],
            _ => [new RepositoryRunner(42, "renamed", false, false)]
        };
        var admin = new FakeAdmin { Lists = Pages(listed) };
        var process = new FakeProcess();
        using var lifecycle = Restore(admin, process, store);

        var error = await Assert.ThrowsAsync<PortableRunnerException>(() => lifecycle.ResumeAsync(1, TimeSpan.Zero, default));

        Assert.Equal(PortableRunnerFailure.IdentityUncertain, error.Failure);
        AssertNothingStarted(admin, process);
        Assert.Equal(original, RecoveryBytes(root));
        Assert.Equal(PortableRunnerState.Paused, lifecycle.State);
    }

    [Theory]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(true, true)]
    public async Task ReopenedResumeRefusesOnlineOrBusyRunnerAndAdoptsNothing(bool online, bool busy)
    {
        using var root = new FakeRoot();
        using var store = PausedStore(root);
        var original = RecoveryBytes(root);
        var admin = new FakeAdmin { Lists = Pages([Runner(42, online, busy)]) };
        var process = new FakeProcess();
        using var lifecycle = Restore(admin, process, store);

        var error = await Assert.ThrowsAsync<PortableRunnerException>(() => lifecycle.ResumeAsync(1, TimeSpan.Zero, default));

        Assert.Equal(PortableRunnerFailure.RemoteRunnerActive, error.Failure);
        AssertNothingStarted(admin, process);
        Assert.False(lifecycle.HasOwnedProcess);
        Assert.Equal(original, RecoveryBytes(root));
    }

    [Fact]
    public async Task ReopenedResumeRemoteUnavailableStartsNothing()
    {
        using var root = new FakeRoot();
        using var store = PausedStore(root);
        var original = RecoveryBytes(root);
        var admin = new FakeAdmin();
        admin.FailListCalls.Add(1);
        var process = new FakeProcess();
        using var lifecycle = Restore(admin, process, store);

        var error = await Assert.ThrowsAsync<PortableRunnerException>(() => lifecycle.ResumeAsync(1, TimeSpan.Zero, default));

        Assert.Equal(PortableRunnerFailure.RemoteUnavailable, error.Failure);
        AssertNothingStarted(admin, process);
        Assert.Equal(original, RecoveryBytes(root));
    }

    [Theory]
    [InlineData(PortableRunnerState.Paused, true, PortableRunnerFailure.PossibleSurvivingProcess)]
    [InlineData(PortableRunnerState.Online, true, PortableRunnerFailure.PossibleSurvivingProcess)]
    [InlineData(PortableRunnerState.Online, false, PortableRunnerFailure.InvalidState)]
    [InlineData(PortableRunnerState.Starting, false, PortableRunnerFailure.InvalidState)]
    [InlineData(PortableRunnerState.Configured, false, PortableRunnerFailure.InvalidState)]
    [InlineData(PortableRunnerState.Degraded, false, PortableRunnerFailure.InvalidState)]
    [InlineData(PortableRunnerState.RemoteRemovalPending, false, PortableRunnerFailure.InvalidState)]
    [InlineData(PortableRunnerState.Removed, false, PortableRunnerFailure.InvalidState)]
    public void OnlyPersistedPlannedPauseCanBeRestored(PortableRunnerState state, bool survivor, PortableRunnerFailure expected)
    {
        using var root = new FakeRoot();
        using var store = PortableRecoveryStore.Open(root.Path, "owner/exec", "runner-1");
        store.Write(42, state, survivor);
        var original = RecoveryBytes(root);
        var admin = new FakeAdmin();
        var process = new FakeProcess();

        var error = Assert.Throws<PortableRunnerException>(() => Restore(admin, process, store));

        Assert.Equal(expected, error.Failure);
        AssertNothingStarted(admin, process);
        Assert.Equal(0, admin.ListCalls);
        Assert.Equal(original, RecoveryBytes(root));
    }

    [Fact]
    public void PausedEvidenceWithoutNumericIdOrForAnotherRunnerCannotBeRestored()
    {
        using var root = new FakeRoot();
        using var store = PortableRecoveryStore.Open(root.Path, "owner/exec", "runner-1");
        store.Write(null, PortableRunnerState.Paused);
        var noId = Assert.Throws<PortableRunnerException>(() => Restore(new FakeAdmin(), new FakeProcess(), store));
        Assert.Equal(PortableRunnerFailure.IdentityUncertain, noId.Failure);

        store.Write(42, PortableRunnerState.Paused);
        var evidence = Assert.IsType<PortableRecoveryEvidence>(store.Read());
        var otherName = Assert.Throws<PortableRunnerException>(() => PortableRunnerLifecycle.RestorePlannedPause(
            new FakeAdmin(), new FakeProcess(), "runner-2", "grl-exec", store, evidence, new NoDelay(), isElevated: () => false));
        Assert.Equal(PortableRunnerFailure.IdentityUncertain, otherName.Failure);
        var stale = Assert.Throws<PortableRunnerException>(() => PortableRunnerLifecycle.RestorePlannedPause(
            new FakeAdmin(), new FakeProcess(), "runner-1", "grl-exec", store, evidence with { RunnerId = 43 },
            new NoDelay(), isElevated: () => false));
        Assert.Equal(PortableRunnerFailure.InvalidState, stale.Failure);
    }

    [Theory]
    [InlineData("drift")]
    [InlineData("probe-failure")]
    public async Task ReopenedResumeVersionDriftFailsBeforeStartAndStaysResumable(string failure)
    {
        using var root = new FakeRoot();
        using var store = PausedStore(root);
        var original = RecoveryBytes(root);
        var admin = new FakeAdmin { Lists = Pages([Runner(42, false)]) };
        var process = new FakeProcess();
        if (failure == "drift") process.ListenerVersions.Enqueue("2.338.0");
        else process.ListenerException = new RunnerProcessException(RunnerProcessFailure.InvalidRoot, "listener missing");
        using var lifecycle = Restore(admin, process, store);

        var error = await Assert.ThrowsAsync<PortableRunnerException>(() => lifecycle.ResumeAsync(1, TimeSpan.Zero, default));

        Assert.Equal(PortableRunnerFailure.UnsupportedVersion, error.Failure);
        Assert.Equal(0, process.StartCount);
        Assert.Equal(1, process.ListenerVerifyCount);
        Assert.Equal(PortableRunnerState.Paused, lifecycle.State);
        Assert.Equal(original, RecoveryBytes(root)); // still the same planned pause
    }

    [Fact]
    public async Task ReopenedStartFailureKeepsTruthfulResumablePause()
    {
        using var root = new FakeRoot();
        using var store = PausedStore(root);
        var original = RecoveryBytes(root);
        var admin = new FakeAdmin { Lists = Pages([Runner(42, false)], [Runner(42, false)], [Runner(42, true)]) };
        var process = new FakeProcess { FailStart = true };
        using var lifecycle = Restore(admin, process, store);

        await Assert.ThrowsAsync<RunnerProcessException>(() => lifecycle.ResumeAsync(1, TimeSpan.Zero, default));

        Assert.False(lifecycle.HasOwnedProcess);
        Assert.Equal(PortableRunnerState.Paused, lifecycle.State);
        Assert.Equal(original, RecoveryBytes(root)); // Paused, same ID, no possible survivor

        process.FailStart = false;
        await lifecycle.ResumeAsync(2, TimeSpan.Zero, default);
        Assert.Equal(PortableRunnerState.Online, lifecycle.State);
        Assert.Empty(process.Commands);
        Assert.Equal(0, admin.RegistrationTokenRequests);
    }

    [Fact]
    public async Task ReopenedOnlineTimeoutNeverWritesOnlineAndKeepsOwnedProcessTruthful()
    {
        using var root = new FakeRoot();
        using var store = PausedStore(root);
        var admin = new FakeAdmin
        {
            // A same-name runner with a different ID going online is not the exact runner.
            Lists = Pages([Runner(42, false)], [Runner(42, false)], [Runner(55, true), Runner(42, false)])
        };
        var process = new FakeProcess();
        using var lifecycle = Restore(admin, process, store);

        var error = await Assert.ThrowsAsync<PortableRunnerException>(() => lifecycle.ResumeAsync(2, TimeSpan.Zero, default));

        Assert.Equal(PortableRunnerFailure.OnlineTimeout, error.Failure);
        Assert.True(lifecycle.HasOwnedProcess);
        Assert.Equal(0, process.Owned.StopCount);
        var degraded = Assert.IsType<PortableRecoveryEvidence>(store.Read());
        Assert.Equal((PortableRunnerState.Degraded, true), (degraded.State, degraded.MayHaveUnownedProcess));
        Assert.DoesNotContain(lifecycle.Journal, x => x.State == PortableRunnerState.Online);
        var repeated = await Assert.ThrowsAsync<PortableRunnerException>(() => lifecycle.ResumeAsync(1, TimeSpan.Zero, default));
        Assert.Equal(PortableRunnerFailure.InvalidState, repeated.Failure);
        Assert.Equal(1, process.StartCount);

        await lifecycle.StopNowAsync(default);
        var paused = Assert.IsType<PortableRecoveryEvidence>(store.Read());
        Assert.Equal((PortableRunnerState.Paused, false), (paused.State, paused.MayHaveUnownedProcess));
    }

    [Theory]
    [InlineData("corrupt")]
    [InlineData("changed")]
    public async Task ReopenedResumeFailsClosedOnCorruptOrChangedRecoveryEvidence(string change)
    {
        using var root = new FakeRoot();
        using var store = PausedStore(root);
        var admin = new FakeAdmin { Lists = Pages([Runner(42, false)]) };
        var process = new FakeProcess();
        using var lifecycle = Restore(admin, process, store);
        if (change == "corrupt") File.WriteAllText(RecoveryPath(root), "{partial");
        else store.Write(42, PortableRunnerState.Degraded, mayHaveUnownedProcess: true);
        var before = RecoveryBytes(root);

        await Assert.ThrowsAnyAsync<Exception>(() => lifecycle.ResumeAsync(1, TimeSpan.Zero, default));

        AssertNothingStarted(admin, process);
        Assert.Equal(0, admin.ListCalls);
        Assert.Equal(before, RecoveryBytes(root));
    }

    [Fact]
    public async Task RestoredLifecycleCannotConfigureOrRemoveThroughCli()
    {
        using var root = new FakeRoot();
        using var store = PausedStore(root);
        var admin = new FakeAdmin();
        var process = new FakeProcess();
        using var lifecycle = Restore(admin, process, store);

        var register = await Assert.ThrowsAsync<PortableRunnerException>(() =>
            lifecycle.RegisterAndStartAsync(1, TimeSpan.Zero, default));
        var unregister = await Assert.ThrowsAsync<PortableRunnerException>(() =>
            lifecycle.UnregisterAsync(1, TimeSpan.Zero, default));

        Assert.Equal(PortableRunnerFailure.InvalidState, register.Failure);
        Assert.Equal(PortableRunnerFailure.InvalidState, unregister.Failure);
        AssertNothingStarted(admin, process);
        Assert.Equal(0, admin.ListCalls);
    }

    [Fact]
    public async Task ExistingRootProcessRefusesConfigCmdAndProbesListenerOnly()
    {
        using var root = new FakeRoot();
        var existing = PortableRunnerProcess.ForExistingRoot(root.Path, isElevated: () => false);

        using var help = new RunnerCommand("config.cmd", ["--help"]);
        var execute = await Assert.ThrowsAsync<RunnerProcessException>(() => existing.ExecuteAsync(help, default));
        var verify = await Assert.ThrowsAsync<RunnerProcessException>(() => existing.VerifyCliAsync(default));
        var listener = await Assert.ThrowsAsync<RunnerProcessException>(() => existing.VerifyListenerVersionAsync(default));

        Assert.Equal(RunnerProcessFailure.UnsafeCommand, execute.Failure);
        Assert.Equal(RunnerProcessFailure.UnsafeCommand, verify.Failure);
        Assert.Equal(RunnerProcessFailure.InvalidRoot, listener.Failure); // no bin\Runner.Listener.exe here
        Assert.Throws<RunnerProcessException>(() => PortableRunnerProcess.ForExistingRoot("relative\\root"));
    }

    [Fact]
    public void RunOnlyCliVerifiesExactVersionAndBuildsOnlyExistingRun()
    {
        var cli = RunnerCliContract.VerifyRunOnly("v2.337.0\n");
        Assert.Equal(RunnerPin.ReviewedVersion, cli.Capabilities.Version);
        using var run = cli.BuildRun();
        Assert.Equal("run.cmd", run.EntryPoint);
        Assert.Empty(run.Arguments);
        Assert.Throws<RunnerCliException>(() => cli.BuildRemove("TOKEN"));
        Assert.Throws<RunnerCliException>(() => cli.BuildConfigure(new RunnerConfiguration(
            new Uri("https://github.com/owner/exec"), "TOKEN", "runner-1", "grl-exec", "_work")));
        var drift = Assert.Throws<RunnerCliException>(() => RunnerCliContract.VerifyRunOnly("2.338.0"));
        Assert.Equal(RunnerCliFailure.UnsupportedVersion, drift.Failure);
    }

    [Fact]
    public async Task ReopenedWizardOffersResumeAfterRepoConfirmationAndReturnsSameIdOnline()
    {
        using var root = new FakeRoot();
        var runnerRoot = Directory.CreateDirectory(System.IO.Path.Combine(root.Path, "runner")).FullName;
        using (var earlier = PortableRecoveryStore.Open(runnerRoot, "owner/exec", "runner-1"))
            earlier.Write(42, PortableRunnerState.Paused); // the earlier app session's planned Stop Now
        var admin = new FakeAdmin { Lists = Pages([Runner(42, false)], [Runner(42, true)]) };
        var process = new FakeProcess();
        using var adapter = ReopenAdapter(root, admin, process);
        var session = NewReopenSession(adapter, root.Path);

        await session.ExecuteUserCommandAsync(WizardEvent.Continue);

        Assert.Equal(WizardState.InstallingDownloading, session.State);
        Assert.Equal("RESUME_EXISTING_AVAILABLE", session.ActiveError?.Code);
        Assert.True(session.CanResumeExisting);
        var action = session.Actions[0];
        Assert.Equal("Resume existing runner registration", action.AutomationName);
        Assert.True(action.IsPrimary);
        Assert.Contains("never registers again", session.RunnerPresenceText);
        Assert.Equal(0, admin.ListCalls);

        await session.ResumeExistingAsync();

        Assert.Equal(WizardState.RunnerIdle, session.State);
        Assert.Null(session.ActiveError);
        Assert.True(session.HasOwnedRunnerProcess);
        Assert.False(session.CanResumeExisting);
        Assert.Equal(1, process.StartCount);
        Assert.Empty(process.Commands);
        Assert.Equal(0, process.VerifyCount);
        Assert.Equal(0, admin.RegistrationTokenRequests);
        Assert.Equal(0, admin.RemoveTokenRequests);

        await session.ResumeExistingAsync(); // repeated click
        Assert.Equal("INVALID_TRANSITION", session.ActiveError?.Code);
        Assert.Equal(1, process.StartCount);
    }

    [Fact]
    public async Task ReopenedCurrentOnlineSurvivorEvidenceIsNeverResumedOrAdopted()
    {
        using var root = new FakeRoot();
        var runnerRoot = Directory.CreateDirectory(System.IO.Path.Combine(root.Path, "runner")).FullName;
        using (var earlier = PortableRecoveryStore.Open(runnerRoot, "owner/exec", "runner-1"))
            earlier.Write(42, PortableRunnerState.Online, mayHaveUnownedProcess: true);
        var admin = new FakeAdmin();
        var process = new FakeProcess();
        using var adapter = ReopenAdapter(root, admin, process);
        var session = NewReopenSession(adapter, root.Path);

        await session.ExecuteUserCommandAsync(WizardEvent.Continue);

        Assert.Equal("RECOVERY_REQUIRED", session.ActiveError?.Code);
        Assert.False(session.CanResumeExisting);
        Assert.DoesNotContain(session.Actions, x => x.AutomationName == "Resume existing runner registration");
        Assert.Equal("RESUME_STATE_INELIGIBLE", (await adapter.ResumeExistingAsync()).ErrorCode);
        AssertNothingStarted(admin, process);
        Assert.Equal(0, admin.ListCalls);
    }

    [Theory]
    [InlineData(true, false, "RESUME_RUNNER_ACTIVE")]
    [InlineData(false, true, "RESUME_RUNNER_ACTIVE")]
    public async Task ReopenedWizardShowsTruthfulPendingStateForActiveRemoteRunner(bool online, bool busy, string code)
    {
        using var root = new FakeRoot();
        var runnerRoot = Directory.CreateDirectory(System.IO.Path.Combine(root.Path, "runner")).FullName;
        using (var earlier = PortableRecoveryStore.Open(runnerRoot, "owner/exec", "runner-1"))
            earlier.Write(42, PortableRunnerState.Paused);
        var original = File.ReadAllBytes(System.IO.Path.Combine(runnerRoot, ".grl-recovery.json"));
        var admin = new FakeAdmin { Lists = Pages([Runner(42, online, busy)], [Runner(42, false)], [Runner(42, true)]) };
        var process = new FakeProcess();
        using var adapter = ReopenAdapter(root, admin, process);
        var session = NewReopenSession(adapter, root.Path);
        await session.ExecuteUserCommandAsync(WizardEvent.Continue);

        await session.ResumeExistingAsync();

        Assert.Equal(code, session.ActiveError?.Code);
        Assert.Contains("not adopted", session.ErrorWhatHappened);
        Assert.Equal(WizardState.InstallingDownloading, session.State);
        Assert.False(session.HasOwnedRunnerProcess);
        Assert.Equal(0, process.StartCount);
        Assert.Equal(original, File.ReadAllBytes(System.IO.Path.Combine(runnerRoot, ".grl-recovery.json")));

        Assert.True(session.CanResumeExisting); // once GitHub shows it offline, an explicit retry may proceed
        await session.ResumeExistingAsync();
        Assert.Equal(WizardState.RunnerIdle, session.State);
        Assert.Equal(1, process.StartCount);
    }

    [Fact]
    public async Task ReopenedWizardRefusesWhileAnotherInstanceHoldsTheRecoveryLease()
    {
        using var root = new FakeRoot();
        var runnerRoot = Directory.CreateDirectory(System.IO.Path.Combine(root.Path, "runner")).FullName;
        using var otherInstance = PortableRecoveryStore.Open(runnerRoot, "owner/exec", "runner-1");
        otherInstance.Write(42, PortableRunnerState.Paused);
        var admin = new FakeAdmin();
        var process = new FakeProcess();
        using var adapter = ReopenAdapter(root, admin, process);
        var session = NewReopenSession(adapter, root.Path);

        await session.ExecuteUserCommandAsync(WizardEvent.Continue);

        Assert.Equal("RECOVERY_STATE_UNAVAILABLE", session.ActiveError?.Code);
        Assert.False(session.CanResumeExisting);
        Assert.Equal("RESUME_STATE_INELIGIBLE", (await adapter.ResumeExistingAsync()).ErrorCode);
        AssertNothingStarted(admin, process);
        Assert.Equal(0, admin.ListCalls);
    }

    [Fact]
    public async Task ReopenedWizardVersionDriftAndStartFailureKeepResumeAvailable()
    {
        using var root = new FakeRoot();
        var runnerRoot = Directory.CreateDirectory(System.IO.Path.Combine(root.Path, "runner")).FullName;
        using (var earlier = PortableRecoveryStore.Open(runnerRoot, "owner/exec", "runner-1"))
            earlier.Write(42, PortableRunnerState.Paused);
        var admin = new FakeAdmin { Lists = Pages([Runner(42, false)], [Runner(42, false)]) };
        var process = new FakeProcess { FailStart = true };
        process.ListenerVersions.Enqueue("2.338.0");
        using var adapter = ReopenAdapter(root, admin, process);
        var session = NewReopenSession(adapter, root.Path);
        await session.ExecuteUserCommandAsync(WizardEvent.Continue);

        await session.ResumeExistingAsync();
        Assert.Equal("RUNNER_VERSION_UNSUPPORTED", session.ActiveError?.Code);
        Assert.Equal(0, process.StartCount);
        Assert.True(session.CanResumeExisting);

        await session.ResumeExistingAsync();
        Assert.Equal("RESUME_START_FAILED", session.ActiveError?.Code);
        Assert.Equal(WizardState.InstallingDownloading, session.State);
        Assert.False(session.HasOwnedRunnerProcess);
        Assert.True(session.CanResumeExisting);
        Assert.Empty(process.Commands);
        Assert.Equal(0, admin.RegistrationTokenRequests);
    }

    [Fact]
    public async Task ReopenedWizardOnlineTimeoutKeepsOwnedProcessForExplicitStopNow()
    {
        using var root = new FakeRoot();
        var runnerRoot = Directory.CreateDirectory(System.IO.Path.Combine(root.Path, "runner")).FullName;
        using (var earlier = PortableRecoveryStore.Open(runnerRoot, "owner/exec", "runner-1"))
            earlier.Write(42, PortableRunnerState.Paused);
        var admin = new FakeAdmin { Lists = new(Enumerable.Repeat<IReadOnlyList<RepositoryRunner>>([Runner(42, false)], 21)) };
        var process = new FakeProcess();
        using var adapter = ReopenAdapter(root, admin, process);
        var session = NewReopenSession(adapter, root.Path);
        await session.ExecuteUserCommandAsync(WizardEvent.Continue);

        await session.ResumeExistingAsync();

        Assert.Equal("RESUME_ONLINE_TIMEOUT", session.ActiveError?.Code);
        Assert.Equal(WizardState.RunnerDegraded, session.State);
        Assert.True(session.HasOwnedRunnerProcess);
        Assert.False(session.CanResumeExisting);
        Assert.True(session.StopNowCommand.CanExecute(null));
        Assert.Equal(1, process.StartCount);

        await session.StopNowAsync();
        Assert.False(session.HasOwnedRunnerProcess);
        Assert.True(session.CanResumeExisting); // back to a planned pause
    }

    [Fact]
    public async Task ResumedThenStoppedRunnerRemovesOnlyThroughExactIdRecovery()
    {
        using var root = new FakeRoot();
        var runnerRoot = Directory.CreateDirectory(System.IO.Path.Combine(root.Path, "runner")).FullName;
        using (var earlier = PortableRecoveryStore.Open(runnerRoot, "owner/exec", "runner-1"))
            earlier.Write(42, PortableRunnerState.Paused);
        var admin = new FakeAdmin { Lists = Pages([Runner(42, false)], [Runner(42, true)], [Runner(42, false)], []) };
        var process = new FakeProcess();
        using var adapter = ReopenAdapter(root, admin, process);
        var session = NewReopenSession(adapter, root.Path);
        await session.ExecuteUserCommandAsync(WizardEvent.Continue);
        await session.ResumeExistingAsync();
        await session.StopNowAsync();
        Assert.Equal(WizardState.RunnerPaused, session.State);

        var result = await adapter.RemoveAsync();

        Assert.Equal(WizardEvent.RemoteRemoved, result);
        Assert.Equal(1, admin.DeleteStaleRequests);
        Assert.Equal(0, admin.RemoveTokenRequests); // no config.cmd remove
        Assert.Empty(process.Commands);
        Assert.False(adapter.HasPendingRecovery);
    }

    // R-GRL015-1: the reviewer's real failure shape — an ordinary root whose bin is a Windows
    // directory junction to a scratch directory holding a harmless Runner.Listener.exe that
    // reports 2.337.0 if executed. The full path must be refused before anything runs.
    [Fact]
    public async Task ReopenedJunctionedBinIsRefusedBeforeListenerRunsOrRunCmdStarts()
    {
        if (!OperatingSystem.IsWindows()) return;
        using var root = new FakeRoot();
        using var scratch = new ScratchDirectory();
        var redirected = WriteHarmlessListener(scratch.Path);
        AssertListenerWouldReportReviewedVersion(redirected);
        using var junction = CreateJunction(System.IO.Path.Combine(root.Path, "bin"), scratch.Path);
        WriteRunLayout(root.Path);
        using var store = PausedStore(root);
        var original = RecoveryBytes(root);
        var admin = new FakeAdmin { Lists = Pages([Runner(42, false)], [Runner(42, true)]) };
        var existing = PortableRunnerProcess.ForExistingRoot(root.Path, isElevated: () => false);

        var probe = await Assert.ThrowsAsync<RunnerProcessException>(() => existing.VerifyListenerVersionAsync(default));
        Assert.Equal(RunnerProcessFailure.InvalidRoot, probe.Failure);

        using var lifecycle = PortableRunnerLifecycle.RestorePlannedPause(admin, existing, "runner-1", "grl-exec",
            store, Assert.IsType<PortableRecoveryEvidence>(store.Read()), new NoDelay(), isElevated: () => false);
        var resume = await Assert.ThrowsAsync<PortableRunnerException>(() => lifecycle.ResumeAsync(1, TimeSpan.Zero, default));

        Assert.Equal(PortableRunnerFailure.UnsupportedVersion, resume.Failure); // listener unverifiable: no start
        Assert.False(File.Exists(ListenerMarker(scratch.Path)));  // redirected listener never executed
        Assert.False(File.Exists(RunMarker(root.Path)));          // run.cmd never started
        Assert.False(lifecycle.HasOwnedProcess);
        Assert.Equal(PortableRunnerState.Paused, lifecycle.State);
        Assert.Equal(original, RecoveryBytes(root));              // still the same resumable planned pause
        Assert.Equal(0, admin.RegistrationTokenRequests);
    }

    [Fact]
    public async Task OrdinaryExistingRootStillVerifiesListenerAndStartsRunCmd()
    {
        if (!OperatingSystem.IsWindows()) return;
        using var root = new FakeRoot();
        var bin = Directory.CreateDirectory(System.IO.Path.Combine(root.Path, "bin")).FullName;
        WriteHarmlessListener(bin);
        WriteRunLayout(root.Path);
        var existing = PortableRunnerProcess.ForExistingRoot(root.Path, isElevated: () => false);

        var cli = await existing.VerifyListenerVersionAsync(default);
        Assert.Equal(RunnerPin.ReviewedVersion, cli.Capabilities.Version);
        Assert.True(File.Exists(ListenerMarker(bin)));

        using var run = cli.BuildRun();
        using var started = await existing.StartAsync(run, cli.Capabilities.Version, default); // boundary passed
        // With NoDefaultCurrentDirectoryInExePath set (deferred NF-2 shells), cmd cannot resolve run.cmd
        // from its working directory, so the batch's own marker is asserted only in an ordinary environment.
        if (Environment.GetEnvironmentVariable("NoDefaultCurrentDirectoryInExePath") is null)
            await WaitForFileAsync(RunMarker(root.Path));
        await started.StopAsync(default); // ensure the harmless batch has exited before cleanup
    }

    [Fact]
    public async Task BinRedirectedAfterListenerVerificationIsRefusedAtRunStart()
    {
        if (!OperatingSystem.IsWindows()) return;
        using var root = new FakeRoot();
        using var scratch = new ScratchDirectory();
        var bin = Directory.CreateDirectory(System.IO.Path.Combine(root.Path, "bin")).FullName;
        WriteHarmlessListener(bin);
        WriteRunLayout(root.Path);
        var existing = PortableRunnerProcess.ForExistingRoot(root.Path, isElevated: () => false);
        var cli = await existing.VerifyListenerVersionAsync(default);

        // After verification, bin is swapped for a junction to another harmless listener.
        Directory.Move(bin, System.IO.Path.Combine(root.Path, "bin-verified"));
        WriteHarmlessListener(scratch.Path);
        using var junction = CreateJunction(bin, scratch.Path);

        using var run = cli.BuildRun();
        var start = await Assert.ThrowsAsync<RunnerProcessException>(() =>
            existing.StartAsync(run, cli.Capabilities.Version, default));

        Assert.Equal(RunnerProcessFailure.InvalidRoot, start.Failure);
        await Task.Delay(500);
        Assert.False(File.Exists(RunMarker(root.Path)));
        Assert.False(File.Exists(ListenerMarker(scratch.Path)));
    }

    private static readonly Lazy<string> HarmlessListenerBuild = new(BuildHarmlessListener);

    // A tiny .NET Framework console program compiled by Windows PowerShell: it writes a marker next
    // to itself and prints the reviewed version. It is harmless and never contacts anything.
    private static string BuildHarmlessListener()
    {
        var directory = Directory.CreateDirectory(System.IO.Path.Combine(System.IO.Path.GetTempPath(),
            "grl-harmless-listener-" + Guid.NewGuid().ToString("N"))).FullName;
        var source = System.IO.Path.Combine(directory, "Listener.cs");
        var exe = System.IO.Path.Combine(directory, "Runner.Listener.exe");
        File.WriteAllText(source, """
            using System;
            using System.IO;
            public static class HarmlessListener
            {
                public static int Main(string[] args)
                {
                    File.WriteAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "listener-ran.marker"), "ran");
                    Console.WriteLine("2.337.0");
                    return 0;
                }
            }
            """);
        var build = new ProcessStartInfo("powershell.exe") { UseShellExecute = false, CreateNoWindow = true };
        foreach (var argument in new[] { "-NoLogo", "-NoProfile", "-NonInteractive", "-Command",
            $"Add-Type -Path '{source}' -OutputAssembly '{exe}' -OutputType ConsoleApplication" })
            build.ArgumentList.Add(argument);
        using var process = Process.Start(build)!;
        Assert.True(process.WaitForExit(120_000));
        Assert.True(process.ExitCode == 0 && File.Exists(exe), "harmless listener fixture did not compile");
        return exe;
    }

    private static string WriteHarmlessListener(string directory)
    {
        var target = System.IO.Path.Combine(directory, "Runner.Listener.exe");
        File.Copy(HarmlessListenerBuild.Value, target);
        return target;
    }

    private static void AssertListenerWouldReportReviewedVersion(string listener)
    {
        var info = new ProcessStartInfo(listener, "--version") { UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true };
        using var process = Process.Start(info)!;
        var output = process.StandardOutput.ReadToEnd();
        Assert.True(process.WaitForExit(30_000));
        Assert.Equal(RunnerPin.ReviewedVersion, output.Trim());
        var marker = ListenerMarker(System.IO.Path.GetDirectoryName(listener)!);
        Assert.True(File.Exists(marker));
        File.Delete(marker); // the positive control must not satisfy the later absence check
    }

    private static void WriteRunLayout(string root)
    {
        File.WriteAllText(System.IO.Path.Combine(root, "run.cmd"),
            "@echo off\r\necho started> \"%~dp0run-started.marker\"\r\n");
        File.WriteAllText(System.IO.Path.Combine(root, "run-helper.cmd.template"), "@echo off\r\n");
    }

    // A real Windows directory junction (mklink /J needs no elevation). Disposing removes only the link.
    private static Junction CreateJunction(string link, string target)
    {
        var info = new ProcessStartInfo(System.IO.Path.Combine(Environment.SystemDirectory, "cmd.exe"))
            { UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true };
        foreach (var argument in new[] { "/d", "/c", "mklink", "/J", link, target }) info.ArgumentList.Add(argument);
        using var process = Process.Start(info)!;
        process.StandardOutput.ReadToEnd();
        Assert.True(process.WaitForExit(30_000));
        Assert.True((File.GetAttributes(link) & FileAttributes.ReparsePoint) != 0, "junction fixture was not created");
        return new Junction(link);
    }

    private sealed class Junction(string link) : IDisposable
    {
        public void Dispose() => Directory.Delete(link);
    }

    private static async Task WaitForFileAsync(string path)
    {
        for (var attempt = 0; attempt < 100 && !File.Exists(path); attempt++) await Task.Delay(100);
        Assert.True(File.Exists(path), "ordinary run.cmd did not start");
    }

    private static string ListenerMarker(string directory) => System.IO.Path.Combine(directory, "listener-ran.marker");
    private static string RunMarker(string root) => System.IO.Path.Combine(root, "run-started.marker");

    private sealed class ScratchDirectory : IDisposable
    {
        public string Path { get; } = Directory.CreateDirectory(System.IO.Path.Combine(System.IO.Path.GetTempPath(),
            "grl-junction-target-" + Guid.NewGuid().ToString("N"))).FullName;
        public void Dispose() => Directory.Delete(Path, recursive: true);
    }

    private static PortableRecoveryStore PausedStore(FakeRoot root)
    {
        var store = PortableRecoveryStore.Open(root.Path, "owner/exec", "runner-1");
        store.Write(42, PortableRunnerState.Paused);
        return store;
    }

    private static PortableRunnerLifecycle Restore(FakeAdmin admin, FakeProcess process, PortableRecoveryStore store) =>
        PortableRunnerLifecycle.RestorePlannedPause(admin, process, "runner-1", "grl-exec", store,
            Assert.IsType<PortableRecoveryEvidence>(store.Read()), new NoDelay(), isElevated: () => false);

    private static LiveWizardAdapters ReopenAdapter(FakeRoot root, FakeAdmin admin, FakeProcess process) =>
        new(new LiveRuntimeOptions("clientidxx", "owner/exec", "runner-1", root.Path),
            admin, recoveryStore: null, reopened: null, new NoDelay(), process);

    // A relaunched app after normal sign-in and exact repository confirmation, at the local folder step.
    private static WizardSession NewReopenSession(LiveWizardAdapters adapter, string root) =>
        new(new ScenarioSelection(FakeScenario.HappyPath, string.Empty), new TestClock(),
            new PreviewDelay(TimeSpan.Zero),
            new WizardAdapters(adapter, adapter, adapter, adapter, adapter, adapter, adapter),
            WizardState.LocationLocal, live: true, liveLocation: root);

    private static void AssertNothingStarted(FakeAdmin admin, FakeProcess process)
    {
        Assert.Equal(0, process.StartCount);
        Assert.Empty(process.Commands);
        Assert.Equal(0, process.VerifyCount);
        Assert.Equal(0, admin.RegistrationTokenRequests);
        Assert.Equal(0, admin.RemoveTokenRequests);
        Assert.Equal(0, admin.DeleteStaleRequests);
    }

    private static string RecoveryPath(FakeRoot root) => System.IO.Path.Combine(root.Path, ".grl-recovery.json");
    private static byte[] RecoveryBytes(FakeRoot root) => File.ReadAllBytes(RecoveryPath(root));
}
