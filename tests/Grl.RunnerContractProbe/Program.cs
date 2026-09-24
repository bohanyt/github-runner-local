using System.Diagnostics;
using Grl.Integration;

if (!OperatingSystem.IsWindows())
{
    Console.Error.WriteLine("BLOCKED: Windows runner contract probe requires Windows.");
    return 2;
}

var pinFile = args.Length == 1 ? args[0] : "runner-pins.json";
var pin = RunnerPin.LoadReviewed(pinFile);
var tempRoot = Path.Combine(Path.GetTempPath(), "grl-c-probe-" + Guid.NewGuid().ToString("N"));
Directory.CreateDirectory(tempRoot);
try
{
    using var http = new HttpClient { Timeout = TimeSpan.FromMinutes(10) };
    var installed = await new RunnerPackageInstaller().DownloadAndInstallAsync(
        http, pin, tempRoot, "runner", CancellationToken.None);
    Console.WriteLine($"release=v{pin.Version}");
    Console.WriteLine($"asset={pin.Asset}");
    Console.WriteLine($"sha256_match={installed.Sha256 == RunnerPin.ReviewedSha256}");
    Console.WriteLine($"download_bytes={installed.DownloadBytes}");
    Console.WriteLine($"extracted_bytes={installed.ExtractedBytes}");
    Console.WriteLine($"entries={installed.EntryCount}");

    var listener = await ReadOnlyProbeAsync(
        Path.Combine(installed.FinalRoot, "bin", "Runner.Listener.exe"), ["--version"],
        installed.FinalRoot);
    Console.WriteLine($"listener_exit={listener.ExitCode}");
    Console.WriteLine($"listener_version={listener.Output.Trim()}");
    if (listener.ExitCode != 0) throw new InvalidOperationException("Listener version probe failed.");

    // This is the only invocation of config.cmd: its read-only help form.
    var help = await ReadOnlyProbeAsync("cmd.exe", ["/d", "/c", "config.cmd --help"], installed.FinalRoot);
    Console.WriteLine($"config_help_exit={help.ExitCode}");
    var contract = RunnerCliContract.Verify(listener.Output, help.Output);
    Console.WriteLine($"required_cli_capabilities={string.Join(',', RunnerCliContract.RequiredHelpOptions)}");
    Console.WriteLine($"capability_count={contract.Capabilities.Options.Count}");
    Console.WriteLine("registration_started=false; run_cmd_started=false; token_used=false; service_changed=false");
    Console.WriteLine("probe_exit=PASS");
    return 0;
}
catch (Exception error)
{
    Console.Error.WriteLine($"probe_exit=BLOCKED; reason={error.GetType().Name}: {error.Message}");
    return 1;
}
finally
{
    // The directory was uniquely created by this process and only the reviewed,
    // hash-verified archive can populate it.
    Directory.Delete(tempRoot, recursive: true);
}

static async Task<(int ExitCode, string Output)> ReadOnlyProbeAsync(
    string executable, IReadOnlyList<string> arguments, string workingDirectory)
{
    using var process = new Process();
    process.StartInfo = new ProcessStartInfo(executable)
    {
        WorkingDirectory = workingDirectory,
        UseShellExecute = false,
        RedirectStandardOutput = true,
        RedirectStandardError = true,
        CreateNoWindow = true
    };
    foreach (var argument in arguments) process.StartInfo.ArgumentList.Add(argument);
    using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(45));
    process.Start();
    var stdout = process.StandardOutput.ReadToEndAsync(timeout.Token);
    var stderr = process.StandardError.ReadToEndAsync(timeout.Token);
    await process.WaitForExitAsync(timeout.Token);
    return (process.ExitCode, (await stdout) + Environment.NewLine + (await stderr));
}
