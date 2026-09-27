using System.Diagnostics;
using System.Text.Json;
using Grl.Integration;

namespace Grl.Integration.Tests;

public sealed class JobGateTests
{
    private const string Sha = "2c8da8834e785ba012001b9427bd68380401bec7";
    private static readonly JobGateIdentity Identity = new("owner/exec", 42, "fixture", [Sha]);

    [Fact]
    public async Task DisabledCapabilityAndMissingHooksNeverFallBackToUngatedStart()
    {
        using var fixture = new Fixture();
        Assert.Throws<InvalidOperationException>(() => new JobGateSession(JobGateCapability.Disabled,
            fixture.Gate, fixture.Runner, Fixture.Node, Identity));
        var inner = new FakeAdapter();
        var adapter = new GatedRunnerProcessAdapter(inner, fixture.Session);
        await Assert.ThrowsAsync<InvalidOperationException>(() => adapter.StartAsync(new("run.cmd", []), "2.337.0", default));
        Assert.Equal(0, inner.Starts);
        await fixture.Session.CheckpointAndInstallSyntheticAsync();
        await adapter.StartAsync(new("run.cmd", []), "2.337.0", default);
        Assert.Equal(1, inner.Starts);
        Assert.Equal("INACTIVE", fixture.State.GetProperty("mode").GetString());
        File.AppendAllText(Path.Combine(fixture.Gate, "job-started.js"), "\n// drift\n");
        await Assert.ThrowsAsync<InvalidOperationException>(() => adapter.StartAsync(new("run.cmd", []), "2.337.0", default));
        Assert.Equal(1, inner.Starts);
    }

    [Fact]
    public async Task ActivationRequiresSoleExactRegistrationAndResumeChangesEpoch()
    {
        using var f = new Fixture(); await f.Install();
        var epoch = f.State.GetProperty("gateEpoch").GetString();
        foreach (GateRegistration[] registrations in new GateRegistration[][] {
            [], [new(43, "fixture")], [new(42, "other")], [new(42, "fixture"), new(43, "second")] })
            await Assert.ThrowsAsync<InvalidOperationException>(() => f.Session.ActivateLocalAsync(registrations, new Observation(true)));
        Assert.Equal("INACTIVE", f.State.GetProperty("mode").GetString());
        await Assert.ThrowsAsync<InvalidOperationException>(() => f.Session.ActivateLocalAsync([new(42, "fixture")], new Observation(false)));
        await f.Session.ActivateLocalAsync([new(42, "fixture")], new Observation(true));
        Assert.NotEqual(epoch, f.State.GetProperty("gateEpoch").GetString());
        var activeEpoch = f.State.GetProperty("gateEpoch").GetString();
        await f.Session.BeginReleaseAsync();
        Assert.Equal("DRAINING", f.State.GetProperty("mode").GetString());
        var owned = new FakeOwned();
        await f.Session.PauseAsync(owned, new Observation(true));
        Assert.True(owned.HasExited);
        await f.Session.ActivateLocalAsync([new(42, "fixture")], new Observation(true));
        Assert.NotEqual(activeEpoch, f.State.GetProperty("gateEpoch").GetString());
    }

