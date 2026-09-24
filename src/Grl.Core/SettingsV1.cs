using System.Text.Json;

namespace Grl.Core;

public sealed record SettingsV1(string Mode, int DiskReserveGiB)
{
    public const int DefaultDiskReserveGiB = 15;
    public const int MaximumDiskReserveGiB = 1_000_000;
    public const string SchemaVersion = "grl.settings.v1";

    public static SettingsV1 Parse(string json)
    {
        using var document = ContractJson.ParseStrict(json);
        var root = document.RootElement;
        ContractJson.NoSecrets(root);
        ContractJson.Object(root, "schema_version", "mode", "disk_reserve_gib");
        ContractJson.Exact(
            ContractJson.String(ContractJson.Required(root, "schema_version"), "schema_version"),
            SchemaVersion, "schema_version");
        var mode = ContractJson.String(ContractJson.Required(root, "mode"), "mode");
        ContractJson.Exact(mode, "portable", "mode");
        var reserve = root.TryGetProperty("disk_reserve_gib", out var value)
            ? ContractJson.Int(value, "disk_reserve_gib", 1)
            : DefaultDiskReserveGiB;
        if (reserve > MaximumDiskReserveGiB)
            throw new ContractException("INVALID_RESERVE", "disk_reserve_gib exceeds its bound.");
        return new SettingsV1(mode, reserve);
    }

    public string ToJson() => JsonSerializer.Serialize(new
    {
        schema_version = SchemaVersion,
        mode = Mode,
        disk_reserve_gib = DiskReserveGiB
    });
}
