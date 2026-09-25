using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using Grl.Integration;

namespace Grl.Integration.Tests;

public sealed class RunnerPackageTests
{
    [Fact]
    public async Task ValidHashExtractsAndMovesOnlyAfterVerification()
    {
        using var temp = new TempRoot();
        var zip = Zip(("bin/Runner.Listener.exe", "listener"), ("config.cmd", "help"));
        var result = await Install(zip, Hash(zip), temp.Path);
        Assert.Equal(Hash(zip), result.Sha256);
        Assert.Equal(zip.Length, result.DownloadBytes);
        Assert.Equal("listener", File.ReadAllText(Path.Combine(result.FinalRoot, "bin", "Runner.Listener.exe")));
        Assert.Empty(Directory.GetDirectories(temp.Path, ".grl-stage-*"));
    }

    [Fact]
    public async Task WrongHashNeverExtractsOrCreatesFinalRoot()
    {
        using var temp = new TempRoot();
        var zip = Zip(("safe.txt", "content"));
        var error = await Assert.ThrowsAsync<RunnerPackageException>(
            () => Install(zip, new string('0', 64), temp.Path));
        Assert.Equal(RunnerPackageFailure.HashMismatch, error.Failure);
        Assert.False(Path.Exists(Path.Combine(temp.Path, "runner")));
        Assert.Empty(Directory.GetFileSystemEntries(temp.Path));
    }

    [Theory]
    [InlineData("/rooted.txt")]
    [InlineData("C:/rooted.txt")]
    [InlineData("../escape.txt")]
    [InlineData("a/../../escape.txt")]
    [InlineData("a:stream")]
    [InlineData("CON.txt")]
    [InlineData("a/COM1.log")]
    [InlineData("a/evil. ")]
    [InlineData("//server/share")]
    [InlineData(@"\\?\C:\device.txt")]
    public async Task UnsafeWindowsEntryIsRejectedAndCleaned(string name)
    {
        using var temp = new TempRoot();
        var zip = Zip(("good.txt", "good"), (name, "bad"));
        var error = await Assert.ThrowsAsync<RunnerPackageException>(() => Install(zip, Hash(zip), temp.Path));
        Assert.Equal(RunnerPackageFailure.UnsafeArchive, error.Failure);
        Assert.False(Path.Exists(Path.Combine(temp.Path, "runner")));
        Assert.Empty(Directory.GetFileSystemEntries(temp.Path));
    }

    [Theory]
    [InlineData("COM\u00B9")]
    [InlineData("COM\u00B2")]
    [InlineData("COM\u00B3")]
    [InlineData("LPT\u00B9")]
    [InlineData("LPT\u00B2")]
    [InlineData("LPT\u00B3")]
    [InlineData("COM\u00B9.txt")]
    [InlineData("COM\u00B2.txt")]
    [InlineData("COM\u00B3.txt")]
    [InlineData("LPT\u00B9.log")]
    [InlineData("LPT\u00B2.log")]
    [InlineData("LPT\u00B3.log")]
    [InlineData("nested/COM\u00B9/child.txt")]
    [InlineData("nested/COM\u00B2/child.txt")]
    [InlineData("nested/COM\u00B3/child.txt")]
    [InlineData("nested/LPT\u00B9/child.txt")]
    [InlineData("nested/LPT\u00B2/child.txt")]
    [InlineData("nested/LPT\u00B3/child.txt")]
    public async Task SuperscriptDeviceAliasIsRejectedWithoutEscapingOwnedRoot(string name)
    {
        using var temp = new TempRoot();
        var ownedRoot = Directory.CreateDirectory(Path.Combine(temp.Path, "owned")).FullName;
        var outside = Path.Combine(temp.Path, "outside.txt");
        File.WriteAllText(outside, "keep");
        var zip = Zip(("good.txt", "good"), (name, "bad"));

        var error = await Assert.ThrowsAsync<RunnerPackageException>(() => Install(zip, Hash(zip), ownedRoot));

        Assert.Equal(RunnerPackageFailure.UnsafeArchive, error.Failure);
        Assert.False(Path.Exists(Path.Combine(ownedRoot, "runner")));
        Assert.Empty(Directory.GetFileSystemEntries(ownedRoot));
        Assert.Equal("keep", File.ReadAllText(outside));
    }

