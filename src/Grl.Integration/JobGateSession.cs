using System.Diagnostics;
using System.Reflection;
using System.Security.Cryptography;
using System.Text.Json;

namespace Grl.Integration;

public enum JobGateCapability { Disabled, Synthetic }
public sealed record JobGateIdentity(string Repo, long RunnerId, string RunnerName, string[] AllowedWorkflowShas);
public sealed record GateRegistration(long Id, string Name);
public interface IWorkerLifetimeObservation
{
    // True means a complete exact owned-tree/lifetime observation, never a best-effort list.
    bool ConfirmNoOwnedWorker();
}

/// <summary>Opt-in source adapter. Normal LiveWizardComposition does not construct this capability.
/// The single-registration snapshot assumes no concurrent enrollment; it is not distributed exclusion.</summary>
public sealed class JobGateSession
{
    private readonly string directory;
    private readonly string runner;
    private readonly string node;
    private readonly JobGateIdentity identity;
    private readonly HashSet<IOwnedRunnerProcess> emergencyStops = new(ReferenceEqualityComparer.Instance);
    private bool emergencyRecordingFailed;
    private readonly SemaphoreSlim operations = new(1, 1);
    private readonly JsonSerializerOptions json = new(JsonSerializerDefaults.Web);
    private static readonly string[] Resources = ["gate.cjs", "configuration.cjs", "control.cjs",
        "ownership.cjs", "offline-git-transport.cjs", "job-started.js", "job-completed.js"];

    public JobGateSession(JobGateCapability capability, string disposableGateDirectory,
        string disposableRunnerDirectory, string nodeExecutable, JobGateIdentity identity)
    {
        if (capability != JobGateCapability.Synthetic)
            throw new InvalidOperationException("GRL-014 capability is disabled; live activation is unavailable.");
        directory = ValidateDirectory(disposableGateDirectory);
        runner = ValidateDirectory(disposableRunnerDirectory);
        if (Within(runner, directory) || Within(directory, runner) ||
            directory.Split(Path.DirectorySeparatorChar).Any(x => x.Equals("_work", StringComparison.OrdinalIgnoreCase)))
            throw new InvalidOperationException("Hook files must be outside the runner application and workspace.");
        node = Path.GetFullPath(nodeExecutable);
        if (!Path.IsPathFullyQualified(nodeExecutable) || !File.Exists(node) || !RunnerBatchBoundary.OrdinaryAncestors(Path.GetDirectoryName(node)!))
            throw new InvalidOperationException("An existing explicit Node executable is required.");
        this.identity = identity;
    }

    public async Task CheckpointAndInstallSyntheticAsync(CancellationToken ct = default)
    {
        await operations.WaitAsync(ct);
        try
        {
            foreach (var name in Resources)
            {
                var bytes = Resource(name);
                var target = Path.Combine(directory, name);
                if (Path.Exists(target))
                {
                    if (!RunnerBatchBoundary.OrdinaryDescendantFile(directory, name) || !File.ReadAllBytes(target).SequenceEqual(bytes))
                        throw new InvalidOperationException("Existing hook resources are foreign or changed.");
                }
                else
                {
                    using var file = new FileStream(target, FileMode.CreateNew, FileAccess.Write, FileShare.None);
                    file.Write(bytes); file.Flush(true);
                }
            }
            await InvokeAsync("install", new { runner, identity.RunnerId }, ct);
        }
        finally { operations.Release(); }
    }

    public async Task StartupInactiveAsync(CancellationToken ct = default)
    {
        await operations.WaitAsync(ct);
        try { await VerifyAsync(ct); await InvokeAsync("initialize", identity, ct); }
        finally { operations.Release(); }
    }

    public async Task ActivateLocalAsync(IReadOnlyList<GateRegistration> freshRegistrations,
        IWorkerLifetimeObservation workers, CancellationToken ct = default)
    {
        await operations.WaitAsync(ct);
        try
        {
            await VerifyAsync(ct);
            RequireNoWorker(workers);
            await InvokeAsync("inspect", new { noOwnedWorker = true }, ct);
            await InvokeAsync("transition", new { mode = "ACTIVE", registrations = freshRegistrations, noOwnedWorker = true }, ct);
        }
        finally { operations.Release(); }
    }

    public async Task BeginReleaseAsync(CancellationToken ct = default)
    {
        await operations.WaitAsync(ct);
        try { await InvokeAsync("transition", new { mode = "DRAINING" }, ct); }
        finally { operations.Release(); }
    }

