using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Grl.Core;

// TrustedDefinitionSha is the profile JSON blob SHA at the workflow commit,
// supplied by the later adapter rather than trusted from request text.
public sealed record ProfilePolicy(
    int MaximumTimeoutMinutes, int MinimumRequiredTests, string TrustedDefinitionSha);

public sealed record RequestValidationContext(
    IReadOnlySet<string> AllowedRepositories,
    IReadOnlyDictionary<string, ProfilePolicy> Profiles,
    DateTimeOffset CommentCreatedAt,
    IClock Clock);

public sealed record RequestContract(
    string RequestId, string Repository, string RequestedSha,
    string ProfileId, string DefinitionSha, DateTimeOffset ExpiresAt,
    int TimeoutMinutes, string FencedBodySha256);

public static partial class RequestEnvelope
{
    public const string Marker = "<!-- grl-request v1 -->";
    public const int MaximumBodyBytes = 4096;

    [GeneratedRegex(@"\A<!-- grl-request v1 -->\r?\n(?<fence>```json\r?\n(?<json>.*?)\r?\n```)[ \t\r\n]*\z", RegexOptions.Singleline)]
    private static partial Regex EnvelopePattern();

    [GeneratedRegex(@"\A[0-9a-f]{8}-[0-9a-f]{4}-4[0-9a-f]{3}-[89ab][0-9a-f]{3}-[0-9a-f]{12}\z")]
    internal static partial Regex UuidV4Pattern();

    [GeneratedRegex(@"\A[0-9a-f]{40}\z")]
    internal static partial Regex ShaPattern();

    [GeneratedRegex(@"\A[a-z0-9](?:[a-z0-9-]{0,37}[a-z0-9])?/[a-z0-9](?:[a-z0-9._-]{0,98}[a-z0-9])?\z")]
    internal static partial Regex RepositoryPattern();

    [GeneratedRegex(@"\A[a-z0-9](?:[a-z0-9-]{0,62}[a-z0-9])?\z")]
    internal static partial Regex ProfilePattern();

    public static RequestContract Parse(ReadOnlySpan<byte> bodyBytes, RequestValidationContext context)
    {
        if (bodyBytes.Length > MaximumBodyBytes)
            throw new ContractException("BODY_TOO_LARGE", "A request exceeds 4096 UTF-8 bytes.");
        if (bodyBytes.Length >= 3 && bodyBytes[0] == 0xEF &&
            bodyBytes[1] == 0xBB && bodyBytes[2] == 0xBF)
            throw new ContractException("BOM", "A UTF-8 BOM is forbidden.");
        string body;
        try { body = new UTF8Encoding(false, true).GetString(bodyBytes); }
        catch (DecoderFallbackException)
        {
            throw new ContractException("INVALID_UTF8", "Request bytes must be valid UTF-8.");
        }

        if (body.Split(new string((char)96, 3), StringSplitOptions.None).Length != 3)
            throw new ContractException("INVALID_ENVELOPE", "Exactly one JSON fence is required.");
        var envelope = EnvelopePattern().Match(body);
        if (!envelope.Success)
            throw new ContractException("INVALID_ENVELOPE", "One exact marker and one JSON fence are required.");
        var fencedBytes = Encoding.UTF8.GetBytes(envelope.Groups["fence"].Value);
        var digest = Convert.ToHexStringLower(SHA256.HashData(fencedBytes));
        using var document = ContractJson.ParseStrict(envelope.Groups["json"].Value);
        var root = document.RootElement;
        ContractJson.Object(root, "schema_version", "request_id", "target", "profile", "expires_at", "timeout_minutes");
        ContractJson.Exact(
            ContractJson.String(ContractJson.Required(root, "schema_version"), "schema_version"),
            "grl.request.v1", "schema_version");
        var requestId = ContractJson.String(ContractJson.Required(root, "request_id"), "request_id");
        if (!UuidV4Pattern().IsMatch(requestId))
            throw new ContractException("INVALID_REQUEST_ID", "request_id must be a lowercase UUIDv4.");

        var target = ContractJson.Required(root, "target");
        ContractJson.Object(target, "repository", "sha");
        var repository = ContractJson.String(ContractJson.Required(target, "repository"), "repository");
        if (!RepositoryPattern().IsMatch(repository) || repository.Contains("..", StringComparison.Ordinal) ||
            !context.AllowedRepositories.Contains(repository))
            throw new ContractException("REPOSITORY_NOT_ALLOWED", "target.repository is not canonical and allowlisted.");
        var sha = ContractJson.String(ContractJson.Required(target, "sha"), "sha");
        if (!ShaPattern().IsMatch(sha))
            throw new ContractException("INVALID_SHA", "target.sha must be 40 lowercase hexadecimal characters.");

        var profile = ContractJson.Required(root, "profile");
        ContractJson.Object(profile, "id", "definition_sha");
        var profileId = ContractJson.String(ContractJson.Required(profile, "id"), "profile.id");
        if (!ProfilePattern().IsMatch(profileId) || !context.Profiles.TryGetValue(profileId, out var policy))
            throw new ContractException("UNKNOWN_PROFILE", "profile.id must be allowlisted.");
        if (policy.MaximumTimeoutMinutes is < 1 or > 1440 ||
            policy.MinimumRequiredTests < 1 ||
            !ShaPattern().IsMatch(policy.TrustedDefinitionSha))
            throw new ContractException("INVALID_PROFILE_POLICY", "Profile policy must require tests and a positive timeout.");
        var definitionSha = ContractJson.String(ContractJson.Required(profile, "definition_sha"), "profile.definition_sha");
        if (!ShaPattern().IsMatch(definitionSha))
            throw new ContractException("INVALID_SHA", "profile.definition_sha must be 40 lowercase hexadecimal characters.");
        if (!string.Equals(definitionSha, policy.TrustedDefinitionSha, StringComparison.Ordinal))
            throw new ContractException("PROFILE_DEFINITION_MISMATCH", "Profile definition SHA differs from the trusted workflow blob.");

        var expiresText = ContractJson.String(ContractJson.Required(root, "expires_at"), "expires_at");
        if (!TryParseUtc(expiresText, out var expires))
            throw new ContractException("INVALID_EXPIRY", "expires_at must be RFC 3339 UTC with Z.");
        var created = context.CommentCreatedAt.ToUniversalTime();
        if (expires < created.AddMinutes(1) || expires > created.AddHours(24))
            throw new ContractException("EXPIRY_WINDOW", "Expiry must be 1 minute to 24 hours after comment creation.");
        if (context.Clock.UtcNow.ToUniversalTime() >= expires)
            throw new ContractException("EXPIRED", "The request has expired.");
        var timeout = ContractJson.Int(ContractJson.Required(root, "timeout_minutes"), "timeout_minutes", 1);
        if (timeout > policy.MaximumTimeoutMinutes)
            throw new ContractException("TIMEOUT_OVER_MAX", "The timeout exceeds the profile limit.");

        return new(requestId, repository, sha, profileId, definitionSha,
            expires, timeout, digest);
    }

    internal static bool TryParseUtc(string text, out DateTimeOffset timestamp)
    {
        timestamp = default;
        if (!text.EndsWith('Z')) return false;
        var formats = new[]
        {
            "yyyy-MM-dd'T'HH:mm:ss'Z'",
            "yyyy-MM-dd'T'HH:mm:ss.FFFFFFF'Z'"
        };
        return DateTimeOffset.TryParseExact(text, formats, CultureInfo.InvariantCulture,
            DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out timestamp);
    }
}
