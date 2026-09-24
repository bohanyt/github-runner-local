using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Nodes;
using Grl.Core;

namespace Grl.Core.Tests;

public sealed class RequestContractTests
{
    public const string RequestedSha = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";
    public const string DefinitionSha = "cccccccccccccccccccccccccccccccccccccccc";
    public const string RequestId = "b70dfe32-244d-4b79-a781-523a0b828447";
    public const string Repository = "bohanyt/grl-exec";
    public const string ProfileId = "smoke-fixture";
    private const string ValidJson = """
        {"schema_version":"grl.request.v1","request_id":"b70dfe32-244d-4b79-a781-523a0b828447","target":{"repository":"bohanyt/grl-exec","sha":"aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa"},"profile":{"id":"smoke-fixture","definition_sha":"cccccccccccccccccccccccccccccccccccccccc"},"expires_at":"2026-09-24T06:00:00Z","timeout_minutes":5}
        """;
    private static readonly DateTimeOffset Created =
        DateTimeOffset.Parse("2026-09-24T05:00:00Z");
    private static readonly string Fence = new((char)96, 3);

    public static RequestValidationContext Context(FakeClock? clock = null) =>
        new(new HashSet<string>(StringComparer.Ordinal) { Repository },
            new Dictionary<string, ProfilePolicy>(StringComparer.Ordinal)
            {
                [ProfileId] = new(10, 1, DefinitionSha)
            },
            Created, clock ?? new FakeClock(Created));

    public static string Body(string json) =>
        RequestEnvelope.Marker + "\n" + Fence + "json\n" + json + "\n" + Fence;

    public static RequestContract ValidRequest() =>
        RequestEnvelope.Parse(Encoding.UTF8.GetBytes(Body(ValidJson)), Context());

    private static string Mutate(Action<JsonObject> change)
    {
        var node = JsonNode.Parse(ValidJson)!.AsObject();
        change(node);
        return node.ToJsonString();
    }

    private static string Reject(string json) =>
        Assert.Throws<ContractException>(() =>
            RequestEnvelope.Parse(Encoding.UTF8.GetBytes(Body(json)), Context())).Code;

