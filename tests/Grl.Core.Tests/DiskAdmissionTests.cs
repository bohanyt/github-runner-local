using Grl.Core;

namespace Grl.Core.Tests;

public sealed class DiskAdmissionTests
{
    [Theory]
    [InlineData(14, false, DiskAdmissionReason.BelowReserve)]
    [InlineData(15, true, DiskAdmissionReason.Admitted)]
    [InlineData(16, true, DiskAdmissionReason.Admitted)]
    public void Reserve_boundary_is_in_explicit_GiB(
        long freeGiB, bool admitted, DiskAdmissionReason reason)
    {
        var result = DiskAdmission.Evaluate(freeGiB * DiskAdmission.BytesPerGiB, 15);
        Assert.Equal(admitted, result.Admitted);
        Assert.Equal(reason, result.Reason);
        Assert.Equal(15 * DiskAdmission.BytesPerGiB, result.RequiredBytes);
    }

    [Fact]
    public void Job_estimate_is_included_without_changing_reserve_reason()
    {
        var result = DiskAdmission.Evaluate(16 * DiskAdmission.BytesPerGiB, 15, 2);
        Assert.False(result.Admitted);
        Assert.Equal(DiskAdmissionReason.BelowRequiredFreeSpace, result.Reason);
        Assert.Equal(17 * DiskAdmission.BytesPerGiB, result.RequiredBytes);
    }

    [Theory]
    [InlineData(-1, 15, 0)]
    [InlineData(0, -1, 0)]
    [InlineData(0, 0, -1)]
    [InlineData(0, long.MaxValue, long.MaxValue)]
    public void Invalid_input_has_stable_reason(long freeBytes, long reserve, long estimate)
    {
        Assert.Equal(DiskAdmissionReason.InvalidInput,
            DiskAdmission.Evaluate(freeBytes, reserve, estimate).Reason);
    }
}