    public async Task CloseAdmissionAsync(CancellationToken ct = default)
    {
        await operations.WaitAsync(ct);
        try { await InvokeAsync("transition", new { mode = "INACTIVE" }, ct); }
        finally { operations.Release(); }
    }

    public async Task PauseAsync(IOwnedRunnerProcess owned, IWorkerLifetimeObservation workers,
        CancellationToken ct = default)
    {
        await operations.WaitAsync(ct);
        var stopAttempted = false;
        try
        {
            // Caller explicitly requests closure after its liveness wait. No timers authorize a stop.
            await InvokeAsync("transition", new { mode = "INACTIVE" }, ct);
            RequireNoWorker(workers);
            await InvokeAsync("inspect", new { noOwnedWorker = true }, ct);
            RequireNoWorker(workers); // Refresh immediately before termination, with gate closed.
            stopAttempted = true;
            if (owned is GateOwnedProcess gated)
            {
                if (!ReferenceEquals(gated.Gate, this)) throw new InvalidOperationException("Owned gate identity mismatch.");
                await gated.Inner.StopAsync(ct);
            }
            else await owned.StopAsync(ct);
            if (!owned.HasExited) throw new InvalidOperationException("Stop unresolved; recovery required.");
        }
        catch
        {
            // Preserve gate records and original lifecycle recovery; never manufacture Paused evidence.
            if (stopAttempted) await InvokeAsync("emergency", new { }, CancellationToken.None);
            throw;
        }
        finally { operations.Release(); }
    }

    public async Task StopNowAsync(IOwnedRunnerProcess owned, bool cancellationWarningAccepted,
        CancellationToken ct = default)
    {
        if (!cancellationWarningAccepted) throw new InvalidOperationException("Stop Now can cancel active work.");
        await operations.WaitAsync(ct);
        try
        {
            var target = owned;
            if (owned is GateOwnedProcess gated)
            {
                if (!ReferenceEquals(gated.Gate, this)) throw new InvalidOperationException("Owned gate identity mismatch.");
                target = gated.Inner;
            }
            if (emergencyStops.Contains(target))
            {
                if (emergencyRecordingFailed) throw new InvalidOperationException("Emergency stop already attempted; recovery recording failed.");
                if (!target.HasExited) throw new InvalidOperationException("Emergency stop already attempted; owned process exit remains unresolved.");
                return;
            }
            Exception? recordingFailure = null;
            try { await InvokeAsync("emergency", new { }, CancellationToken.None); }
            catch (Exception normalFailure)
            {
                try { RecordEmergencyFallback(); }
                catch (Exception fallbackFailure)
                {
                    emergencyRecordingFailed = true; // Also quarantine this session if persistence is unavailable.
                    recordingFailure = new AggregateException(normalFailure, fallbackFailure);
                }
            }
            // Once warned, recording/cancellation failures must not suppress or duplicate the kill.
            emergencyStops.Add(target);
            try { await target.StopAsync(CancellationToken.None); }
            catch (Exception stopFailure) when (recordingFailure is not null)
            { throw new AggregateException("Emergency stop and recovery recording failed.", recordingFailure, stopFailure); }
            if (recordingFailure is not null)
                throw new InvalidOperationException("Emergency stop attempted; durable recovery recording failed.", recordingFailure);
        }
        finally { operations.Release(); }
    }

    public async Task RestoreSyntheticConfigurationAsync(CancellationToken ct = default)
    {
        await operations.WaitAsync(ct);
        try { await InvokeAsync("restore", new { runner }, ct); }
        finally { operations.Release(); }
    }

    public async Task VerifyAsync(CancellationToken ct = default)
    {
        if (emergencyRecordingFailed || Path.Exists(Path.Combine(directory, "emergency-recovery-required.json")))
            throw new InvalidOperationException("Emergency recovery evidence requires reconciliation.");
        ValidateDirectory(directory); ValidateDirectory(runner);
        foreach (var name in Resources)
            if (!RunnerBatchBoundary.OrdinaryDescendantFile(directory, name) ||
                !SHA256.HashData(File.ReadAllBytes(Path.Combine(directory, name))).SequenceEqual(SHA256.HashData(Resource(name))))
                throw new InvalidOperationException("Gate-enabled start refuses missing or unverified hooks.");
        await InvokeAsync("verify", new { runner, identity.RunnerId }, ct);
    }

