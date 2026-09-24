using System.Text.Json;
using System.Text.RegularExpressions;

namespace Grl.Core;

public sealed class ContractException(string code, string message) : Exception(message)
{
    public string Code { get; } = code;
}

internal static partial class ContractJson
{
    private static readonly string[] SecretPrefixes =
        ["ghu_", "ghr_", "ghp_", "gho_", "ghs_", "github_pat_"];

    public static JsonDocument ParseStrict(string json)
    {
        JsonDocument document;
        try
        {
            document = JsonDocument.Parse(json, new JsonDocumentOptions
            {
                AllowTrailingCommas = false,
                CommentHandling = JsonCommentHandling.Disallow,
                MaxDepth = 32
            });
        }
        catch (JsonException exception)
        {
            throw new ContractException("INVALID_JSON", exception.Message);
        }

        try
        {
            CheckDuplicateKeys(document.RootElement);
            return document;
        }
        catch
        {
            document.Dispose();
            throw;
        }
    }

    public static void Object(JsonElement value, params string[] allowed)
    {
        if (value.ValueKind != JsonValueKind.Object)
            throw new ContractException("EXPECTED_OBJECT", "An object is required.");
        foreach (var property in value.EnumerateObject())
        {
            if (!allowed.Contains(property.Name, StringComparer.Ordinal))
                throw new ContractException("UNKNOWN_FIELD", $"Unknown field: {property.Name}");
        }
    }

    public static JsonElement Required(JsonElement parent, string name)
    {
        if (!parent.TryGetProperty(name, out var value))
            throw new ContractException("MISSING_FIELD", $"Missing field: {name}");
        return value;
    }

    public static string String(JsonElement value, string name)
    {
        if (value.ValueKind != JsonValueKind.String)
            throw new ContractException("INVALID_TYPE", $"{name} must be a string.");
        var result = value.GetString()!;
        if (result.Length == 0)
            throw new ContractException("INVALID_VALUE", $"{name} cannot be empty.");
        return result;
    }

    public static int Int(JsonElement value, string name, int minimum = 0)
    {
        if (value.ValueKind != JsonValueKind.Number || !value.TryGetInt32(out var result) || result < minimum)
            throw new ContractException("INVALID_NUMBER", $"{name} must be an integer >= {minimum}.");
        return result;
    }

    public static long Long(JsonElement value, string name, long minimum = 0)
    {
        if (value.ValueKind != JsonValueKind.Number || !value.TryGetInt64(out var result) || result < minimum)
            throw new ContractException("INVALID_NUMBER", $"{name} must be an integer >= {minimum}.");
        return result;
    }

    public static double FiniteNonnegative(JsonElement value, string name)
    {
        if (value.ValueKind != JsonValueKind.Number || !value.TryGetDouble(out var result) ||
            !double.IsFinite(result) || result < 0)
            throw new ContractException("INVALID_NUMBER", $"{name} must be a finite nonnegative number.");
        return result;
    }

    public static bool Bool(JsonElement value, string name)
    {
        if (value.ValueKind is not (JsonValueKind.True or JsonValueKind.False))
            throw new ContractException("INVALID_TYPE", $"{name} must be a boolean.");
        return value.GetBoolean();
    }

    public static void Exact(string actual, string expected, string name)
    {
        if (!string.Equals(actual, expected, StringComparison.Ordinal))
            throw new ContractException("INVALID_VALUE", $"Invalid {name}.");
    }

    public static void NoSecrets(JsonElement value)
    {
        switch (value.ValueKind)
        {
            case JsonValueKind.Object:
                foreach (var property in value.EnumerateObject())
                {
                    RejectSecret(property.Name);
                    NoSecrets(property.Value);
                }
                break;
            case JsonValueKind.Array:
                foreach (var child in value.EnumerateArray()) NoSecrets(child);
                break;
            case JsonValueKind.String:
                RejectSecret(value.GetString()!);
                break;
        }
    }

    private static void RejectSecret(string text)
    {
        if (SecretPrefixes.Any(prefix =>
            text.Contains(prefix, StringComparison.OrdinalIgnoreCase)))
            throw new ContractException("SECRET_SHAPED_VALUE", "A credential-shaped value is forbidden.");
    }

    private static void CheckDuplicateKeys(JsonElement value)
    {
        if (value.ValueKind == JsonValueKind.Object)
        {
            var names = new HashSet<string>(StringComparer.Ordinal);
            foreach (var property in value.EnumerateObject())
            {
                if (!names.Add(property.Name))
                    throw new ContractException("DUPLICATE_KEY", $"Duplicate JSON key: {property.Name}");
                CheckDuplicateKeys(property.Value);
            }
        }
        else if (value.ValueKind == JsonValueKind.Array)
        {
            foreach (var child in value.EnumerateArray()) CheckDuplicateKeys(child);
        }
    }
}