    [Fact]
    public async Task CompletionChildMarkerAndRealSyntheticWorkerLifetimeGateStopOnWindows()
    {
        Assert.True(OperatingSystem.IsWindows());
        using var f = new Fixture(); await f.Install();
        await f.Session.ActivateLocalAsync([new(42, "fixture")], new Observation(true));
        // Harmless copied Node executable provides a real Windows Worker-shaped process, never a runner package.
        var workerExe = Path.Combine(f.Runner, "Runner.Worker.exe"); File.Copy(Fixture.Node, workerExe);
        var release = Path.Combine(f.Base, "exit-worker");
        var ready = Path.Combine(f.Base, "completed");
        var script = Path.Combine(f.Base, "worker.cjs");
        Func<string, string> q = value => JsonSerializer.Serialize(value);
        File.WriteAllText(script, "const fs=require('node:fs'),cp=require('node:child_process');" +
            "for(const name of ['job-started.js','job-completed.js']){" +
            $"const r=cp.spawnSync(process.execPath,[{q(f.Gate)}+'/' + name],{{env:process.env}});if(r.status!==0)process.exit(1);}}" +
            $"fs.writeFileSync({q(ready)},'done');const timer=setInterval(()=>{{if(fs.existsSync({q(release)})){{clearInterval(timer);process.exit(0);}}}},20);");
        var rootScript = Path.Combine(f.Base, "root.cjs");
        File.WriteAllText(rootScript, $"require('node:child_process').spawn({q(workerExe)},[{q(script)}],{{env:process.env,stdio:'ignore'}});setInterval(()=>{{}},1000);");
        using var parent = StartNode(rootScript, f.Environment());
        try
        {
            await WaitFor(ready);
            Assert.Single(Directory.GetFiles(Path.Combine(f.Gate, "jobs"), "*.done"));
            var owner = new ProcessLifetime(parent.Id, 0, parent.StartTime.ToUniversalTime().Ticks, Fixture.Node);
            var observation = new OwnedWorkerObservation(owner, f.Runner,
                () => WindowsProcessInventory.Capture(parent.Id), WindowsProcessInventory.HasExited);
            Assert.False(observation.ConfirmNoOwnedWorker());
            var owned = new RealOwned(parent);
            await Assert.ThrowsAsync<InvalidOperationException>(() => f.Session.PauseAsync(owned, observation));
            Assert.False(parent.HasExited); Assert.Equal(0, owned.Stops);
            File.WriteAllText(release, "go");
            for (var i = 0; i < 200 && !observation.ConfirmNoOwnedWorker(); i++) await Task.Delay(20);
            var finalInventory = WindowsProcessInventory.Capture(parent.Id);
            Assert.True(observation.ConfirmNoOwnedWorker(), $"Synthetic inventory complete={finalInventory.Complete}; images={string.Join(',', finalInventory.Processes.Select(p => Path.GetFileName(p.Image)))}");
            await f.Session.PauseAsync(owned, observation);
            Assert.True(parent.HasExited); Assert.Equal(1, owned.Stops);
        }
        finally { if (!parent.HasExited) { parent.Kill(true); await parent.WaitForExitAsync(); } }
    }

    [Fact]
    public void PartialPermissionUnknownOwnershipAndPidReuseNeverBecomeAbsence()
    {
        var owner = new ProcessLifetime(1, 0, 100, "root.exe");
        var worker = new ProcessLifetime(2, 1, 200, @"C:\fixture\Runner.Worker.exe");
        ProcessInventory view = new(true, [owner, worker]);
        bool? workerExited = false;
        var observer = new OwnedWorkerObservation(owner, @"C:\fixture", () => view,
            p => p.Id == 1 ? false : workerExited);
        Assert.False(observer.ConfirmNoOwnedWorker());
        view = new(true, [owner, worker with { Created = 300, ParentId = 999 }]);
        workerExited = null; Assert.False(observer.ConfirmNoOwnedWorker());
        view = new(false, [owner]); workerExited = true; Assert.False(observer.ConfirmNoOwnedWorker());
        view = new(true, [owner]); Assert.True(observer.ConfirmNoOwnedWorker());
        view = new(true, [owner with { Created = 999 }]); Assert.False(observer.ConfirmNoOwnedWorker());
        var denied = new OwnedWorkerObservation(owner, @"C:\fixture", () => throw new UnauthorizedAccessException(), _ => false);
        Assert.False(denied.ConfirmNoOwnedWorker());
    }

    [Fact]
    public async Task FilesystemSharingDenialRefusesActualHookAndLeavesNoPass()
    {
        using var f = new Fixture(); await f.Install();
        await f.Session.ActivateLocalAsync([new(42, "fixture")], new Observation(true));
        using (var blocked = new FileStream(Path.Combine(f.Gate, "state.json"), FileMode.Open, FileAccess.Read, FileShare.None))
        {
            using var hook = StartNode(Path.Combine(f.Gate, "job-started.js"), f.Environment());
            await hook.WaitForExitAsync(); Assert.Equal(1, hook.ExitCode);
        }
        Assert.Empty(Directory.GetFiles(Path.Combine(f.Gate, "jobs"), "*.pass"));
        Assert.Single(Directory.GetFiles(Path.Combine(f.Gate, "jobs"), "*.refuse"));
    }

