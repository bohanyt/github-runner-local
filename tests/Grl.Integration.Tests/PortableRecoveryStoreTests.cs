using Grl.Integration;

namespace Grl.Integration.Tests;

public sealed class PortableRecoveryStoreTests
{
    [Fact]
    public void ReopenPreservesExactNonSecretIdentityAndPendingState()
    {
        using var root = new Root();
        using (var store = PortableRecoveryStore.Open(root.Path, "owner/exec", "runner-1"))
        {
            Assert.Null(store.Read());
            store.Write(null, PortableRunnerState.Configuring);
            store.Write(42, PortableRunnerState.Degraded, mayHaveUnownedProcess: true);
        }
        using var reopened = PortableRecoveryStore.Open(root.Path, "owner/exec", "runner-1");
        var evidence = Assert.IsType<PortableRecoveryEvidence>(reopened.Read());
        Assert.Equal("owner/exec", evidence.Repository);
        Assert.Equal("runner-1", evidence.RunnerName);
        Assert.Equal(root.Path, evidence.Root);
        Assert.Equal(42, evidence.RunnerId);
        Assert.Equal(PortableRunnerState.Degraded, evidence.State);
        Assert.True(evidence.MayHaveUnownedProcess);
        Assert.False(evidence.CanAttemptExactRemoteRemoval);
        Assert.DoesNotContain("SECRET", File.ReadAllText(System.IO.Path.Combine(root.Path, ".grl-recovery.json")));
    }

    [Fact]
    public void ReopenAllowsExactRemoteRemovalOnlyWhenNoProcessCouldHaveStarted()
    {
        using var root = new Root();
        using (var store = PortableRecoveryStore.Open(root.Path, "owner/exec", "runner-1"))
            store.Write(42, PortableRunnerState.Configured);
        using var reopened = PortableRecoveryStore.Open(root.Path, "owner/exec", "runner-1");
        Assert.True(reopened.Read()!.CanAttemptExactRemoteRemoval);
    }

    [Fact]
    public void WrongIdentityCorruptAndInterruptedWritesFailClosed()
    {
        using var root = new Root();
        using (var store = PortableRecoveryStore.Open(root.Path, "owner/exec", "runner-1"))
            store.Write(42, PortableRunnerState.RemoteRemovalPending);
        File.WriteAllText(System.IO.Path.Combine(root.Path, ".grl-recovery-interrupted.tmp"), "partial");
        using (var store = PortableRecoveryStore.Open(root.Path, "owner/exec", "runner-1"))
            Assert.Equal(PortableRunnerState.RemoteRemovalPending, store.Read()!.State);
        using (var wrong = PortableRecoveryStore.Open(root.Path, "owner/other", "runner-1"))
            Assert.Throws<InvalidOperationException>(() => wrong.Read());
        File.WriteAllText(System.IO.Path.Combine(root.Path, ".grl-recovery.json"), "{partial");
        using (var store = PortableRecoveryStore.Open(root.Path, "owner/exec", "runner-1"))
            Assert.Throws<InvalidOperationException>(() => store.Read());
    }

    [Fact]
    public void ConcurrentRecoveryLeaseAndRedirectedFileAreRejected()
    {
        using var root = new Root();
        using (var first = PortableRecoveryStore.Open(root.Path, "owner/exec", "runner-1"))
            Assert.Throws<IOException>(() => PortableRecoveryStore.Open(root.Path, "owner/exec", "runner-1"));
        if (OperatingSystem.IsWindows()) return; // Symlink creation needs optional developer privileges on Windows.
        var statePath = System.IO.Path.Combine(root.Path, ".grl-recovery.json");
        var target = System.IO.Path.Combine(root.Path, "target");
        File.WriteAllText(target, "{}");
        File.CreateSymbolicLink(statePath, target);
        Assert.Throws<InvalidOperationException>(() => PortableRecoveryStore.Open(root.Path, "owner/exec", "runner-1"));
    }

    private sealed class Root : IDisposable
    {
        public string Path { get; } = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "grl-recovery-" + Guid.NewGuid().ToString("N"));
        public Root() => Directory.CreateDirectory(Path);
        public void Dispose() => Directory.Delete(Path, recursive: true);
    }
}
