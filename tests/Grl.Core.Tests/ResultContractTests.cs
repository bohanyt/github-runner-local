using System.Text;
using System.Text.Json.Nodes;
using Grl.Core;

namespace Grl.Core.Tests;

public sealed class ResultContractTests
{
    private const string ResultTemplate = """
        {
          "schema_version":"grl.result.v1",
          "request_id":"b70dfe32-244d-4b79-a781-523a0b828447",
          "request_comment_id":123,
          "request_body_sha256":"DIGEST",
          "execution_repo":"bohanyt/grl-exec",
          "run":{"id":456,"attempt":1,"url":"https://github.com/bohanyt/grl-exec/actions/runs/456"},
          "workflow_sha":"bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb",
          "profile":{"id":"smoke-fixture","definition_sha":"cccccccccccccccccccccccccccccccccccccccc"},
          "target":{
            "repository":"bohanyt/grl-exec",
            "requested_sha":"aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa",
            "tested_sha":"aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa",
            "containing_branch":"main",
            "branch_head_at_admission":"aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa"
          },
          "runner":{"name":"example-runner","version":"2.337.0","os":"Windows","arch":"X64","identity_class":"portable-user","elevated":false},
          "timing":{"admitted_at":"2026-09-24T05:01:00Z","started_at":"2026-09-24T05:02:00Z","finished_at":"2026-09-24T05:03:00Z","phases":[{"name":"test","duration_s":60}]},
          "checks":[{"name":"fixture","exit_code":0,"duration_s":60,"tests":{"passed":3,"failed":0,"skipped":0,"errored":0,"source":"junit"}}],
          "execution_status":"PASS",
          "disk":{"free_before_gib":100,"free_after_gib":99,"reserve_gib":15},
          "artifacts":[],
          "logs":{"url":"https://github.com/bohanyt/grl-exec/actions/runs/456"},
          "report":{"attempts":1,"status_posted":true,"comment_posted":true}
        }
        """;

    private static readonly ProfilePolicy Profile =
        new(10, 1, RequestContractTests.DefinitionSha);

    private static string ValidJson(RequestContract request) =>
        ResultTemplate.Replace("DIGEST", request.FencedBodySha256, StringComparison.Ordinal);

    private static JsonObject Mutate(RequestContract request, Action<JsonObject> change)
    {
        var root = JsonNode.Parse(ValidJson(request))!.AsObject();
        change(root);
        return root;
    }

    private static ResultContract Parse(string json, RequestContract request) =>
        ResultValidator.Parse(json, request, Profile, RequestContractTests.Repository, 123);

    private static string Reject(RequestContract request, Action<JsonObject> change)
    {
        var json = Mutate(request, change).ToJsonString();
        return Assert.Throws<ContractException>(() => Parse(json, request)).Code;
    }

    private static ResultObservation CompleteObservation(RequestContract request) =>
        new(true, true, true, "github-actions[bot]", "github-actions[bot]",
            "success", request.RequestId, 123,
            request.Repository, request.RequestedSha, 456, 1, request.FencedBodySha256);

    [Fact]
    public void Valid_PASS_has_separate_workflow_and_definition_SHA_and_observed_verdict()
    {
        var request = RequestContractTests.ValidRequest();
        var result = Parse(ValidJson(request), request);
        Assert.NotEqual(result.WorkflowSha, result.DefinitionSha);
        Assert.Equal(result.RequestedSha, result.TestedSha);
        var verdict = ResultValidator.Observe(result, CompleteObservation(request));
        Assert.True(verdict.AcceptedPass);
        Assert.Equal(ReportingCompleteness.Complete, verdict.Reporting);
    }

    [Fact]
    public void Result_envelope_has_one_marker_and_one_fence_with_optional_human_line()
    {
        var request = RequestContractTests.ValidRequest();
        var fence = new string((char)96, 3);
        var body = ResultEnvelope.Marker + "\n" + fence + "json\n" +
            ValidJson(request) + "\n" + fence + "\nFixture result";
        var parsed = ResultEnvelope.Parse(Encoding.UTF8.GetBytes(body), request,
            Profile, RequestContractTests.Repository, 123);
        Assert.Equal(ExecutionStatus.PASS, parsed.Status);
        var two = body + "\n" + fence + "json\n{}\n" + fence;
        Assert.Equal("INVALID_ENVELOPE",
            Assert.Throws<ContractException>(() => ResultEnvelope.Parse(
                Encoding.UTF8.GetBytes(two), request, Profile,
                RequestContractTests.Repository, 123)).Code);
    }

    [Fact]
    public void Workflow_and_definition_SHA_can_coincidentally_match_but_are_distinct_fields()
    {
        var request = RequestContractTests.ValidRequest();
        var root = Mutate(request, result =>
            result["workflow_sha"] = RequestContractTests.DefinitionSha);
        var parsed = Parse(root.ToJsonString(), request);
        Assert.Equal(parsed.WorkflowSha, parsed.DefinitionSha);
        root.Remove("workflow_sha");
        Assert.Equal("MISSING_FIELD",
            Assert.Throws<ContractException>(() => Parse(root.ToJsonString(), request)).Code);
    }

    [Fact]
    public void PASS_requires_profile_minimum_tests_and_nonempty_checks()
    {
        var request = RequestContractTests.ValidRequest();
        Assert.Equal("INVALID_PASS", Reject(request, root =>
            root["checks"]![0]!["tests"]!["passed"] = 0));
        Assert.Equal("INVALID_PASS", Reject(request, root =>
            root["checks"] = new JsonArray()));
    }

