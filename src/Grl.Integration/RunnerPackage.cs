using System.IO.Compression;
using System.Security.Cryptography;
using System.Text.Json;

namespace Grl.Integration;

public enum RunnerPackageFailure
{
    InvalidPin, DownloadFailed, SizeLimit, HashMismatch, UnsafeArchive, ExistingRoot
}

public sealed class RunnerPackageException(RunnerPackageFailure failure, string message) : Exception(message)
{
    public RunnerPackageFailure Failure { get; } = failure;
}

public sealed record RunnerPin(string Version, string Asset, Uri Url, string Sha256)
{
    public const string ReviewedVersion = "2.337.0";
    public const string ReviewedAsset = "actions-runner-win-x64-2.337.0.zip";
    public const string ReviewedSha256 = "1150692afa94e71f872017e254ea55b6eece1eece3fe7e3a6d4c93d0a1b85cfc";

    public static RunnerPin Parse(string json)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            var version = root.GetProperty("version").GetString() ?? "";
            var asset = root.GetProperty("asset").GetString() ?? "";
            var urlText = root.GetProperty("url").GetString() ?? "";
            var hash = root.GetProperty("sha256").GetString() ?? "";
            var expectedAsset = $"actions-runner-win-x64-{version}.zip";
            var expectedUrl = $"https://github.com/actions/runner/releases/download/v{version}/{expectedAsset}";
            if (!System.Text.RegularExpressions.Regex.IsMatch(version, @"^\d+\.\d+\.\d+$") ||
                asset != expectedAsset || urlText != expectedUrl ||
                hash.Length != 64 || !hash.All(Uri.IsHexDigit))
                throw InvalidPin();
            return new RunnerPin(version, asset, new Uri(urlText), hash.ToLowerInvariant());
        }
        catch (Exception e) when (e is JsonException or KeyNotFoundException or InvalidOperationException or UriFormatException)
        {
            throw InvalidPin();
        }
    }

    public static RunnerPin LoadReviewed(string filePath)
    {
        var pin = Parse(File.ReadAllText(filePath));
        if (pin.Version != ReviewedVersion || pin.Asset != ReviewedAsset ||
            !pin.Sha256.Equals(ReviewedSha256, StringComparison.OrdinalIgnoreCase))
            throw InvalidPin();
        return pin;
    }

    private static RunnerPackageException InvalidPin() =>
        new(RunnerPackageFailure.InvalidPin, "The runner pin is not the reviewed official Windows x64 release.");
}

public sealed record RunnerPackageLimits(long MaxDownloadBytes, long MaxEntryBytes, long MaxExtractedBytes, int MaxEntries)
{
    public static RunnerPackageLimits Default { get; } = new(200_000_000, 500_000_000, 1_500_000_000, 20_000);
}

public sealed record RunnerInstallResult(string FinalRoot, string Sha256, long DownloadBytes, long ExtractedBytes, int EntryCount);

public sealed class RunnerPackageInstaller(RunnerPackageLimits? limits = null)
{
    private readonly RunnerPackageLimits limits = limits ?? RunnerPackageLimits.Default;

