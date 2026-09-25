namespace Grl.Integration;

public enum PortableRunnerState
{
    Ready, Configuring, Configured, Starting, Online, Draining, Paused,
    Degraded, RemoteRemovalPending, Removed
}

public sealed record PortableRunnerJournalEntry(DateTimeOffset At, PortableRunnerState State, string Evidence)
{
    public override string ToString() => $"{At:O} {State}: {Evidence}";
}

public enum PortableRunnerFailure { NameCollision, OnlineTimeout, DrainTimeout, NoOwnedProcess, InvalidState }

public sealed class PortableRunnerException(PortableRunnerFailure failure, string message) : Exception(message)
{
    public PortableRunnerFailure Failure { get; } = failure;
}

public sealed class PortableRunnerLifecycle : IDisposable
{
    private readonly IRunnerAdministration admin;
    private readonly IRunnerCli cli;
    private readonly IRunnerProcessAdapter process;
    private readonly IAsyncDelay delay;
    private readonly TimeProvider clock;
    private readonly Func<bool> elevated;
    private readonly string name;
    private readonly string label;
    private readonly List<PortableRunnerJournalEntry> journal = [];
    private IOwnedRunnerProcess? owned;

    public PortableRunnerLifecycle(IRunnerAdministration admin, IRunnerCli cli, IRunnerProcessAdapter process,
        string name, string label, IAsyncDelay? delay = null, TimeProvider? clock = null,
        Func<bool>? isElevated = null)
    {
        ArgumentNullException.ThrowIfNull(admin);
        ArgumentNullException.ThrowIfNull(cli);
        ArgumentNullException.ThrowIfNull(process);
        if (!Simple(name) || !Simple(label) || !admin.Repository.IsPrivate || !admin.Repository.HasAdminPermission)
            throw new ArgumentException("A private administration target and simple runner identity are required.");
        this.admin = admin;
        this.cli = cli;
        this.process = process;
        this.name = name;
        this.label = label;
        this.delay = delay ?? new SystemAsyncDelay();
        this.clock = clock ?? TimeProvider.System;
        elevated = isElevated ?? PortableRunnerProcess.IsElevated;
    }

    public PortableRunnerState State { get; private set; } = PortableRunnerState.Ready;
    public IReadOnlyList<PortableRunnerJournalEntry> Journal => journal.AsReadOnly();
    public bool HasOwnedProcess => owned is { HasExited: false };
    public string RunnerName => name;
    public GitHubRepository Repository => admin.Repository;