    [Theory]
    [InlineData("failed")]
    [InlineData("errored")]
    public void PASS_rejects_failures_and_errors(string field)
    {
        var request = RequestContractTests.ValidRequest();
        Assert.Equal("INVALID_PASS", Reject(request, root =>
            root["checks"]![0]!["tests"]![field] = 1));
    }

    [Fact]
    public void PASS_rejects_any_relevant_nonzero_exit()
    {
        var request = RequestContractTests.ValidRequest();
        Assert.Equal("INVALID_PASS", Reject(request, root =>
            root["checks"]![0]!["exit_code"] = 1));
    }

    [Fact]
    public void Tested_SHA_must_equal_requested_SHA()
    {
        var request = RequestContractTests.ValidRequest();
        Assert.Equal("INVALID_VALUE", Reject(request, root =>
            root["target"]!["tested_sha"] = new string('d', 40)));
    }

    [Fact]
    public void Unknown_status_or_elevated_runner_cannot_be_accepted_PASS()
    {
        var request = RequestContractTests.ValidRequest();
        Assert.Equal("UNKNOWN_STATUS", Reject(request, root =>
            root["execution_status"] = "SUCCESS"));
        Assert.Equal("UNKNOWN_STATUS", Reject(request, root =>
            root["execution_status"] = "0"));
        Assert.Equal("INVALID_PASS", Reject(request, root =>
            root["runner"]!["elevated"] = true));
    }

    [Fact]
    public void Non_PASS_execution_is_recorded_but_never_accepted_PASS()
    {
        var request = RequestContractTests.ValidRequest();
        var result = Parse(Mutate(request, root =>
            root["execution_status"] = "FAIL").ToJsonString(), request);
        var verdict = ResultValidator.Observe(result, CompleteObservation(request));
        Assert.False(verdict.AcceptedPass);
        Assert.Equal("FAIL", verdict.ReasonCode);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(2147483648L)]
    public void Negative_or_overflowed_test_counts_are_rejected(long count)
    {
        var request = RequestContractTests.ValidRequest();
        Assert.Equal("INVALID_NUMBER", Reject(request, root =>
            root["checks"]![0]!["tests"]!["passed"] = count));
    }

    [Fact]
    public void Boolean_test_count_is_not_an_integer()
    {
        var request = RequestContractTests.ValidRequest();
        Assert.Equal("INVALID_NUMBER", Reject(request, root =>
            root["checks"]![0]!["tests"]!["passed"] = true));
    }

    [Fact]
    public void Nested_unknown_and_duplicate_fields_are_rejected()
    {
        var request = RequestContractTests.ValidRequest();
        Assert.Equal("UNKNOWN_FIELD", Reject(request, root =>
            root["runner"]!["token"] = "x"));
        var duplicate = ValidJson(request).Replace(
            "\"attempt\":1", "\"attempt\":1,\"attempt\":1", StringComparison.Ordinal);
        Assert.Equal("DUPLICATE_KEY",
            Assert.Throws<ContractException>(() => Parse(duplicate, request)).Code);
    }

    [Fact]
    public void Run_URL_and_request_identity_must_match()
    {
        var request = RequestContractTests.ValidRequest();
        Assert.Equal("RUN_URL_MISMATCH", Reject(request, root =>
            root["run"]!["url"] = "https://github.com/other/repo/actions/runs/456"));
        Assert.Equal("INVALID_VALUE", Reject(request, root =>
            root["request_id"] = "b70dfe32-244d-4b79-a781-523a0b828448"));
    }

    [Fact]
    public void Reporting_completeness_is_observer_derived()
    {
        var request = RequestContractTests.ValidRequest();
        var result = Parse(ValidJson(request), request);
        var complete = CompleteObservation(request);
        var observations = new[]
        {
            complete with { AckPresent = false },
            complete with { RunConcluded = false },
            complete with { ResultCommentPresent = false },
            complete with { ResultCommentAuthor = "human" },
            complete with { AckAuthor = "human" },
            complete with { AckRequestId = "b70dfe32-244d-4b79-a781-523a0b828448" },
            complete with { AckRequestCommentId = 124 },
            complete with { AckTargetRepository = "bohanyt/other" },
            complete with { AckTargetSha = new string('e', 40) },
            complete with { AckRunId = 457 },
            complete with { AckAttempt = 2 },
            complete with { AckRequestBodySha256 = new string('e', 64) }
        };
        foreach (var observation in observations)
        {
            var verdict = ResultValidator.Observe(result, observation);
            Assert.False(verdict.AcceptedPass);
            Assert.Equal(ReportingCompleteness.Incomplete, verdict.Reporting);
        }
        var reportFailed = Parse(Mutate(request, root =>
            root["report"]!["comment_posted"] = false).ToJsonString(), request);
        Assert.Equal("REPORTING_INCOMPLETE",
            ResultValidator.Observe(reportFailed, complete).ReasonCode);
        var failedRun = ResultValidator.Observe(result,
            complete with { RunConclusion = "failure" });
        Assert.False(failedRun.AcceptedPass);
        Assert.Equal("RUN_NOT_SUCCESSFUL", failedRun.ReasonCode);
    }
}
