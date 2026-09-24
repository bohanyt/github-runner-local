namespace Grl.Core;

public enum DiskAdmissionReason
{
    Admitted, InvalidInput, BelowReserve, BelowRequiredFreeSpace
}

public sealed record DiskAdmissionResult(
    bool Admitted, DiskAdmissionReason Reason,
    long FreeBytes, long ReserveGiB, long EstimatedJobGiB, long RequiredBytes);

public static class DiskAdmission
{
    public const long BytesPerGiB = 1024L * 1024 * 1024;

    public static DiskAdmissionResult Evaluate(
        long freeBytes, long reserveGiB, long estimatedJobGiB = 0)
    {
        if (freeBytes < 0 || reserveGiB < 0 || estimatedJobGiB < 0 ||
            reserveGiB > long.MaxValue / BytesPerGiB - estimatedJobGiB)
            return new(false, DiskAdmissionReason.InvalidInput,
                freeBytes, reserveGiB, estimatedJobGiB, 0);

        var reserveBytes = reserveGiB * BytesPerGiB;
        var requiredBytes = (reserveGiB + estimatedJobGiB) * BytesPerGiB;
        var reason = freeBytes < reserveBytes
            ? DiskAdmissionReason.BelowReserve
            : freeBytes < requiredBytes
                ? DiskAdmissionReason.BelowRequiredFreeSpace
                : DiskAdmissionReason.Admitted;
        return new(reason == DiskAdmissionReason.Admitted, reason,
            freeBytes, reserveGiB, estimatedJobGiB, requiredBytes);
    }
}