    [Fact]
    public void Valid_request_hashes_exact_fenced_bytes()
    {
        var request = ValidRequest();
        Assert.Equal(RequestId, request.RequestId);
        Assert.Equal(Repository, request.Repository);
        Assert.Equal(DefinitionSha, request.DefinitionSha);
        Assert.Equal(5, request.TimeoutMinutes);
        var fence = Fence + "json\n" + ValidJson + "\n" + Fence;
        Assert.Equal(Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(fence))),
            request.FencedBodySha256);
    }

    [Theory]
    [InlineData("command", "arbitrary-shell")]
    [InlineData("example_only", true)]
    [InlineData("machine_selector", "local-windows")]
    public void Extra_or_design_only_fields_are_rejected(string name, object value)
    {
        var json = Mutate(root => root[name] = JsonValue.Create(value));
        Assert.Equal("UNKNOWN_FIELD", Reject(json));
    }

    [Fact]
    public void Nested_extra_fields_are_rejected()
    {
        Assert.Equal("UNKNOWN_FIELD", Reject(Mutate(root =>
            root["target"]!.AsObject()["command"] = "echo")));
    }

    [Theory]
    [InlineData("abc123")]
    [InlineData("AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA")]
    public void Target_sha_must_be_full_lowercase(string sha)
    {
        Assert.Equal("INVALID_SHA", Reject(Mutate(root =>
            root["target"]!.AsObject()["sha"] = sha)));
    }

    [Theory]
    [InlineData("b70dfe32-244d-1b79-a781-523a0b828447")]
    [InlineData("B70DFE32-244D-4B79-A781-523A0B828447")]
    [InlineData("not-a-uuid")]
    public void Request_id_must_be_lowercase_UUIDv4(string id)
    {
        Assert.Equal("INVALID_REQUEST_ID", Reject(Mutate(root => root["request_id"] = id)));
    }

    [Fact]
    public void Duplicate_JSON_keys_are_rejected_at_any_depth()
    {
        var rootDuplicate = ValidJson.Replace(
            "\"timeout_minutes\":5", "\"timeout_minutes\":5,\"timeout_minutes\":5",
            StringComparison.Ordinal);
        var nestedDuplicate = ValidJson.Replace(
            "\"sha\":\"" + RequestedSha + "\"",
            "\"sha\":\"" + RequestedSha + "\",\"sha\":\"" + RequestedSha + "\"",
            StringComparison.Ordinal);
        Assert.Equal("DUPLICATE_KEY", Reject(rootDuplicate));
        Assert.Equal("DUPLICATE_KEY", Reject(nestedDuplicate));
    }

    [Fact]
    public void Size_BOM_and_invalid_UTF8_are_rejected_as_bytes()
    {
        var oversize = Encoding.UTF8.GetBytes(Body(ValidJson) + new string(' ', 4096));
        Assert.Equal("BODY_TOO_LARGE",
            Assert.Throws<ContractException>(() => RequestEnvelope.Parse(oversize, Context())).Code);
        var bom = new byte[] { 0xEF, 0xBB, 0xBF }
            .Concat(Encoding.UTF8.GetBytes(Body(ValidJson))).ToArray();
        Assert.Equal("BOM",
            Assert.Throws<ContractException>(() => RequestEnvelope.Parse(bom, Context())).Code);
        Assert.Equal("INVALID_UTF8",
            Assert.Throws<ContractException>(() =>
                RequestEnvelope.Parse([0xFF], Context())).Code);
    }

    [Theory]
    [InlineData("prefix")]
    [InlineData("<!-- grl-request v0 -->")]
    public void Outer_text_or_wrong_marker_is_rejected(string prefix)
    {
        var body = prefix + "\n" + Body(ValidJson);
        Assert.Equal("INVALID_ENVELOPE",
            Assert.Throws<ContractException>(() =>
                RequestEnvelope.Parse(Encoding.UTF8.GetBytes(body), Context())).Code);
    }

    [Fact]
    public void Two_fences_are_rejected()
    {
        var body = Body(ValidJson) + "\n" + Fence + "json\n{}\n" + Fence;
        Assert.Equal("INVALID_ENVELOPE",
            Assert.Throws<ContractException>(() =>
                RequestEnvelope.Parse(Encoding.UTF8.GetBytes(body), Context())).Code);
    }

    [Theory]
    [InlineData("2026-09-24T04:59:00Z", "EXPIRY_WINDOW")]
    [InlineData("2026-09-25T05:00:01Z", "EXPIRY_WINDOW")]
    [InlineData("2026-09-24T06:00:00+00:00", "INVALID_EXPIRY")]
    [InlineData("2026-09-24T06:00:00", "INVALID_EXPIRY")]
    [InlineData("2026-09-24T06:00:00z", "INVALID_EXPIRY")]
    public void Expiry_has_strict_UTC_and_creation_window(string expiry, string code)
    {
        Assert.Equal(code, Reject(Mutate(root => root["expires_at"] = expiry)));
    }

    [Fact]
    public void Expired_at_observation_is_rejected()
    {
        var clock = new FakeClock(DateTimeOffset.Parse("2026-09-24T06:00:00Z"));
        Assert.Equal("EXPIRED",
            Assert.Throws<ContractException>(() => RequestEnvelope.Parse(
                Encoding.UTF8.GetBytes(Body(ValidJson)), Context(clock))).Code);
    }

    [Theory]
    [InlineData(0, "INVALID_NUMBER")]
    [InlineData(11, "TIMEOUT_OVER_MAX")]
    public void Timeout_is_positive_and_profile_bounded(int minutes, string code)
    {
        Assert.Equal(code, Reject(Mutate(root => root["timeout_minutes"] = minutes)));
    }

    [Fact]
    public void Unknown_profile_and_non_allowlisted_repository_are_rejected()
    {
        Assert.Equal("UNKNOWN_PROFILE", Reject(Mutate(root =>
            root["profile"]!.AsObject()["id"] = "arbitrary")));
        Assert.Equal("REPOSITORY_NOT_ALLOWED", Reject(Mutate(root =>
            root["target"]!.AsObject()["repository"] = "bohanyt/other")));
    }

    [Fact]
    public void Profile_definition_must_match_trusted_workflow_blob()
    {
        Assert.Equal("PROFILE_DEFINITION_MISMATCH", Reject(Mutate(root =>
            root["profile"]!.AsObject()["definition_sha"] = new string('d', 40))));
    }

    [Fact]
    public void Homoglyph_repository_is_not_canonical_ASCII()
    {
        Assert.Equal("REPOSITORY_NOT_ALLOWED", Reject(Mutate(root =>
            root["target"]!.AsObject()["repository"] = "bоhanyt/grl-exec")));
    }

    [Fact]
    public void JSON_boolean_and_non_object_are_not_accepted_as_request()
    {
        Assert.Throws<ContractException>(() => RequestEnvelope.Parse(
            Encoding.UTF8.GetBytes(Body("[]")), Context()));
        Assert.Equal("INVALID_NUMBER", Reject(Mutate(root => root["timeout_minutes"] = true)));
    }
}
