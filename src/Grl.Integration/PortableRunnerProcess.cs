using System.Diagnostics;
using System.Security.Principal;
using System.Text.RegularExpressions;

namespace Grl.Integration;

public enum RunnerProcessFailure { Elevated, InvalidRoot, UnsafeCommand, StartupFailed, CommandFailed }

public sealed class RunnerProcessException(RunnerProcessFailure failure, string message) : Exception(message)
{
    public RunnerProcessFailure Failure { get; } = failure;
}

public interface IOwnedRunnerProcess : IDisposable
{
    bool HasExited { get; }
    Task StopAsync(CancellationToken ct);
}

public interface IRunnerProcessAdapter
{
    Task ExecuteAsync(RunnerCommand command, CancellationToken ct);
    Task<IRunnerCli> VerifyCliAsync(CancellationToken ct);
    /// <summary>Read-only listener version probe; never invokes config.cmd.</summary>
    Task<IRunnerCli> VerifyListenerVersionAsync(CancellationToken ct);
    Task<IOwnedRunnerProcess> StartAsync(RunnerCommand command, string verifiedVersion, CancellationToken ct);
}

public static class RunnerBatchBoundary
{
    private static readonly Regex SafeArgument = new(@"\A[A-Za-z0-9._:/,-]{1,512}\z", RegexOptions.CultureInvariant);

    public static void SetRunEnvironment(ProcessStartInfo start, string verifiedVersion)
    {
        if (verifiedVersion != RunnerPin.ReviewedVersion)
            throw new RunnerProcessException(RunnerProcessFailure.UnsafeCommand, "A verified supported runner version is required.");
        start.Environment["GRL_RUNNER_VERSION"] = verifiedVersion;
        start.Environment["GRL_IDENTITY_CLASS"] = "portable-user";
    }

    public static ProcessStartInfo Build(string verifiedRoot, RunnerCommand command, string commandInterpreter)
    {
        if (!OperatingSystem.IsWindows() || !Path.IsPathFullyQualified(verifiedRoot) ||
            !verifiedRoot.StartsWith("C:\\", StringComparison.OrdinalIgnoreCase) ||
            verifiedRoot.Any(c => char.IsControl(c) || c is '%' or '!' or '&' or '|' or '<' or '>' or '^' or '"' or '(' or ')') ||
            !Directory.Exists(verifiedRoot) || !OrdinaryAncestors(verifiedRoot) ||
            command.EntryPoint is not ("config.cmd" or "run.cmd") ||
            !File.Exists(Path.Combine(verifiedRoot, command.EntryPoint)) ||
            (File.GetAttributes(Path.Combine(verifiedRoot, command.EntryPoint)) & FileAttributes.ReparsePoint) != 0 ||
            !string.Equals(Path.GetFullPath(commandInterpreter), Path.Combine(Environment.SystemDirectory, "cmd.exe"),
                StringComparison.OrdinalIgnoreCase) || !File.Exists(commandInterpreter))
            throw new RunnerProcessException(RunnerProcessFailure.InvalidRoot, "The verified runner entrypoint is unavailable.");
        if (command.Arguments.Count > 32 || command.Arguments.Any(x => x is null || !SafeArgument.IsMatch(x)) ||
            command.Arguments.Any(x => x is "--replace" or "--disableupdate" or "--runasservice") ||
            !ReviewedShape(command))
            throw new RunnerProcessException(RunnerProcessFailure.UnsafeCommand, "Runner arguments failed the batch boundary.");
        var start = new ProcessStartInfo(commandInterpreter)
        {
            WorkingDirectory = verifiedRoot,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };
        // The first command is a fixed file in WorkingDirectory. All subsequent
        // arguments pass a strict metacharacter/expansion/quote exclusion boundary.
        start.ArgumentList.Add("/d");
        start.ArgumentList.Add("/c");
        start.ArgumentList.Add(command.EntryPoint);
        foreach (var argument in command.Arguments) start.ArgumentList.Add(argument);
        return start;
    }