    [Fact]
    public async Task EmergencyStopAndFailedAutomaticStopPreserveRecoveryRatherThanSuccessfulPause()
    {
        using var f = new Fixture(); await f.Install();
        var owned = new FakeOwned();
        await Assert.ThrowsAsync<InvalidOperationException>(() => f.Session.StopNowAsync(owned, false));
        Assert.Equal(0, owned.Stops);
        await f.Session.StopNowAsync(owned, true);
        Assert.True(f.State.GetProperty("recoveryRequired").GetBoolean());
        await Assert.ThrowsAsync<InvalidOperationException>(() => f.Session.ActivateLocalAsync([new(42, "fixture")], new Observation(true)));
        using var other = new Fixture(); await other.Install();
        await Assert.ThrowsAsync<IOException>(() => other.Session.PauseAsync(new FakeOwned { Fail = true }, new Observation(true)));
        Assert.True(other.State.GetProperty("recoveryRequired").GetBoolean());
    }

    [Fact]
    public async Task InterruptedActiveStartupIsRecoveryRequiredAndExactSyntheticEnvRestores()
    {
        using var f = new Fixture();
        var original = "# fixture comment\r\nUNRELATED=fixture\r\n";
        File.WriteAllText(Path.Combine(f.Runner, ".env"), original);
        await f.Install(); await f.Session.ActivateLocalAsync([new(42, "fixture")], new Observation(true));
        await f.Session.StartupInactiveAsync();
        Assert.Equal("INACTIVE", f.State.GetProperty("mode").GetString());
        Assert.True(f.State.GetProperty("recoveryRequired").GetBoolean());
        await f.Session.RestoreSyntheticConfigurationAsync();
        Assert.Equal(original, File.ReadAllText(Path.Combine(f.Runner, ".env")));
    }

    [Fact]
    public async Task ProofIsRecheckedImmediatelyBeforeStopAndPartialEvidenceBlocks()
    {
        using var f = new Fixture(); await f.Install();
        var owned = new FakeOwned(); var count = 0;
        await Assert.ThrowsAsync<InvalidOperationException>(() => f.Session.PauseAsync(owned, new Observation(() => ++count == 1)));
        Assert.Equal(0, owned.Stops);
        File.WriteAllText(Path.Combine(f.Gate, "jobs", "partial.tmp"), "{");
        await Assert.ThrowsAsync<InvalidOperationException>(() => f.Session.PauseAsync(owned, new Observation(true)));
        Assert.Equal(0, owned.Stops);
    }

    [Fact]
    public async Task ExistingAdapterEmergencyStopRecordsGateRecoveryAndPlannedStopDoesNot()
    {
        using var f = new Fixture(); await f.Session.CheckpointAndInstallSyntheticAsync();
        var adapter = new GatedRunnerProcessAdapter(new FakeAdapter(), f.Session);
        using var first = await adapter.StartAsync(new("run.cmd", []), "2.337.0", default);
        await first.StopAsync(default);
        Assert.True(f.State.GetProperty("recoveryRequired").GetBoolean());
        using var other = new Fixture(); await other.Session.CheckpointAndInstallSyntheticAsync();
        var secondAdapter = new GatedRunnerProcessAdapter(new FakeAdapter(), other.Session);
        using var second = await secondAdapter.StartAsync(new("run.cmd", []), "2.337.0", default);
        await other.Session.PauseAsync(second, new Observation(true));
        Assert.False(other.State.GetProperty("recoveryRequired").GetBoolean());
        using var direct = new Fixture(); await direct.Session.CheckpointAndInstallSyntheticAsync();
        var directAdapter = new GatedRunnerProcessAdapter(new FakeAdapter(), direct.Session);
        using var third = await directAdapter.StartAsync(new("run.cmd", []), "2.337.0", default);
        await direct.Session.StopNowAsync(third, true);
        Assert.True(third.HasExited);
    }

