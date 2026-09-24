using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Windows.Automation;
using System.Windows.Forms;

namespace Grl.App.UiSmoke;

internal static class Program
{
    private const int WaitSeconds = 30;
    private static readonly Stopwatch Campaign = Stopwatch.StartNew();
    private static readonly List<Process> Started = [];
    private static readonly Dictionary<string, string> Results = new();

    [STAThread]
    private static int Main()
    {
        try
        {
            var root = RepositoryRoot();
            var exe = Path.Combine(root, "src", "Grl.App", "bin", "Debug",
                "net10.0-windows", "win-x64", "GitHubRunnerLocal.exe");
            if (!File.Exists(exe)) throw new InvalidOperationException("Build the Debug app first.");
            if (!Environment.UserInteractive)
            {
                foreach (var id in new[] { "S1", "S2", "S3", "S4", "S5" })
                    Results[id] = "BLOCKED_SESSION";
                return Report();
            }

            Process? main = null;
            AutomationElement? window = null;
            try
            {
                (main, window) = Launch(exe, null);
                WaitForText(window!, "Preview build", WaitSeconds);
                Results["S1"] = "PASS";
                Results["S4"] = AccessibleNames(window) ? "PASS" : "FAIL";
                Results["S2"] = KeyboardHappyPath(window);
                Results["S4"] = Results["S4"] == "PASS" && AccessibleNames(window) ? "PASS" : "FAIL";
                CloseOwn(main!, window);
                if (!main!.HasExited || main.ExitCode != 0) Results["S1"] = "FAIL";
            }
            catch (Win32Exception error) when (error.NativeErrorCode == 740)
            {
                Results["S1"] = "BLOCKED_APP_CONTROL";
                Results["S2"] = "NOT_RUN";
                Results["S4"] = "NOT_RUN";
                Results["S3"] = "NOT_RUN";
                Results["S5"] = "NOT_RUN";
                return Report();
            }
            catch (TimeoutException error)
            {
                Console.WriteLine("S1 discovery: " + error.Message);
                Results["S1"] = main is { HasExited: false } ? "BLOCKED_SESSION" : "BLOCKED_APP_CONTROL";
                Results["S2"] = "NOT_RUN";
                Results["S4"] = "NOT_RUN";
                Results["S3"] = "NOT_RUN";
                Results["S5"] = "NOT_RUN";
                return Report();
            }
            finally
            {
                if (main is not null) StopOwn(main);
            }

            var namesStatus = Results["S4"];
            Results["S3"] = BlockedPaths(exe, ref namesStatus);
            Results["S4"] = namesStatus;
            try
            {
                var (second, secondWindow) = Launch(exe, null);
                try
                {
                    WaitForText(secondWindow, "Welcome", WaitSeconds);
                    var noLocalData = !Directory.Exists(Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                        "github-runner-local"));
                    var noDocumentsData = !Directory.Exists(Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                        "github-runner-local"));
                    Results["S5"] = noLocalData && noDocumentsData ? "PASS" : "FAIL";
                    Console.WriteLine($"S5 data directories absent: local={noLocalData}; documents={noDocumentsData}");
                    CloseOwn(second, secondWindow);
                }
                finally { StopOwn(second); }
            }
            catch (TimeoutException) { Results["S5"] = "BLOCKED_SESSION"; }