    public async Task RegisterAndStartAsync(int maxPolls, TimeSpan pollInterval, CancellationToken ct)
    {
        RefuseElevation();
        if (State != PortableRunnerState.Ready || maxPolls is < 1 or > 120 || !ValidInterval(pollInterval))
            throw new PortableRunnerException(PortableRunnerFailure.InvalidState, "Registration parameters or state are invalid.");
        var existing = await admin.ListAsync(ct);
        if (existing.Any(x => x.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
            throw new PortableRunnerException(PortableRunnerFailure.NameCollision, "The runner name is already registered.");
        Record(PortableRunnerState.Configuring, "No matching remote runner; configuration started.");
        try
        {
            using (var token = await admin.CreateRegistrationTokenAsync(ct))
            {
                using var command = cli.BuildConfigure(new RunnerConfiguration(
                    new Uri($"https://github.com/{admin.Repository.FullName}"), token.Value,
                    name, label, "_work"));
                await process.ExecuteAsync(command, ct);
            }
            Record(PortableRunnerState.Configured, "Configuration completed; remote status unconfirmed.");
            Record(PortableRunnerState.Starting, "Portable runner start requested.");
            using var runCommand = cli.BuildRun();
            owned = await process.StartAsync(runCommand, ct);
            for (var attempt = 0; attempt < maxPolls; attempt++)
            {
                ct.ThrowIfCancellationRequested();
                var match = (await admin.ListAsync(ct)).SingleOrDefault(x => x.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
                if (match?.Online == true)
                {
                    Record(PortableRunnerState.Online, "Exact runner name observed online.");
                    return;
                }
                if (owned.HasExited) break;
                if (attempt + 1 < maxPolls) await delay.WaitAsync(pollInterval, ct);
            }
            throw new PortableRunnerException(PortableRunnerFailure.OnlineTimeout, "Runner did not become online within the polling bound.");
        }
        catch
        {
            if (owned is not null)
            {
                try { await owned.StopAsync(CancellationToken.None); } catch { /* retain degraded state */ }
                owned.Dispose();
                owned = null;
            }
            Record(PortableRunnerState.Degraded, "Registration/start incomplete; manual inspection or explicit unregister required.");
            throw;
        }
    }

    public async Task DrainAsync(int maxPolls, TimeSpan pollInterval, CancellationToken ct)
    {
        RefuseElevation();
        if (owned is null) throw new PortableRunnerException(PortableRunnerFailure.NoOwnedProcess, "No product-owned runner process is active.");
        if (maxPolls is < 1 or > 120 || !ValidInterval(pollInterval))
            throw new PortableRunnerException(PortableRunnerFailure.InvalidState, "Drain polling bound is invalid.");
        Record(PortableRunnerState.Draining, "Waiting for the exact runner to become idle.");
        for (var attempt = 0; attempt < maxPolls; attempt++)
        {
            ct.ThrowIfCancellationRequested();
            var match = (await admin.ListAsync(ct)).SingleOrDefault(x => x.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
            if (match is null || !match.Busy)
            {
                await StopOwnedAsync(ct);
                Record(PortableRunnerState.Paused, "Owned runner process stopped after idle observation.");
                return;
            }
            if (attempt + 1 < maxPolls) await delay.WaitAsync(pollInterval, ct);
        }
        throw new PortableRunnerException(PortableRunnerFailure.DrainTimeout, "Runner remained busy; no process was stopped.");
    }

    public async Task StopNowAsync(CancellationToken ct)
    {
        RefuseElevation();
        if (owned is null) throw new PortableRunnerException(PortableRunnerFailure.NoOwnedProcess, "No product-owned runner process is active.");
        await StopOwnedAsync(ct);
        Record(PortableRunnerState.Paused, "Explicit stop-now terminated only the owned runner process.");
    }

    public async Task ResumeAsync(int maxPolls, TimeSpan interval, CancellationToken ct)
    {
        RefuseElevation();
        if (State != PortableRunnerState.Paused || owned is not null || maxPolls is < 1 or > 120 || !ValidInterval(interval))
            throw new PortableRunnerException(PortableRunnerFailure.InvalidState, "Resume state or polling bound is invalid.");
        Record(PortableRunnerState.Starting, "Portable runner resume requested.");
        try
        {
            using var runCommand = cli.BuildRun();
            owned = await process.StartAsync(runCommand, ct);
            for (var attempt = 0; attempt < maxPolls; attempt++)
            {
                var match = (await admin.ListAsync(ct)).SingleOrDefault(x => x.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
                if (match?.Online == true)
                {
                    Record(PortableRunnerState.Online, "Exact runner name observed online after resume.");
                    return;
                }
                if (owned.HasExited) break;
                if (attempt + 1 < maxPolls) await delay.WaitAsync(interval, ct);
            }
            throw new PortableRunnerException(PortableRunnerFailure.OnlineTimeout, "Runner did not resume online within the polling bound.");
        }
        catch
        {
            if (owned is not null)
            {
                try { await owned.StopAsync(CancellationToken.None); } catch { }
                owned.Dispose();
                owned = null;
            }
            Record(PortableRunnerState.Degraded, "Resume incomplete; manual inspection required.");
            throw;
        }
    }

    public async Task UnregisterAsync(int maxPolls, TimeSpan interval, CancellationToken ct)
    {
        RefuseElevation();
        if (owned is not null) await DrainAsync(maxPolls, interval, ct);
        if (State is PortableRunnerState.Ready or PortableRunnerState.Removed)
            throw new PortableRunnerException(PortableRunnerFailure.InvalidState, "No configured runner is available for unregister.");
        try
        {
            using (var token = await admin.CreateRemoveTokenAsync(ct))
            using (var command = cli.BuildRemove(token.Value))
                await process.ExecuteAsync(command, ct);
            for (var attempt = 0; attempt < maxPolls; attempt++)
            {
                if (!(await admin.ListAsync(ct)).Any(x => x.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
                {
                    Record(PortableRunnerState.Removed, "Exact runner absent from repository list.");
                    return;
                }
                if (attempt + 1 < maxPolls) await delay.WaitAsync(interval, ct);
            }
            Record(PortableRunnerState.RemoteRemovalPending, "Removal command completed; remote absence unverified.");
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
        catch
        {
            Record(PortableRunnerState.RemoteRemovalPending, "Remote removal unavailable or unverified; retry explicitly.");
            throw;
        }
    }

    public Task DeleteStaleRemoteAsync(long id, string confirmedName, CancellationToken ct)
    {
        RefuseElevation();
        return admin.DeleteStaleAsync(id, confirmedName, ct);
    }

    public async Task<RepositoryRunner?> GetRemoteStatusAsync(CancellationToken ct) =>
        (await admin.ListAsync(ct)).SingleOrDefault(x => x.Name.Equals(name, StringComparison.OrdinalIgnoreCase));

    private async Task StopOwnedAsync(CancellationToken ct)
    {
        var target = owned!;
        await target.StopAsync(ct);
        target.Dispose();
        owned = null;
    }

    private void Record(PortableRunnerState state, string evidence)
    {
        State = state;
        journal.Add(new PortableRunnerJournalEntry(clock.GetUtcNow(), state, evidence));
        if (journal.Count > 100) journal.RemoveAt(0);
    }

    private static bool Simple(string value) =>
        !string.IsNullOrWhiteSpace(value) && value.Length <= 128 &&
        value.All(c => char.IsAsciiLetterOrDigit(c) || c is '-' or '_' or '.');

    private static bool ValidInterval(TimeSpan value) => value >= TimeSpan.Zero && value <= TimeSpan.FromMinutes(1);

    private void RefuseElevation()
    {
        if (elevated()) throw new RunnerProcessException(RunnerProcessFailure.Elevated,
            "Portable runner mutation requires an unelevated process.");
    }

    public void Dispose()
    {
        owned?.Dispose();
        owned = null;
    }
}
