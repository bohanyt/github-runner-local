using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace Grl.Integration;

public sealed record PortableRecoveryEvidence(
    string Repository, string RunnerName, string Root, long? RunnerId, PortableRunnerState State,
    bool MayHaveUnownedProcess)
{
    [JsonIgnore]
    public bool CanAttemptExactRemoteRemoval => RunnerId is > 0 && !MayHaveUnownedProcess &&
        State != PortableRunnerState.Removed;
}

/// <summary>Non-secret, recovery-only evidence. A file is never proof of process ownership.</summary>
public sealed class PortableRecoveryStore : IDisposable
{
    private const int MaximumBytes = 4096;
    private readonly string root;
    private readonly string statePath;
    private readonly FileStream lease;
    private readonly string repository;
    private readonly string runnerName;

    private PortableRecoveryStore(string root, string repository, string runnerName)
    {
        this.root = Path.GetFullPath(root);
        this.repository = repository;
        this.runnerName = runnerName;
        RequireOrdinaryRoot(this.root);
        statePath = Path.Combine(this.root, ".grl-recovery.json");
        var lockPath = Path.Combine(this.root, ".grl-recovery.lock");
        RequireOrdinaryFile(lockPath);
        RequireOrdinaryFile(statePath);
        lease = new FileStream(lockPath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
    }

    public static PortableRecoveryStore Open(string root, string repository, string runnerName)
    {
        if (!Regex.IsMatch(repository, @"\A[A-Za-z0-9-]{1,39}/[A-Za-z0-9_.-]{1,100}\z") ||
            !Regex.IsMatch(runnerName, @"\A[A-Za-z0-9._-]{1,128}\z"))
            throw new InvalidOperationException("Recovery identity is invalid.");
        return new PortableRecoveryStore(root, repository, runnerName);
    }

    public PortableRecoveryEvidence? Read()
    {
        RequireOrdinaryRoot(root);
        RequireOrdinaryFile(statePath);
        if (!File.Exists(statePath)) return null;
        using var input = new FileStream(statePath, FileMode.Open, FileAccess.Read, FileShare.Read);
        if (input.Length is 0 or > MaximumBytes) throw new InvalidOperationException("Recovery evidence is invalid.");
        var bytes = new byte[(int)input.Length];
        input.ReadExactly(bytes);
        if (input.ReadByte() != -1) throw new InvalidOperationException("Recovery evidence is invalid.");
        try
        {
            using var document = JsonDocument.Parse(bytes);
            if (document.RootElement.ValueKind != JsonValueKind.Object ||
                document.RootElement.EnumerateObject().Count() != 6 ||
                !document.RootElement.TryGetProperty("MayHaveUnownedProcess", out var survival) ||
                survival.ValueKind is not (JsonValueKind.True or JsonValueKind.False))
                throw new InvalidOperationException("Recovery evidence is invalid.");
            var value = JsonSerializer.Deserialize<PortableRecoveryEvidence>(bytes);
            if (value is null || value.Repository != repository || value.RunnerName != runnerName ||
                !string.Equals(value.Root, root, StringComparison.OrdinalIgnoreCase) ||
                value.RunnerId is <= 0 || !Enum.IsDefined(value.State))
                throw new InvalidOperationException("Recovery identity is invalid.");
            return value;
        }
        catch (JsonException) { throw new InvalidOperationException("Recovery evidence is invalid."); }
    }

    public void Write(long? runnerId, PortableRunnerState state, bool mayHaveUnownedProcess = false)
    {
        if (runnerId is <= 0 || !Enum.IsDefined(state)) throw new InvalidOperationException("Recovery state is invalid.");
        RequireOrdinaryRoot(root);
        RequireOrdinaryFile(statePath);
        var bytes = JsonSerializer.SerializeToUtf8Bytes(new PortableRecoveryEvidence(
            repository, runnerName, root, runnerId, state, mayHaveUnownedProcess));
        if (bytes.Length > MaximumBytes) throw new InvalidOperationException("Recovery evidence exceeds its bound.");
        var temporary = Path.Combine(root, ".grl-recovery-" + Guid.NewGuid().ToString("N") + ".tmp");
        try
        {
            using (var output = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                output.Write(bytes);
                output.Flush(flushToDisk: true);
            }
            RequireOrdinaryFile(statePath);
            File.Move(temporary, statePath, overwrite: true);
        }
        finally { if (File.Exists(temporary)) File.Delete(temporary); }
    }

    private static void RequireOrdinaryRoot(string path)
    {
        if (!Path.IsPathFullyQualified(path) || !Directory.Exists(path))
            throw new InvalidOperationException("Recovery root is unavailable.");
        for (var cursor = path; cursor is not null; cursor = Directory.GetParent(cursor)?.FullName)
            if ((File.GetAttributes(cursor) & FileAttributes.ReparsePoint) != 0)
                throw new InvalidOperationException("Recovery root is redirected.");
    }

    private static void RequireOrdinaryFile(string path)
    {
        if (new FileInfo(path).LinkTarget is not null ||
            (Path.Exists(path) && (File.GetAttributes(path) & FileAttributes.ReparsePoint) != 0))
            throw new InvalidOperationException("Recovery file is redirected.");
    }

    public void Dispose() => lease.Dispose();
}
