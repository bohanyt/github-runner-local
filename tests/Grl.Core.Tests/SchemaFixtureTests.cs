using System.Text.Json;
using Grl.Core;

namespace Grl.Core.Tests;

public sealed class SchemaFixtureTests
{
    private static string FixturePath(string name) =>
        Path.Combine(AppContext.BaseDirectory, "fixtures", "contracts", name);

    private static string SchemaPath(string name) =>
        Path.Combine(AppContext.BaseDirectory, "schemas", name);

    [Fact]
    public void Shipped_request_result_and_settings_fixtures_parse_as_contracts()
    {
        var requestBytes = Convert.FromBase64String(
            File.ReadAllText(FixturePath("request.valid.base64")).Trim());
        var request = RequestEnvelope.Parse(
            requestBytes, RequestContractTests.Context());
        Assert.Equal("53bb85435444d602d99d188c586c120257d903cf0a88fc3474a3ff44254a8dda",
            request.FencedBodySha256);
        var result = ResultValidator.Parse(
            File.ReadAllText(FixturePath("result.valid.json")), request,
            new ProfilePolicy(10, 1, RequestContractTests.DefinitionSha),
            RequestContractTests.Repository, 123);
        Assert.Equal(ExecutionStatus.PASS, result.Status);
        Assert.Equal(15, SettingsV1.Parse(
            File.ReadAllText(FixturePath("settings.valid.json"))).DiskReserveGiB);
    }

    [Fact]
    public void Earlier_design_example_stays_rejected()
    {
        var json = File.ReadAllText(FixturePath("request.design-example-rejected.json"));
        Assert.Throws<ContractException>(() => SettingsV1.Parse(json));
        Assert.Throws<ContractException>(() => RequestEnvelope.Parse(
            System.Text.Encoding.UTF8.GetBytes(RequestContractTests.Body(json)),
            RequestContractTests.Context()));
    }

    [Theory]
    [InlineData("grl.request.v1.schema.json")]
    [InlineData("grl.result.v1.schema.json")]
    [InlineData("grl.settings.v1.schema.json")]
    public void Published_schemas_are_strict_JSON_Schema_documents(string name)
    {
        using var schema = JsonDocument.Parse(File.ReadAllText(SchemaPath(name)));
        var root = schema.RootElement;
        Assert.Equal("https://json-schema.org/draft/2020-12/schema",
            root.GetProperty("$schema").GetString());
        Assert.Equal(JsonValueKind.False, root.GetProperty("additionalProperties").ValueKind);
        Assert.NotEmpty(root.GetProperty("required").EnumerateArray());
        Assert.Equal(JsonValueKind.Object, root.GetProperty("properties").ValueKind);
    }
}
