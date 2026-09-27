using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Grl.Integration;

public sealed record ProcessLifetime(int Id, int ParentId, long Created, string Image);
public sealed record ProcessInventory(bool Complete, IReadOnlyList<ProcessLifetime> Processes);

/// <summary>Conservative: any owned work descendant blocks, even after its completed hook wrote .done.
/// A held root lifetime plus complete Windows snapshot and retained descendant lifetimes are required.
/// A Worker from the same runner image directory also blocks if its parent has already exited.</summary>
public sealed class OwnedWorkerObservation : IWorkerLifetimeObservation
{
    private readonly ProcessLifetime owner;
    private readonly string? runnerDirectory;
    private readonly Func<ProcessInventory> snapshot;
    private readonly Func<ProcessLifetime, bool?> exited;
    private readonly Dictionary<(int, long), ProcessLifetime> observed = [];
    public OwnedWorkerObservation(ProcessLifetime owner, string runnerDirectory,
        Func<ProcessInventory> snapshot, Func<ProcessLifetime, bool?> exited)
    {
        this.owner = owner;
        try { this.runnerDirectory = CanonicalPath(runnerDirectory); }
        catch { this.runnerDirectory = null; } // Invalid/relative paths never authorize absence.
        this.snapshot = snapshot; this.exited = exited;
    }
    public bool ConfirmNoOwnedWorker()
    {
        try
        {
            if (runnerDirectory is null) return false;
            var view = snapshot();
            // Canonicalize all queried images first: unknown paths are uncertainty.
            var images = view.Processes.ToDictionary(p => p, p => CanonicalPath(p.Image));
            if (!view.Complete || exited(owner) != false ||
                !view.Processes.Any(p => p.Id == owner.Id && p.Created == owner.Created)) return false;
            var parents = new HashSet<int> { owner.Id };
            bool changed;
            do
            {
                changed = false;
                foreach (var p in view.Processes)
                    if (p.Id != owner.Id && parents.Contains(p.ParentId) && parents.Add(p.Id))
                    {
                        // Listener/console/batch hosts must remain alive until the planned stop.
                        // Other children, including hook/profile children, are conservatively work.
                        if (Path.GetFileName(images[p]) is not ("Runner.Listener.exe" or "conhost.exe" or "cmd.exe"))
                            observed[(p.Id, p.Created)] = p;
                        changed = true;
                    }
            } while (changed);
            foreach (var p in view.Processes)
                if (Path.GetFileName(images[p]).Equals("Runner.Worker.exe", StringComparison.OrdinalIgnoreCase) &&
                    IsContained(runnerDirectory, images[p]))
                    observed[(p.Id, p.Created)] = p;
            // PID disappearance/reuse is insufficient. The exact lifetime must be confirmed exited.
            return observed.Values.All(p => exited(p) == true);
        }
        catch { return false; }
    }
    private static string CanonicalPath(string value)
    {
        if (string.IsNullOrWhiteSpace(value) || !Path.IsPathFullyQualified(value) ||
            value.IndexOfAny(Path.GetInvalidPathChars()) >= 0 || value.Contains('*') || value.Contains('?'))
            throw new ArgumentException("Fully qualified process path required.");
        return Path.TrimEndingDirectorySeparator(Path.GetFullPath(value));
    }
    private static bool IsContained(string directory, string image)
    {
        var relative = Path.GetRelativePath(directory, image);
        return !Path.IsPathRooted(relative) && relative != ".." &&
            relative.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)[0] != "..";
    }
}

/// <summary>Read-only Toolhelp snapshot. Query failure/partial enumeration is uncertainty, never absence.</summary>
public static class WindowsProcessInventory
{
    public static ProcessInventory Capture(int ownedRootId)
    {
        if (!OperatingSystem.IsWindows()) return new(false, []);
        var handle = CreateToolhelp32Snapshot(2, 0);
        if (handle == new IntPtr(-1)) return new(false, []);
        try
        {
            var rows = new List<ProcessLifetime>();
            var raw = new List<(int Id, int Parent, string Name)>();
            var entry = new Entry { Size = (uint)Marshal.SizeOf<Entry>(), Name = "" };
            if (!Process32First(handle, ref entry)) return new(false, []);
            do
            {
                if (entry.Id != 0) raw.Add(((int)entry.Id, (int)entry.ParentId, entry.Name));
                entry.Size = (uint)Marshal.SizeOf<Entry>();
            } while (Process32Next(handle, ref entry));
            if (Marshal.GetLastWin32Error() != 18) return new(false, rows);
            var tree = new HashSet<int> { ownedRootId };
            bool changed;
            do
            {
                changed = false;
                foreach (var p in raw) if (tree.Contains(p.Parent) && tree.Add(p.Id)) changed = true;
            } while (changed);
            foreach (var p in raw.Where(p => tree.Contains(p.Id) ||
                p.Name.Equals("Runner.Worker.exe", StringComparison.OrdinalIgnoreCase)))
            {
                try
                {
                    using var process = Process.GetProcessById(p.Id);
                    rows.Add(new(p.Id, p.Parent, process.StartTime.ToUniversalTime().Ticks,
                        process.MainModule?.FileName ?? throw new InvalidOperationException()));
                }
                catch { return new(false, rows); } // Permission/disappearing rows are uncertainty.
            }
            return new(true, rows);
        }
        finally { CloseHandle(handle); }
    }
    public static bool? HasExited(ProcessLifetime lifetime)
    {
        try
        {
            using var process = Process.GetProcessById(lifetime.Id);
            if (process.HasExited) return true;
            // A different creation time proves that this PID's old lifetime ended.
            return process.StartTime.ToUniversalTime().Ticks != lifetime.Created;
        }
        catch (ArgumentException) { return true; }
        catch { return null; }
    }
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct Entry
    {
        public uint Size, Usage, Id;
        public IntPtr Heap;
        public uint Module, Threads, ParentId;
        public int Priority;
        public uint Flags;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)] public string Name;
    }
    [DllImport("kernel32.dll", SetLastError = true)] private static extern IntPtr CreateToolhelp32Snapshot(uint flags, uint pid);
    [DllImport("kernel32.dll", EntryPoint = "Process32FirstW", CharSet = CharSet.Unicode, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)] private static extern bool Process32First(IntPtr handle, ref Entry entry);
    [DllImport("kernel32.dll", EntryPoint = "Process32NextW", CharSet = CharSet.Unicode, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)] private static extern bool Process32Next(IntPtr handle, ref Entry entry);
    [DllImport("kernel32.dll")] [return: MarshalAs(UnmanagedType.Bool)] private static extern bool CloseHandle(IntPtr handle);
}