    private static bool ReviewedShape(RunnerCommand command)
    {
        var a = command.Arguments;
        if (command.EntryPoint == "run.cmd") return a.Count == 0;
        if (a.SequenceEqual(["--help"])) return true;
        if (a.Count == 3 && a[0] == "remove" && a[1] == "--token")
            return SafeToken(a[2]);
        if (a.Count is < 11 or > 13 ||
            a[0] != "--unattended" || a[1] != "--url" || a[3] != "--token" ||
            a[5] != "--name" || a[7] != "--labels" || a[9] != "--work" ||
            !Uri.TryCreate(a[2], UriKind.Absolute, out var url) || url.Scheme != "https" ||
            url.Host != "github.com" || !url.IsDefaultPort || url.UserInfo.Length != 0 ||
            url.Query.Length != 0 || url.Fragment.Length != 0 ||
            !Regex.IsMatch(url.AbsolutePath, @"\A/[A-Za-z0-9-]+/[A-Za-z0-9_.-]+\z") ||
            url.AbsolutePath.Contains("..", StringComparison.Ordinal) ||
            !SafeToken(a[4]) || !Simple(a[6]) || !a[8].Split(',').All(Simple) || !Simple(a[10])) return false;
        var flags = a.Skip(11).ToArray();
        return flags.SequenceEqual([]) || flags.SequenceEqual(["--ephemeral"]) ||
            flags.SequenceEqual(["--no-default-labels"]) ||
            flags.SequenceEqual(["--ephemeral", "--no-default-labels"]);
    }

    private static bool SafeToken(string value) =>
        value.Length is >= 1 and <= 512 && value.All(c => char.IsAsciiLetterOrDigit(c) || c is '-' or '_');

    private static bool Simple(string value) =>
        value.Length is >= 1 and <= 128 && value.All(c => char.IsAsciiLetterOrDigit(c) || c is '-' or '_' or '.');

    internal static bool OrdinaryAncestors(string root)
    {
        for (var cursor = root; cursor is not null; cursor = Directory.GetParent(cursor)?.FullName)
            if ((File.GetAttributes(cursor) & FileAttributes.ReparsePoint) != 0) return false;
        return true;
    }
}

public sealed class PortableRunnerProcess : IRunnerProcessAdapter
{
    private readonly string root;
    private readonly string cmd;
    private readonly Func<bool> elevated;
    private readonly bool resumeOnly;

    public PortableRunnerProcess(RunnerInstallResult installed, Func<bool>? isElevated = null)
    {
        if (!string.Equals(installed.Sha256, RunnerPin.ReviewedSha256, StringComparison.OrdinalIgnoreCase))
            throw new RunnerProcessException(RunnerProcessFailure.InvalidRoot, "The reviewed runner package is required.");
        root = Path.GetFullPath(installed.FinalRoot);
        cmd = Path.Combine(Environment.SystemDirectory, "cmd.exe");
        elevated = isElevated ?? IsElevated;
    }

    private PortableRunnerProcess(string existingRoot, Func<bool>? isElevated)
    {
        root = Path.GetFullPath(existingRoot);
        cmd = Path.Combine(Environment.SystemDirectory, "cmd.exe");
        elevated = isElevated ?? IsElevated;
        resumeOnly = true;
    }

    /// <summary>
    /// An already-configured root from an earlier app session. No package hash is claimed:
    /// only the listener version probe and the existing run.cmd are available, never config.cmd.
    /// </summary>
    public static PortableRunnerProcess ForExistingRoot(string existingRoot, Func<bool>? isElevated = null)
    {
        if (!Path.IsPathFullyQualified(existingRoot))
            throw new RunnerProcessException(RunnerProcessFailure.InvalidRoot, "The existing runner root is unavailable.");
        return new PortableRunnerProcess(existingRoot, isElevated);
    }

    public async Task ExecuteAsync(RunnerCommand command, CancellationToken ct)
    {
        RefuseElevation();
        if (resumeOnly || command.EntryPoint != "config.cmd")
            throw new RunnerProcessException(RunnerProcessFailure.UnsafeCommand, "Configuration entrypoint required.");
        using var process = Start(command);
        using var limit = CancellationTokenSource.CreateLinkedTokenSource(ct);
        limit.CancelAfter(TimeSpan.FromMinutes(5));
        try
        {
            await process.WaitForExitAsync(limit.Token);
            if (process.ExitCode != 0)
                throw new RunnerProcessException(RunnerProcessFailure.CommandFailed, "Runner configuration failed; inspect local runner state before retrying.");
        }
        catch (OperationCanceledException)
        {
            if (!process.HasExited) process.Kill(entireProcessTree: true);
            throw;
        }
    }

    public Task<IOwnedRunnerProcess> StartAsync(RunnerCommand command, string verifiedVersion, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        RefuseElevation();
        if (command.EntryPoint != "run.cmd" || command.Arguments.Count != 0)
            throw new RunnerProcessException(RunnerProcessFailure.UnsafeCommand, "Portable run entrypoint required.");
        var info = RunnerBatchBoundary.Build(root, command, cmd);
        RunnerBatchBoundary.SetRunEnvironment(info, verifiedVersion);
        var process = Start(info);
        return Task.FromResult<IOwnedRunnerProcess>(new OwnedRunnerProcess(process));
    }