    private sealed class Observation(Func<bool> observe) : IWorkerLifetimeObservation
    {
        public Observation(bool value) : this(() => value) { }
        public bool ConfirmNoOwnedWorker() => observe();
    }
    private sealed class FakeOwned : IOwnedRunnerProcess
    {
        public bool Fail { get; init; }
        public int Stops { get; private set; }
        public bool HasExited { get; private set; }
        public Task StopAsync(CancellationToken ct) { Stops++; if (Fail) throw new IOException("synthetic stop failure"); HasExited = true; return Task.CompletedTask; }
        public void Dispose() { }
    }
    private sealed class RealOwned(Process process) : IOwnedRunnerProcess
    {
        public int Stops { get; private set; }
        public bool HasExited => process.HasExited;
        public async Task StopAsync(CancellationToken ct) { Stops++; process.Kill(true); await process.WaitForExitAsync(ct); }
        public void Dispose() { }
    }
    private sealed class FakeAdapter : IRunnerProcessAdapter
    {
        public int Starts { get; private set; }
        public Task ExecuteAsync(RunnerCommand command, CancellationToken ct) => throw new NotSupportedException();
        public Task<IRunnerCli> VerifyCliAsync(CancellationToken ct) => throw new NotSupportedException();
        public Task<IRunnerCli> VerifyListenerVersionAsync(CancellationToken ct) => throw new NotSupportedException();
        public Task<IOwnedRunnerProcess> StartAsync(RunnerCommand command, string verifiedVersion, CancellationToken ct)
        { Starts++; return Task.FromResult<IOwnedRunnerProcess>(new FakeOwned()); }
    }
    private static Process StartNode(string script, Dictionary<string, string> environment)
    {
        var start = new ProcessStartInfo(Fixture.Node) { UseShellExecute = false, CreateNoWindow = true,
            RedirectStandardOutput = true, RedirectStandardError = true };
        start.ArgumentList.Add(script); foreach (var pair in environment) start.Environment[pair.Key] = pair.Value;
        var process = Process.Start(start)!;
        _ = process.StandardOutput.BaseStream.CopyToAsync(Stream.Null);
        _ = process.StandardError.BaseStream.CopyToAsync(Stream.Null);
        return process;
    }
    private static async Task WaitFor(string file)
    {
        for (var i = 0; i < 500; i++) { if (File.Exists(file)) return; await Task.Delay(20); }
        throw new TimeoutException("Synthetic process barrier not reached.");
    }
    private sealed class Fixture : IDisposable
    {
        public static string Node => System.Environment.GetEnvironmentVariable("GRL_TEST_NODE") ??
            Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.ProgramFiles), "nodejs", "node.exe");
        public string Base { get; } = Path.Combine(Path.GetTempPath(), "grl014-net-" + Guid.NewGuid());
        public string Gate => Path.Combine(Base, "gate");
        public string Runner => Path.Combine(Base, "runner");
        public JobGateSession Session { get; }
        public Fixture()
        {
            Directory.CreateDirectory(Gate); Directory.CreateDirectory(Runner);
            Session = new(JobGateCapability.Synthetic, Gate, Runner, Node, Identity);
        }
        public JsonElement State => JsonDocument.Parse(File.ReadAllText(Path.Combine(Gate, "state.json"))).RootElement.Clone();
        public async Task Install() { await Session.CheckpointAndInstallSyntheticAsync(); await Session.StartupInactiveAsync(); }
        public Dictionary<string, string> Environment() => new() { ["GITHUB_REPOSITORY"] = Identity.Repo,
            ["RUNNER_NAME"] = Identity.RunnerName, ["GRL_GATE_RUNNER_ID"] = "42", ["GITHUB_SHA"] = Sha,
            ["GITHUB_WORKFLOW_SHA"] = Sha, ["GITHUB_RUN_ID"] = "100", ["GITHUB_RUN_ATTEMPT"] = "1",
            ["GITHUB_JOB"] = "admit", ["RUNNER_TRACKING_ID"] = "github_" + Guid.NewGuid() };
        public void Dispose()
        {
            // Checked absolute fixture path, single-shell/runtime cleanup only; never a runner root.
            var absolute = Path.GetFullPath(Base);
            if (!absolute.StartsWith(Path.GetFullPath(Path.GetTempPath()) + "grl014-net-", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Unsafe fixture cleanup.");
            Directory.Delete(absolute, true);
        }
    }
}