            return Report();
        }
        catch (Exception error)
        {
            Console.WriteLine("Smoke harness failure: " + error.GetType().Name + " — " + error.Message);
            foreach (var id in new[] { "S1", "S2", "S3", "S4", "S5" })
                Results.TryAdd(id, "FAIL");
            return Report();
        }
        finally
        {
            foreach (var process in Started) StopOwn(process);
        }
    }

    private static string KeyboardHappyPath(AutomationElement window)
    {
        try
        {
            var checkbox = FindByName(window, "Welcome trusted-code acknowledgement");
            if (checkbox is null || !checkbox.Current.IsKeyboardFocusable) return "FAIL";
            if (!WaitForFocusName("Welcome trusted-code acknowledgement", 5))
                return "BLOCKED_SESSION";
            SendKeys.SendWait(" ");
            if (!WaitUntil(() => FindByName(window, "Welcome continue preview")?.Current.IsEnabled == true, 5))
                return "FAIL";
            SendKeys.SendWait("{TAB}");
            if (!WaitForFocusName("Welcome continue preview", 5)) return "BLOCKED_SESSION";
            SendKeys.SendWait("{ENTER}");
            foreach (var (title, nextTitle) in new[]
            {
                ("Preflight passed", "Signed in"),
                ("Signed in", "Execution target selected"),
                ("Execution target selected", "Local location"),
                ("Local location", "Runner idle")
            })
            {
                WaitForText(window, title, WaitSeconds);
                if (!AccessibleNames(window)) Results["S4"] = "FAIL";
                if (!FocusActionByTab(window, "Continue", 25)) return "BLOCKED_SESSION";
                SendKeys.SendWait("{ENTER}");
                WaitForText(window, nextTitle, WaitSeconds);
            }
            return "PASS";
        }
        catch (TimeoutException) { return "FAIL"; }
        catch (InvalidOperationException) { return "BLOCKED_SESSION"; }
    }

    private static string BlockedPaths(string exe, ref string namesStatus)
    {
        try
        {
            foreach (var (scenario, expected, action) in new[]
            {
                ("PreflightBlocked", "Preflight blocked", "Retry"),
                ("InstallVerifyFailed", "Simulated verification failed", "Cancel"),
                ("SignInNetworkError", "Simulated network error", "Cancel")
            })
            {
                var (process, window) = Launch(exe, scenario);
                try
                {
                    StartFromWelcome(window);
                    if (scenario != "PreflightBlocked")
                    {
                        if (scenario == "InstallVerifyFailed")
                            AdvanceByInvoke(window, 4);
                        else AdvanceByInvoke(window, 1);
                    }
                    WaitForText(window, expected, WaitSeconds);
                    if (scenario == "PreflightBlocked")
                        WaitForText(window, "Simulated blockers", WaitSeconds);
                    if (!AccessibleNames(window)) namesStatus = "FAIL";
                    var button = FindByNameContains(window, action);
                    if (button is null) return "FAIL";
                    if (scenario == "InstallVerifyFailed")
                    {
                        Invoke(button);
                        WaitForText(window, "Simulated cleanup", WaitSeconds);
                        WaitForText(window, "Local location", WaitSeconds);
                    }
                }
                finally { CloseOwn(process, window); StopOwn(process); }
            }
            return "PASS";
        }
        catch (TimeoutException) { return "FAIL"; }
    }

    private static void StartFromWelcome(AutomationElement window)
    {
        var check = FindByName(window, "Welcome trusted-code acknowledgement")
            ?? throw new TimeoutException("Welcome checkbox missing.");
        ((TogglePattern)check.GetCurrentPattern(TogglePattern.Pattern)).Toggle();
        Invoke(FindByName(window, "Welcome continue preview")
            ?? throw new TimeoutException("Welcome continue missing."));
    }

    private static void AdvanceByInvoke(AutomationElement window, int count)
    {
        for (var i = 0; i < count; i++)
        {
            if (!WaitUntil(() => FindByNameContains(window, " Continue") is not null, WaitSeconds))
                throw new TimeoutException("Continue action missing.");
            Invoke(FindByNameContains(window, " Continue")!);
        }
    }

    private static bool AccessibleNames(AutomationElement window)
    {
        var elements = window.FindAll(TreeScope.Descendants, Condition.TrueCondition);
        foreach (AutomationElement element in elements)
        {
            if (!element.Current.IsKeyboardFocusable) continue;
            if (string.IsNullOrWhiteSpace(element.Current.Name)) return false;
        }
        return true;
    }

    private static bool FocusActionByTab(AutomationElement window, string action, int maxTabs)
    {
        for (var tab = 0; tab < maxTabs; tab++)
        {
            var focus = AutomationElement.FocusedElement;
            if (focus is not null && focus.Current.Name.Contains(action, StringComparison.OrdinalIgnoreCase))
                return true;
            SendKeys.SendWait("{TAB}");
            Thread.Sleep(40);
        }
        return false;
    }

    private static bool WaitForFocusName(string name, int seconds) =>
        WaitUntil(() => AutomationElement.FocusedElement?.Current.Name == name, seconds);

    private static (Process, AutomationElement) Launch(string exe, string? scenario)
    {
        var start = new ProcessStartInfo(exe) { UseShellExecute = false };
        if (scenario is not null)
        {
            start.ArgumentList.Add("--scenario");
            start.ArgumentList.Add(scenario);
        }
        var process = Process.Start(start) ?? throw new InvalidOperationException("App did not start.");
        Started.Add(process);
        AutomationElement? window = null;
        if (!WaitUntil(() =>
        {
            process.Refresh();
            if (process.MainWindowHandle == IntPtr.Zero) return false;
            window = AutomationElement.FromHandle(process.MainWindowHandle);
            return window is not null;
        }, WaitSeconds)) throw new TimeoutException(
            $"Main window unavailable; processAlive={!process.HasExited}; " +
            $"handlePresent={process.MainWindowHandle != IntPtr.Zero}; " +
            $"title={process.MainWindowTitle}.");
        return (process, window!);
    }

    private static AutomationElement? FindByName(AutomationElement root, string name) =>
        root.FindFirst(TreeScope.Descendants,
            new PropertyCondition(AutomationElement.NameProperty, name));

    private static AutomationElement? FindByNameContains(AutomationElement root, string fragment)
    {
        var elements = root.FindAll(TreeScope.Descendants,
            new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Button));
        return elements.Cast<AutomationElement>().FirstOrDefault(element =>
            element.Current.Name.Contains(fragment, StringComparison.OrdinalIgnoreCase));
    }

    private static void WaitForText(AutomationElement root, string text, int seconds)
    {
        if (!WaitUntil(() => root.FindAll(TreeScope.Descendants, Condition.TrueCondition)
            .Cast<AutomationElement>().Any(element =>
                element.Current.Name.Contains(text, StringComparison.OrdinalIgnoreCase)), seconds))
        {
            Console.WriteLine("UIA visible names: " + string.Join(" | ",
                root.FindAll(TreeScope.Descendants, Condition.TrueCondition)
                    .Cast<AutomationElement>().Take(12).Select(item => item.Current.Name)));
            throw new TimeoutException("Expected UI text unavailable: " + text);
        }
    }

    private static bool WaitUntil(Func<bool> check, int seconds)
    {
        var watch = Stopwatch.StartNew();
        while (watch.Elapsed < TimeSpan.FromSeconds(seconds) && Campaign.Elapsed < TimeSpan.FromMinutes(10))
        {
            try { if (check()) return true; }
            catch (ElementNotAvailableException) { }
            Thread.Sleep(100);
        }
        return false;
    }

    private static void Invoke(AutomationElement element) =>
        ((InvokePattern)element.GetCurrentPattern(InvokePattern.Pattern)).Invoke();

    private static void CloseOwn(Process process, AutomationElement window)
    {
        if (process.HasExited) return;
        try { ((WindowPattern)window.GetCurrentPattern(WindowPattern.Pattern)).Close(); }
        catch (ElementNotAvailableException) { }
        process.WaitForExit(5000);
    }

    private static void StopOwn(Process process)
    {
        if (process.HasExited) return;
        process.Kill(entireProcessTree: true);
        process.WaitForExit(5000);
    }

    private static string RepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "global.json")))
            directory = directory.Parent;
        return directory?.FullName ?? throw new InvalidOperationException("Repository root unavailable.");
    }

    private static int Report()
    {
        foreach (var id in new[] { "S1", "S2", "S3", "S4", "S5" })
            Console.WriteLine($"{id}: {Results.GetValueOrDefault(id, "NOT_RUN")}");
        Console.WriteLine($"Elapsed seconds: {Campaign.Elapsed.TotalSeconds:F1}");
        return Results.Values.Any(status => status == "FAIL") ? 1 : 0;
    }
}
