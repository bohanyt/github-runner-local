using Grl.Core;

namespace Grl.Core.Tests;

public sealed class FakeClock(DateTimeOffset now) : IClock
{
    public DateTimeOffset UtcNow { get; set; } = now;
}

public sealed class WizardAndSettingsTests
{
    private static readonly DateTimeOffset FixedTime =
        DateTimeOffset.Parse("2026-09-24T05:00:00Z");

    public static IEnumerable<object[]> EveryTransition() =>
        WizardStateMachine.Transitions.Select(row => new object[] { row });

    [Theory]
    [MemberData(nameof(EveryTransition))]
    public void Every_supported_transition_is_deterministic(WizardTransition row)
    {
        var clock = new FakeClock(FixedTime);
        var machine = new WizardStateMachine(clock, row.From);
        var entry = machine.Apply(row.Event);
        Assert.Equal(row.To, machine.State);
        Assert.Equal(row.From, entry.From);
        Assert.Equal(row.To, entry.To);
        Assert.Equal(row.Compensation, entry.Compensation);
        Assert.Equal(FixedTime, entry.AtUtc);
    }

    [Theory]
    [InlineData(WizardState.PreflightRunning, WizardEvent.JobStarted)]
    [InlineData(WizardState.SignInAwaitingCode, WizardEvent.Configured)]
    [InlineData(WizardState.ScopeSelecting, WizardEvent.SignedIn)]
    [InlineData(WizardState.LocationLocal, WizardEvent.Downloaded)]
    [InlineData(WizardState.InstallingDownloading, WizardEvent.Verified)]
    [InlineData(WizardState.RunnerPaused, WizardEvent.JobStarted)]
    [InlineData(WizardState.DisconnectDone, WizardEvent.Disconnect)]
    public void Invalid_transition_does_not_change_state(WizardState initial, WizardEvent action)
    {
        var machine = new WizardStateMachine(new FakeClock(FixedTime), initial);
        var error = Assert.Throws<ContractException>(() => machine.Apply(action));
        Assert.Equal("INVALID_TRANSITION", error.Code);
        Assert.Equal(initial, machine.State);
    }

    [Theory]
    [InlineData(WizardState.PreflightRunning, WizardState.PreflightBlocked, CompensationAction.None)]
    [InlineData(WizardState.SignInAwaitingCode, WizardState.SignInCancelled, CompensationAction.DiscardDeviceCode)]
    [InlineData(WizardState.SignInPolling, WizardState.SignInCancelled, CompensationAction.DiscardDeviceCode)]
    [InlineData(WizardState.InstallingDownloading, WizardState.LocationLocal, CompensationAction.RemoveStaging)]
    [InlineData(WizardState.InstallingVerifying, WizardState.LocationLocal, CompensationAction.RemoveStaging)]
    [InlineData(WizardState.InstallingExtracting, WizardState.LocationLocal, CompensationAction.RemoveStaging)]
    [InlineData(WizardState.InstallingConfiguring, WizardState.LocationLocal, CompensationAction.RemoveStaging)]
    [InlineData(WizardState.RunnerBusy, WizardState.RunnerDraining, CompensationAction.CancelJob)]
    [InlineData(WizardState.RunnerDraining, WizardState.RunnerPaused, CompensationAction.CancelJob)]
    [InlineData(WizardState.DisconnectPending, WizardState.DisconnectRemotePending, CompensationAction.PreserveRemoteRemoval)]
    [InlineData(WizardState.DisconnectRemotePending, WizardState.DisconnectRemotePending, CompensationAction.PreserveRemoteRemoval)]
    public void Every_async_state_has_explicit_cancel_target(
        WizardState initial, WizardState target, CompensationAction compensation)
    {
        var entry = new WizardStateMachine(new FakeClock(FixedTime), initial)
            .Apply(WizardEvent.Cancel);
        Assert.Equal(target, entry.To);
        Assert.Equal(compensation, entry.Compensation);
    }

    [Fact]
    public void Fake_clock_is_used_at_transition_time_and_journal_has_no_free_text()
    {
        var clock = new FakeClock(FixedTime);
        var machine = new WizardStateMachine(clock);
        var first = machine.Apply(WizardEvent.Passed);
        clock.UtcNow = FixedTime.AddMinutes(7);
        var second = machine.Apply(WizardEvent.Continue);
        Assert.Equal(FixedTime, first.AtUtc);
        Assert.Equal(FixedTime.AddMinutes(7), second.AtUtc);
        Assert.Equal(5, typeof(WizardJournalEntry).GetProperties().Length);
        Assert.All(typeof(WizardJournalEntry).GetProperties(),
            property => Assert.NotEqual(typeof(string), property.PropertyType));
    }

    [Fact]
    public void Settings_defaults_reserve_and_round_trips()
    {
        var settings = SettingsV1.Parse("""{"schema_version":"grl.settings.v1","mode":"portable"}""");
        Assert.Equal(15, settings.DiskReserveGiB);
        Assert.Equal(settings, SettingsV1.Parse(settings.ToJson()));
    }

    [Theory]
    [InlineData("""{"schema_version":"grl.settings.v1","mode":"portable","disk_reserve_gib":20}""", 20)]
    [InlineData("""{"schema_version":"grl.settings.v1","mode":"portable","disk_reserve_gib":1}""", 1)]
    public void Settings_accepts_valid_reserve(string json, int expected)
    {
        Assert.Equal(expected, SettingsV1.Parse(json).DiskReserveGiB);
    }

    [Theory]
    [InlineData("""{"schema_version":"grl.settings.v1","mode":"portable","token":"x"}""")]
    [InlineData("""{"schema_version":"grl.settings.v1","mode":"service"}""")]
    [InlineData("""{"mode":"portable"}""")]
    [InlineData("""{"schema_version":"grl.settings.v1"}""")]
    [InlineData("""{"schema_version":"grl.settings.v1","mode":"portable","disk_reserve_gib":0}""")]
    [InlineData("""{"schema_version":"grl.settings.v1","mode":"portable","disk_reserve_gib":-1}""")]
    [InlineData("""{"schema_version":"grl.settings.v1","mode":"portable","disk_reserve_gib":1.5}""")]
    [InlineData("""{"schema_version":"grl.settings.v1","mode":"portable","disk_reserve_gib":true}""")]
    [InlineData("""{"schema_version":"grl.settings.v1","mode":"portable","disk_reserve_gib":1000001}""")]
    [InlineData("""{"schema_version":"grl.settings.v1","mode":"portable","mode":"portable"}""")]
    [InlineData("""{"schema_version":"grl.settings.v1","mode":"portable","note":"ghu_secret"}""")]
    [InlineData("""{"schema_version":"grl.settings.v1","mode":"portable","note":"github_pat_secret"}""")]
    public void Settings_rejects_unknown_invalid_duplicate_or_secret_values(string json)
    {
        Assert.Throws<ContractException>(() => SettingsV1.Parse(json));
    }

    [Theory]
    [InlineData("ghr_")]
    [InlineData("ghp_")]
    [InlineData("gho_")]
    [InlineData("ghs_")]
    public void Settings_rejects_each_secret_prefix(string prefix)
    {
        var json = "{\"schema_version\":\"grl.settings.v1\",\"mode\":\"portable\",\"note\":\"" +
            prefix + "abc\"}";
        Assert.Equal("SECRET_SHAPED_VALUE",
            Assert.Throws<ContractException>(() => SettingsV1.Parse(json)).Code);
    }
}