    [Theory]
    [InlineData("SOM\u00B9.txt")]
    [InlineData("COM\u2074.txt")]
    [InlineData("LPT\u00E9.txt")]
    public async Task UnrelatedUnicodeNamesRemainAllowed(string name)
    {
        using var temp = new TempRoot();
        var zip = Zip((name, "allowed"));

        var result = await Install(zip, Hash(zip), temp.Path);

        Assert.Equal("allowed", File.ReadAllText(Path.Combine(result.FinalRoot, name)));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task SymlinkAndReparseEntriesAreRejected(bool unixSymlink)
    {
        using var temp = new TempRoot();
        var zip = ZipWithAttributes(unixSymlink ? unchecked((int)0xA0000000) : (int)FileAttributes.ReparsePoint);
        var error = await Assert.ThrowsAsync<RunnerPackageException>(() => Install(zip, Hash(zip), temp.Path));
        Assert.Equal(RunnerPackageFailure.UnsafeArchive, error.Failure);
        Assert.Empty(Directory.GetFileSystemEntries(temp.Path));
    }

    [Fact]
    public async Task DownloadSizeLimitRejectsAndCleans()
    {
        using var temp = new TempRoot();
        var zip = Zip(("large.txt", new string('x', 1000)));
        var limits = new RunnerPackageLimits(zip.Length - 1, 1000, 1000, 10);
        var error = await Assert.ThrowsAsync<RunnerPackageException>(
            () => Install(zip, Hash(zip), temp.Path, limits));
        Assert.Equal(RunnerPackageFailure.SizeLimit, error.Failure);
        Assert.Empty(Directory.GetFileSystemEntries(temp.Path));
    }

    [Theory]
    [InlineData(4, 100, 10)]
    [InlineData(100, 4, 10)]
    [InlineData(100, 100, 0)]
    public async Task EntryTotalAndCountLimitsReject(int entry, int total, int count)
    {
        using var temp = new TempRoot();
        var zip = Zip(("one.txt", "12345"), ("two.txt", "12345"));
        var limits = new RunnerPackageLimits(10000, entry, total, count);
        var error = await Assert.ThrowsAsync<RunnerPackageException>(
            () => Install(zip, Hash(zip), temp.Path, limits));
        Assert.Equal(RunnerPackageFailure.SizeLimit, error.Failure);
        Assert.Empty(Directory.GetFileSystemEntries(temp.Path));
    }

    [Fact]
    public async Task ExistingFinalRootIsNeverReplaced()
    {
        using var temp = new TempRoot();
        var final = Directory.CreateDirectory(Path.Combine(temp.Path, "runner"));
        File.WriteAllText(Path.Combine(final.FullName, "keep.txt"), "keep");
        var zip = Zip(("new.txt", "new"));
        var error = await Assert.ThrowsAsync<RunnerPackageException>(() => Install(zip, Hash(zip), temp.Path));
        Assert.Equal(RunnerPackageFailure.ExistingRoot, error.Failure);
        Assert.Equal("keep", File.ReadAllText(Path.Combine(final.FullName, "keep.txt")));
        Assert.False(File.Exists(Path.Combine(final.FullName, "new.txt")));
    }

    [Fact]
    public void ExactOfficialUrlAndReviewedMetadataAreRequired()
    {
        var good = $$"""
            {"version":"2.337.0","asset":"actions-runner-win-x64-2.337.0.zip","url":"https://github.com/actions/runner/releases/download/v2.337.0/actions-runner-win-x64-2.337.0.zip","sha256":"{{RunnerPin.ReviewedSha256}}"}
            """;
        Assert.Equal(RunnerPin.ReviewedSha256, RunnerPin.Parse(good).Sha256);
        Assert.Equal(RunnerPackageFailure.InvalidPin,
            Assert.Throws<RunnerPackageException>(() => RunnerPin.Parse(good.Replace("github.com", "example.com"))).Failure);
    }

    private static Task<RunnerInstallResult> Install(
        byte[] zip, string hash, string root, RunnerPackageLimits? limits = null) =>
        new RunnerPackageInstaller(limits).InstallFromStreamAsync(new MemoryStream(zip), hash, root, "runner", default);

    private static byte[] Zip(params (string Name, string Content)[] entries)
    {
        using var memory = new MemoryStream();
        using (var archive = new ZipArchive(memory, ZipArchiveMode.Create, true))
            foreach (var (name, content) in entries)
            {
                var entry = archive.CreateEntry(name);
                using var writer = new StreamWriter(entry.Open(), Encoding.UTF8);
                writer.Write(content);
            }
        return memory.ToArray();
    }

    private static byte[] ZipWithAttributes(int attributes)
    {
        using var memory = new MemoryStream();
        using (var archive = new ZipArchive(memory, ZipArchiveMode.Create, true))
        {
            var entry = archive.CreateEntry("link");
            entry.ExternalAttributes = attributes;
            using var writer = new StreamWriter(entry.Open());
            writer.Write("target");
        }
        return memory.ToArray();
    }

    private static string Hash(byte[] bytes) => Convert.ToHexStringLower(SHA256.HashData(bytes));

    private sealed class TempRoot : IDisposable
    {
        public string Path { get; } = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "grl-c-test-" + Guid.NewGuid().ToString("N"));
        public TempRoot() => Directory.CreateDirectory(Path);
        public void Dispose() => Directory.Delete(Path, true);
    }
}