    public async Task<IRunnerCli> VerifyCliAsync(CancellationToken ct)
    {
        RefuseElevation();
        if (resumeOnly)
            throw new RunnerProcessException(RunnerProcessFailure.UnsafeCommand, "A reopened root verifies its listener version only.");
        var version = ListenerVersionProbe();
        var help = RunnerBatchBoundary.Build(root, new RunnerCommand("config.cmd", ["--help"]), cmd);
        var versionOutput = await ProbeAsync(version, ct);
        var helpOutput = await ProbeAsync(help, ct);
        return RunnerCliContract.Verify(versionOutput, helpOutput);
    }

    public async Task<IRunnerCli> VerifyListenerVersionAsync(CancellationToken ct)
    {
        RefuseElevation();
        return RunnerCliContract.VerifyRunOnly(await ProbeAsync(ListenerVersionProbe(), ct));
    }

    private ProcessStartInfo ListenerVersionProbe()
    {
        var listener = Path.Combine(root, "bin", "Runner.Listener.exe");
        if (!Directory.Exists(root) || !RunnerBatchBoundary.OrdinaryAncestors(root) ||
            !File.Exists(listener) || (File.GetAttributes(listener) & FileAttributes.ReparsePoint) != 0)
            throw new RunnerProcessException(RunnerProcessFailure.InvalidRoot, "The reviewed listener is unavailable.");
        var version = new ProcessStartInfo(listener)
        {
            WorkingDirectory = root, UseShellExecute = false, CreateNoWindow = true,
            RedirectStandardOutput = true, RedirectStandardError = true
        };
        version.ArgumentList.Add("--version");
        return version;
    }

    private static async Task<string> ProbeAsync(ProcessStartInfo info, CancellationToken ct)
    {
        using var limit = CancellationTokenSource.CreateLinkedTokenSource(ct);
        limit.CancelAfter(TimeSpan.FromSeconds(45));
        using var process = new Process { StartInfo = info };
        try
        {
            if (!process.Start()) throw new InvalidOperationException();
            var stdout = process.StandardOutput.ReadToEndAsync(limit.Token);
            var stderr = process.StandardError.ReadToEndAsync(limit.Token);
            await process.WaitForExitAsync(limit.Token);
            var result = await stdout + "\n" + await stderr;
            if (process.ExitCode != 0 || result.Length > 262_144)
                throw new RunnerProcessException(RunnerProcessFailure.CommandFailed, "Read-only runner CLI probe failed.");
            return result;
        }
        catch (OperationCanceledException)
        {
            if (!process.HasExited) process.Kill(entireProcessTree: true);
            throw;
        }
        catch (Exception e) when (e is InvalidOperationException or System.ComponentModel.Win32Exception)
        {
            throw new RunnerProcessException(RunnerProcessFailure.StartupFailed, "Read-only runner CLI probe could not start.");
        }
    }

    private Process Start(RunnerCommand command) => Start(RunnerBatchBoundary.Build(root, command, cmd));

    private static Process Start(ProcessStartInfo info)
    {
        var process = new Process { StartInfo = info };
        try
        {
            if (!process.Start()) throw new InvalidOperationException();
            // Never retain or publish CLI output. The runner may echo sensitive context.
            _ = process.StandardOutput.BaseStream.CopyToAsync(Stream.Null);
            _ = process.StandardError.BaseStream.CopyToAsync(Stream.Null);
            return process;
        }
        catch (Exception e) when (e is InvalidOperationException or System.ComponentModel.Win32Exception)
        {
            process.Dispose();
            throw new RunnerProcessException(RunnerProcessFailure.StartupFailed, "The runner process could not start.");
        }
    }

    private void RefuseElevation()
    {
        if (elevated()) throw new RunnerProcessException(RunnerProcessFailure.Elevated, "Portable runner mutation requires an unelevated process.");
    }

    public static bool IsElevated()
    {
        if (!OperatingSystem.IsWindows()) return true;
        using var identity = WindowsIdentity.GetCurrent();
        return new WindowsPrincipal(identity).IsInRole(WindowsBuiltInRole.Administrator);
    }

    private sealed class OwnedRunnerProcess(Process process) : IOwnedRunnerProcess
    {
        public bool HasExited => process.HasExited;
        public async Task StopAsync(CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            if (process.HasExited) return;
            process.Kill(entireProcessTree: true);
            await process.WaitForExitAsync(ct);
        }
        public void Dispose() => process.Dispose();
    }
}
