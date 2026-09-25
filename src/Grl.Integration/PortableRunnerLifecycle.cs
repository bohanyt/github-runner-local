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

public enum PortableRunnerFailure
{
    NameCollision, OnlineTimeout, DrainUnavailable, ActiveProcess, IdentityUncertain,
    RemoteUnavailable, PossibleSurvivingProcess, UnsupportedVersion, NoOwnedProcess, InvalidState
}

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
    private readonly PortableRecoveryStore? recovery;
    private readonly List<PortableRunnerJournalEntry> journal = [];
    private IOwnedRunnerProcess? owned;
    private long? runnerId;
    private bool registrationMayExist;
    private bool processMaySurvive;

    public PortableRunnerLifecycle(IRunnerAdministration admin, IRunnerCli cli, IRunnerProcessAdapter process,
        string name, string label, IAsyncDelay? delay = null, TimeProvider? clock = null,
        Func<bool>? isElevated = null, PortableRecoveryStore? recovery = null)
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
        this.recovery = recovery;
    }

    public PortableRunnerState State { get; private set; } = PortableRunnerState.Ready;
    public IReadOnlyList<PortableRunnerJournalEntry> Journal => journal.AsReadOnly();
    public bool HasOwnedProcess => owned is { HasExited: false };
    public long? RunnerId => runnerId;
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
        Record(PortableRunnerState.Configuring, "No matching remote runner; configuration is pending.");
        try
        {
            using (var token = await admin.CreateRegistrationTokenAsync(ct))
            {
                using var command = cli.BuildConfigure(new RunnerConfiguration(
                    new Uri($"https://github.com/{admin.Repository.FullName}"), token.Value,
                    name, label, "_work"));
                registrationMayExist = true;
                await process.ExecuteAsync(command, ct);
            }

            // A name match after a failed or uncertain configure cannot establish provenance.
            await ResolveRunnerIdAsync(maxPolls, pollInterval, requireOfflineIdle: true, ct);

            Record(PortableRunnerState.Configured, "Configuration completed and exact remote runner ID is persisted.");
            var verifiedCli = await VerifyBeforeStartAsync(ct);
            processMaySurvive = true;
            Record(PortableRunnerState.Starting, "Portable runner start requested after an immediate CLI version probe; a process may survive an app crash.");
            using var runCommand = verifiedCli.BuildRun();
            owned = await process.StartAsync(runCommand, verifiedCli.Capabilities.Version, ct);
            for (var attempt = 0; attempt < maxPolls; attempt++)
            {
                ct.ThrowIfCancellationRequested();
                var match = (await admin.ListAsync(ct)).SingleOrDefault(
                    x => x.Id == runnerId && x.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
                if (match?.Online == true)
                {
                    Record(PortableRunnerState.Online, "Exact runner name observed online.");
                    return;
                }
                if (owned.HasExited) break;
                if (attempt + 1 < maxPolls) await delay.WaitAsync(pollInterval, ct);
            }
            throw new PortableRunnerException(PortableRunnerFailure.OnlineTimeout,
                "Runner did not become online within the polling bound.");
        }
        catch (Exception error)
        {
            if (owned is null && error is RunnerProcessException { Failure: RunnerProcessFailure.StartupFailed })
                processMaySurvive = false;
            ReleaseExited();
            if (registrationMayExist && !HasOwnedProcess && !processMaySurvive)
            {
                Record(PortableRunnerState.RemoteRemovalPending,
                    runnerId is null
                        ? "Configuration or exact ID attribution is uncertain. Leave recovery pending and inspect repository Settings > Actions > Runners manually."
                        : "Configuration registered a remote runner but portable start did not complete; exact removal remains pending.");
            }
            else
            {
                Record(PortableRunnerState.Degraded, HasOwnedProcess
                    ? "Registration/start uncertain; owned process remains active. Explicit Stop Now may cancel work."
                    : "Registration/start incomplete; a process may survive or remote cleanup may be pending.");
            }
            throw;
        }
    }

    public Task DrainAsync(int maxPolls, TimeSpan pollInterval, CancellationToken ct)
    {
        RefuseElevation();
        if (maxPolls is < 1 or > 120 || !ValidInterval(pollInterval))
            throw new PortableRunnerException(PortableRunnerFailure.InvalidState, "Drain polling bound is invalid.");
        ct.ThrowIfCancellationRequested();
        Record(State, "Safe automatic Drain is unavailable; process and registration were not changed.");
        return Task.FromException(new PortableRunnerException(PortableRunnerFailure.DrainUnavailable,
            "Safe automatic Drain is unavailable for this portable runner."));
    }

    public async Task StopNowAsync(CancellationToken ct)
    {
        RefuseElevation();
        if (!HasOwnedProcess)
            throw new PortableRunnerException(PortableRunnerFailure.NoOwnedProcess,
                "No live product-owned runner process is available to stop.");
        await StopOwnedAsync(ct);
        processMaySurvive = false;
        Record(PortableRunnerState.Paused,
            "Explicit Stop Now terminated the owned process; an active job may have been cancelled.");
    }

    public async Task ResumeAsync(int maxPolls, TimeSpan interval, CancellationToken ct)
    {
        RefuseElevation();
        ReleaseExited();
        if (State != PortableRunnerState.Paused || owned is not null || runnerId is null ||
            maxPolls is < 1 or > 120 || !ValidInterval(interval))
            throw new PortableRunnerException(PortableRunnerFailure.InvalidState,
                "Resume state or polling bound is invalid.");

        IRunnerCli verifiedCli;
        try
        {
            verifiedCli = await VerifyBeforeStartAsync(ct);
            Record(PortableRunnerState.Paused,
                "Runner CLI version re-verified immediately before resume; no process has started yet.");
        }
        catch
        {
            processMaySurvive = false;
            Record(PortableRunnerState.Paused,
                "Resume blocked before process start because the runner CLI could not be verified at the reviewed version.");
            throw;
        }

        processMaySurvive = true;
        Record(PortableRunnerState.Starting,
            "Portable runner resume requested after an immediate CLI version probe; a process may survive an app crash.");
        try
        {
            using var runCommand = verifiedCli.BuildRun();
            owned = await process.StartAsync(runCommand, verifiedCli.Capabilities.Version, ct);
            for (var attempt = 0; attempt < maxPolls; attempt++)
            {
                ct.ThrowIfCancellationRequested();
                var match = (await admin.ListAsync(ct)).SingleOrDefault(
                    x => x.Id == runnerId && x.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
                if (match?.Online == true)
                {
                    Record(PortableRunnerState.Online, "Exact runner name observed online after resume.");
                    return;
                }
                if (owned.HasExited) break;
                if (attempt + 1 < maxPolls) await delay.WaitAsync(interval, ct);
            }
            throw new PortableRunnerException(PortableRunnerFailure.OnlineTimeout,
                "Runner did not resume online within the polling bound.");
        }
        catch (Exception error)
        {
            if (owned is null && error is RunnerProcessException { Failure: RunnerProcessFailure.StartupFailed })
                processMaySurvive = false;
            ReleaseExited();
            Record(PortableRunnerState.Degraded, HasOwnedProcess
                ? "Resume uncertain; owned process remains active. Explicit Stop Now may cancel work."
                : processMaySurvive
                    ? "Resume incomplete; a process may survive and remote registration remains pending."
                    : "Resume did not start a process; remote registration remains.");
            throw;
        }
    }

    public async Task UnregisterAsync(int maxPolls, TimeSpan interval, CancellationToken ct)
    {
        RefuseElevation();
        ReleaseExited();
        if (HasOwnedProcess)
            throw new PortableRunnerException(PortableRunnerFailure.ActiveProcess,
                "Owned runner process is active; unregister is pending. Stop Now is a separate warned action.");
        if (processMaySurvive)
            throw new PortableRunnerException(PortableRunnerFailure.PossibleSurvivingProcess,
                "A runner process may survive without current ownership. Retrying removal cannot make that evidence safe.");
        if (maxPolls is < 1 or > 120 || !ValidInterval(interval))
            throw new PortableRunnerException(PortableRunnerFailure.InvalidState, "Removal polling bound is invalid.");
        if (State is PortableRunnerState.Ready or PortableRunnerState.Removed)
            throw new PortableRunnerException(PortableRunnerFailure.InvalidState,
                "No configured runner is available for unregister.");

        try
        {
            if (runnerId is null)
                throw new PortableRunnerException(PortableRunnerFailure.IdentityUncertain,
                    "No numeric runner ID was bound after successful configuration. Inspect repository Settings > Actions > Runners manually.");

            var before = await admin.ListAsync(ct);
            var exact = before.SingleOrDefault(
                x => x.Id == runnerId && x.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
            if (exact is null || exact.Online || exact.Busy ||
                before.Any(x => x.Name.Equals(name, StringComparison.OrdinalIgnoreCase) && x.Id != runnerId))
                throw new PortableRunnerException(PortableRunnerFailure.IdentityUncertain,
                    "Exact remote runner identity is not uniquely confirmed offline and non-busy.");

            using (var token = await admin.CreateRemoveTokenAsync(ct))
            using (var command = cli.BuildRemove(token.Value))
                await process.ExecuteAsync(command, ct);

            for (var attempt = 0; attempt < maxPolls; attempt++)
            {
                if (!(await admin.ListAsync(ct)).Any(
                    x => x.Id == runnerId || x.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
                {
                    registrationMayExist = false;
                    Record(PortableRunnerState.Removed, "Exact runner absent from repository list.");
                    return;
                }
                if (attempt + 1 < maxPolls) await delay.WaitAsync(interval, ct);
            }
            Record(PortableRunnerState.RemoteRemovalPending,
                "Removal command completed; remote absence remains unverified.");
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            Record(PortableRunnerState.RemoteRemovalPending,
                "Removal cancelled; remote state is unverified.");
            throw;
        }
        catch (PortableRunnerException error)
            when (error.Failure is PortableRunnerFailure.IdentityUncertain or
                PortableRunnerFailure.PossibleSurvivingProcess)
        {
            Record(PortableRunnerState.RemoteRemovalPending,
                "Removal is evidence-blocked. Retrying alone cannot establish safety; inspect the exact repository runner only after independently confirming no listener is active.");
            throw;
        }
        catch
        {
            Record(PortableRunnerState.RemoteRemovalPending,
                "Remote runner status or removal is unavailable; retry only after connectivity is restored.");
            throw;
        }
    }

    public Task DeleteStaleRemoteAsync(long id, string confirmedName, CancellationToken ct)
    {
        RefuseElevation();
        return admin.DeleteStaleAsync(id, confirmedName, ct);
    }

    public async Task<RepositoryRunner?> GetRemoteStatusAsync(CancellationToken ct) =>
        (await admin.ListAsync(ct)).SingleOrDefault(
            x => x.Id == runnerId && x.Name.Equals(name, StringComparison.OrdinalIgnoreCase));

    private async Task<RepositoryRunner> ResolveRunnerIdAsync(
        int maxPolls, TimeSpan interval, bool requireOfflineIdle, CancellationToken ct)
    {
        Exception? lastListError = null;
        var successfulLists = 0;
        for (var attempt = 0; attempt < maxPolls; attempt++)
        {
            ct.ThrowIfCancellationRequested();
            IReadOnlyList<RepositoryRunner> listed;
            try
            {
                listed = await admin.ListAsync(ct);
                successfulLists++;
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception error)
            {
                lastListError = error;
                if (attempt + 1 < maxPolls) await delay.WaitAsync(interval, ct);
                continue;
            }

            var matches = listed
                .Where(x => x.Name.Equals(name, StringComparison.OrdinalIgnoreCase))
                .ToArray();
            if (matches.Length > 1)
                throw new PortableRunnerException(PortableRunnerFailure.IdentityUncertain,
                    "Multiple exact-name runners exist; numeric identity cannot be chosen safely.");
            if (matches.Length == 1)
            {
                var match = matches[0];
                if (match.Id <= 0)
                    throw new PortableRunnerException(PortableRunnerFailure.IdentityUncertain,
                        "The exact-name runner has no usable numeric identity.");
                if (requireOfflineIdle && (match.Online || match.Busy))
                    throw new PortableRunnerException(PortableRunnerFailure.IdentityUncertain,
                        "The exact-name runner is online or busy; removal/start is evidence-blocked.");
                runnerId = match.Id;
                Record(State, "Exact remote runner ID resolved and persisted after successful configuration, before portable start.");
                return match;
            }

            if (attempt + 1 < maxPolls) await delay.WaitAsync(interval, ct);
        }

        if (successfulLists == 0 && lastListError is not null)
            throw new PortableRunnerException(PortableRunnerFailure.RemoteUnavailable,
                "GitHub runner status could not be read within the bounded identity-resolution poll.");

        throw new PortableRunnerException(PortableRunnerFailure.IdentityUncertain,
            "No unique exact-name runner ID became visible within the bounded poll. Retrying alone cannot prove identity.");
    }

    private async Task<IRunnerCli> VerifyBeforeStartAsync(CancellationToken ct)
    {
        try
        {
            var verified = await process.VerifyCliAsync(ct);
            if (verified.Capabilities.Version != RunnerPin.ReviewedVersion)
                throw new PortableRunnerException(PortableRunnerFailure.UnsupportedVersion,
                    $"Runner self-update/version drift detected. This wizard supports only {RunnerPin.ReviewedVersion}.");
            return verified;
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (PortableRunnerException)
        {
            throw;
        }
        catch (Exception error) when (error is RunnerCliException or RunnerProcessException)
        {
            throw new PortableRunnerException(PortableRunnerFailure.UnsupportedVersion,
                $"Runner version could not be verified immediately before start. It may have self-updated; this wizard supports only {RunnerPin.ReviewedVersion}.");
        }
    }

    private void ReleaseExited()
    {
        if (owned is { HasExited: true })
        {
            owned.Dispose();
            owned = null;
            processMaySurvive = false;
        }
    }

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
        recovery?.Write(runnerId, state, processMaySurvive);
    }

    private static bool Simple(string value) =>
        !string.IsNullOrWhiteSpace(value) && value.Length <= 128 &&
        value.All(c => char.IsAsciiLetterOrDigit(c) || c is '-' or '_' or '.');

    private static bool ValidInterval(TimeSpan value) =>
        value >= TimeSpan.Zero && value <= TimeSpan.FromMinutes(1);

    private void RefuseElevation()
    {
        if (elevated())
            throw new RunnerProcessException(RunnerProcessFailure.Elevated,
                "Portable runner mutation requires an unelevated process.");
    }

    public void Dispose()
    {
        owned?.Dispose();
        owned = null;
    }
}