    public async Task<RunnerInstallResult> DownloadAndInstallAsync(
        HttpClient http, RunnerPin pin, string ownedRoot, string finalName, CancellationToken ct)
    {
        // Re-derive and validate the URL before any network request.
        var checkedPin = RunnerPin.Parse(JsonSerializer.Serialize(new
        {
            version = pin.Version, asset = pin.Asset, url = pin.Url.ToString(), sha256 = pin.Sha256
        }));
        if (checkedPin.Version != RunnerPin.ReviewedVersion ||
            checkedPin.Sha256 != RunnerPin.ReviewedSha256)
            throw new RunnerPackageException(RunnerPackageFailure.InvalidPin, "The reviewed runner pin is required.");
        using var request = new HttpRequestMessage(HttpMethod.Get, checkedPin.Url);
        HttpResponseMessage response;
        try { response = await http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct); }
        catch (HttpRequestException) { throw new RunnerPackageException(RunnerPackageFailure.DownloadFailed, "Runner download failed."); }
        using (response)
        {
            if (!response.IsSuccessStatusCode)
                throw new RunnerPackageException(RunnerPackageFailure.DownloadFailed, "Runner download failed.");
            if (response.Content.Headers.ContentLength > limits.MaxDownloadBytes)
                throw new RunnerPackageException(RunnerPackageFailure.SizeLimit, "Runner download exceeds the size limit.");
            await using var stream = await response.Content.ReadAsStreamAsync(ct);
            return await InstallFromStreamAsync(stream, checkedPin.Sha256, ownedRoot, finalName, ct);
        }
    }

    // Also used by deterministic synthetic-archive tests. The downloaded bytes enter
    // this same hash-before-extract path; no caller can skip the verification step.
    public async Task<RunnerInstallResult> InstallFromStreamAsync(
        Stream archiveStream, string expectedSha256, string ownedRoot, string finalName, CancellationToken ct)
    {
        if (expectedSha256.Length != 64 || !expectedSha256.All(Uri.IsHexDigit))
            throw new RunnerPackageException(RunnerPackageFailure.InvalidPin, "A SHA-256 digest is required.");
        var root = Path.GetFullPath(ownedRoot);
        if (!Directory.Exists(root) || IsReparse(root))
            throw Unsafe("The caller-owned root must be an ordinary existing directory.");
        EnsureOrdinaryAncestors(root);
        ValidateSegment(finalName);
        var finalRoot = Path.GetFullPath(Path.Combine(root, finalName));
        EnsureChild(root, finalRoot);
        if (Path.Exists(finalRoot))
            throw new RunnerPackageException(RunnerPackageFailure.ExistingRoot, "The final runner root already exists.");

        var stage = Path.Combine(root, ".grl-stage-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(stage);
        var archivePath = Path.Combine(stage, "package.zip");
        var extractRoot = Path.Combine(stage, "extract");
        try
        {
            long downloaded = 0;
            using (var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256))
            await using (var output = new FileStream(archivePath, FileMode.CreateNew, FileAccess.Write, FileShare.None, 81920, FileOptions.Asynchronous))
            {
                var buffer = new byte[81920];
                int read;
                while ((read = await archiveStream.ReadAsync(buffer, ct)) != 0)
                {
                    downloaded = checked(downloaded + read);
                    if (downloaded > limits.MaxDownloadBytes)
                        throw new RunnerPackageException(RunnerPackageFailure.SizeLimit, "Runner download exceeds the size limit.");
                    hash.AppendData(buffer, 0, read);
                    await output.WriteAsync(buffer.AsMemory(0, read), ct);
                }
                var actual = Convert.ToHexStringLower(hash.GetHashAndReset());
                if (!actual.Equals(expectedSha256, StringComparison.OrdinalIgnoreCase))
                    throw new RunnerPackageException(RunnerPackageFailure.HashMismatch, "Runner package SHA-256 did not match.");
            }

            Directory.CreateDirectory(extractRoot);
            var (extracted, entries) = await ExtractVerifiedAsync(archivePath, extractRoot, ct);
            EnsureOrdinaryTree(extractRoot);
            if (Path.Exists(finalRoot))
                throw new RunnerPackageException(RunnerPackageFailure.ExistingRoot, "The final runner root already exists.");
            Directory.Move(extractRoot, finalRoot);
            return new RunnerInstallResult(finalRoot, expectedSha256.ToLowerInvariant(), downloaded, extracted, entries);
        }
        finally
        {
            SafeDeleteStage(root, stage);
        }
    }

    private async Task<(long Bytes, int Entries)> ExtractVerifiedAsync(string archivePath, string extractRoot, CancellationToken ct)
    {
        try
        {
            using var archive = ZipFile.OpenRead(archivePath);
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            long total = 0;
            var count = 0;
            foreach (var entry in archive.Entries)
            {
                ct.ThrowIfCancellationRequested();
                if (++count > limits.MaxEntries)
                    throw new RunnerPackageException(RunnerPackageFailure.SizeLimit, "Runner archive has too many entries.");
                var relative = entry.FullName.Replace('\\', '/');
                var directory = relative.EndsWith('/');
                relative = relative.TrimEnd('/');
                if (relative.Length == 0 || (entry.ExternalAttributes & (int)FileAttributes.ReparsePoint) != 0 ||
                    (((uint)entry.ExternalAttributes >> 16) & 0xF000) == 0xA000)
                    throw Unsafe("Runner archive contains an unsafe entry.");
                var segments = relative.Split('/');
                foreach (var segment in segments) ValidateSegment(segment);
                var canonical = string.Join('/', segments);
                if (!seen.Add(canonical))
                    throw Unsafe("Runner archive contains duplicate paths.");
                if (entry.Length < 0 || entry.Length > limits.MaxEntryBytes ||
                    total > limits.MaxExtractedBytes - entry.Length)
                    throw new RunnerPackageException(RunnerPackageFailure.SizeLimit, "Runner archive exceeds the extraction size limit.");
                var destination = Path.GetFullPath(Path.Combine(extractRoot, Path.Combine(segments)));
                EnsureChild(extractRoot, destination);
                if (directory)
                {
                    if (entry.Length != 0) throw Unsafe("Runner archive directory has content.");
                    Directory.CreateDirectory(destination);
                    continue;
                }
                Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
                EnsureOrdinaryAncestors(Path.GetDirectoryName(destination)!);
                await using var output = new FileStream(destination, FileMode.CreateNew, FileAccess.Write, FileShare.None, 81920, FileOptions.Asynchronous);
                using var input = entry.Open();
                var buffer = new byte[81920];
                long copied = 0;
                int read;
                while ((read = await input.ReadAsync(buffer, ct)) != 0)
                {
                    copied = checked(copied + read);
                    if (copied > limits.MaxEntryBytes || total > limits.MaxExtractedBytes - copied)
                        throw new RunnerPackageException(RunnerPackageFailure.SizeLimit, "Runner archive exceeds the extraction size limit.");
                    await output.WriteAsync(buffer.AsMemory(0, read), ct);
                }
                if (copied != entry.Length) throw Unsafe("Runner archive entry length changed.");
                total = checked(total + copied);
            }
            return (total, count);
        }
        catch (InvalidDataException) { throw Unsafe("Runner archive is invalid."); }
    }

    private static void ValidateSegment(string segment)
    {
        if (segment.Length == 0 || segment is "." or ".." || segment[^1] is '.' or ' ' ||
            segment.Any(c => c < 32 || c is ':' or '<' or '>' or '"' or '|' or '?' or '*' or '\\' or '/') ||
            IsDeviceName(segment))
            throw Unsafe("Runner archive contains an unsafe Windows path.");
    }

    private static bool IsDeviceName(string segment)
    {
        var baseName = segment.Split('.')[0].TrimEnd(' ');
        return baseName.Equals("CON", StringComparison.OrdinalIgnoreCase) ||
            baseName.Equals("PRN", StringComparison.OrdinalIgnoreCase) ||
            baseName.Equals("AUX", StringComparison.OrdinalIgnoreCase) ||
            baseName.Equals("NUL", StringComparison.OrdinalIgnoreCase) ||
            baseName.Equals("CONIN$", StringComparison.OrdinalIgnoreCase) ||
            baseName.Equals("CONOUT$", StringComparison.OrdinalIgnoreCase) ||
            (baseName.Length == 4 && (baseName.StartsWith("COM", StringComparison.OrdinalIgnoreCase) ||
                                      baseName.StartsWith("LPT", StringComparison.OrdinalIgnoreCase)) &&
             baseName[3] is >= '1' and <= '9');
    }

    private static void EnsureChild(string root, string path)
    {
        if (!path.StartsWith(root.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
            throw Unsafe("Runner path escaped its owned root.");
    }

    private static bool IsReparse(string path) =>
        (File.GetAttributes(path) & FileAttributes.ReparsePoint) != 0;

    private static void EnsureOrdinaryAncestors(string path)
    {
        for (var current = new DirectoryInfo(path); current is not null; current = current.Parent)
            if (current.Exists && (current.Attributes & FileAttributes.ReparsePoint) != 0)
                throw Unsafe("A runner path crosses a reparse point.");
    }

    private static void EnsureOrdinaryTree(string root)
    {
        var pending = new Stack<string>();
        pending.Push(root);
        while (pending.TryPop(out var path))
        {
            if (IsReparse(path)) throw Unsafe("Extracted runner contains a reparse point.");
            foreach (var child in Directory.EnumerateFileSystemEntries(path))
            {
                if (IsReparse(child)) throw Unsafe("Extracted runner contains a reparse point.");
                if (Directory.Exists(child)) pending.Push(child);
            }
        }
    }

    // Never follow a link during failure cleanup. Every deletion target is checked
    // lexically against the unique stage path created beneath the caller-owned root.
    private static void SafeDeleteStage(string root, string stage)
    {
        if (!Directory.Exists(stage)) return;
        EnsureChild(root, stage);
        if (IsReparse(stage))
        {
            Directory.Delete(stage);
            return;
        }
        void DeleteTree(string path)
        {
            EnsureChild(root, path);
            foreach (var child in Directory.EnumerateFileSystemEntries(path))
            {
                EnsureChild(stage, child);
                if (IsReparse(child))
                {
                    if (Directory.Exists(child)) Directory.Delete(child);
                    else File.Delete(child);
                }
                else if (Directory.Exists(child)) DeleteTree(child);
                else File.Delete(child);
            }
            Directory.Delete(path);
        }
        DeleteTree(stage);
    }

    private static RunnerPackageException Unsafe(string message) =>
        new(RunnerPackageFailure.UnsafeArchive, message);
}