    private void RecordEmergencyFallback()
    {
        ValidateDirectory(directory);
        const string name = "emergency-recovery-required.json";
        var marker = Path.Combine(directory, name);
        if (Path.Exists(marker))
        {
            if (!RunnerBatchBoundary.OrdinaryDescendantFile(directory, name))
                throw new IOException("Emergency marker is not an ordinary file.");
            return; // Presence (including partial evidence) remains a permanent fail-closed fence.
        }
        using var file = new FileStream(marker, FileMode.CreateNew, FileAccess.Write, FileShare.Read);
        file.Write(JsonSerializer.SerializeToUtf8Bytes(new { v = 1, kind = "RECOVERY_REQUIRED", reason = "EMERGENCY_RECORD_UNAVAILABLE" }, json));
        file.Flush(true);
    }

    private static void RequireNoWorker(IWorkerLifetimeObservation workers)
    {
        if (!workers.ConfirmNoOwnedWorker()) throw new InvalidOperationException("Worker lifetime unknown or still running; stop refused.");
    }
    private async Task InvokeAsync(string command, object argument, CancellationToken ct)
    {
        var start = new ProcessStartInfo(node) { UseShellExecute = false, CreateNoWindow = true,
            RedirectStandardOutput = true, RedirectStandardError = true, WorkingDirectory = directory };
        start.ArgumentList.Add(Path.Combine(directory, "control.cjs"));
        start.ArgumentList.Add(command); start.ArgumentList.Add(directory);
        start.ArgumentList.Add(JsonSerializer.Serialize(argument, json));
        using var process = Process.Start(start) ?? throw new InvalidOperationException("Gate process unavailable.");
        var output = process.StandardOutput.ReadToEndAsync(ct);
        var error = process.StandardError.ReadToEndAsync(ct);
        // Cancellation does not imply that a mutation failed. Do not kill or retry as a new operation.
        await process.WaitForExitAsync(ct); await output; await error;
        if (process.ExitCode != 0) throw new InvalidOperationException("Gate operation refused or unresolved.");
    }
    private static byte[] Resource(string name)
    {
        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("Grl.Integration.JobGate." + name)
            ?? throw new InvalidOperationException("Gate resource missing.");
        using var memory = new MemoryStream(); stream.CopyTo(memory); return memory.ToArray();
    }
    private static string ValidateDirectory(string value)
    {
        if (!Path.IsPathFullyQualified(value)) throw new InvalidOperationException("Absolute ordinary root required.");
        var full = Path.GetFullPath(value).TrimEnd(Path.DirectorySeparatorChar);
        if (full.Length < 4 || !full.StartsWith("C:\\", StringComparison.OrdinalIgnoreCase) ||
            !Directory.Exists(full) || !RunnerBatchBoundary.OrdinaryAncestors(full))
            throw new InvalidOperationException("Ordinary local owned root required.");
        return full;
    }
    private static bool Within(string parent, string child) => child.Equals(parent, StringComparison.OrdinalIgnoreCase) ||
        child.StartsWith(parent + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase);
}

/// <summary>Explicit gate-enabled process adapter; no fallback to the wrapped ungated adapter.</summary>
public sealed class GatedRunnerProcessAdapter(IRunnerProcessAdapter inner, JobGateSession gate) : IRunnerProcessAdapter
{
    public Task ExecuteAsync(RunnerCommand command, CancellationToken ct) => inner.ExecuteAsync(command, ct);
    public Task<IRunnerCli> VerifyCliAsync(CancellationToken ct) => inner.VerifyCliAsync(ct);
    public Task<IRunnerCli> VerifyListenerVersionAsync(CancellationToken ct) => inner.VerifyListenerVersionAsync(ct);
    public async Task<IOwnedRunnerProcess> StartAsync(RunnerCommand command, string verifiedVersion, CancellationToken ct)
    {
        await gate.StartupInactiveAsync(ct);
        return new GateOwnedProcess(await inner.StartAsync(command, verifiedVersion, ct), gate);
    }
}

// Existing portable emergency Stop Now calls IOwnedRunnerProcess.StopAsync. Preserve its warning
// semantics while ensuring a gated adapter cannot omit the new RECOVERY_REQUIRED evidence.
internal sealed class GateOwnedProcess(IOwnedRunnerProcess inner, JobGateSession gate) : IOwnedRunnerProcess
{
    internal IOwnedRunnerProcess Inner => inner;
    internal JobGateSession Gate => gate;
    public bool HasExited => inner.HasExited;
    public Task StopAsync(CancellationToken ct) => gate.StopNowAsync(inner, true, ct);
    public void Dispose() => inner.Dispose();
}
