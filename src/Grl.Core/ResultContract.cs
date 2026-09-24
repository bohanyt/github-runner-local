using System.Text.Json;

namespace Grl.Core;

public enum ExecutionStatus
{
    PASS, FAIL, BLOCKED, CANCELLED, TIMED_OUT, EXPIRED, REJECTED, INTERRUPTED
}

public sealed record CheckCounts(int Passed, int Failed, int Skipped, int Errored);
public sealed record ResultCheck(string Name, int ExitCode, CheckCounts Tests);

public sealed record ResultContract(
    string RequestId, long RequestCommentId, string RequestBodySha256,
    string ExecutionRepository, long RunId, int RunAttempt,
    string WorkflowSha, string ProfileId, string DefinitionSha,
    string Repository, string RequestedSha, string TestedSha,
    bool RunnerElevated, ExecutionStatus Status,
    IReadOnlyList<ResultCheck> Checks, bool StatusPosted, bool CommentPosted);

public sealed record ResultObservation(
    bool AckPresent, bool RunConcluded, bool ResultCommentPresent,
    string ResultCommentAuthor, string AckAuthor, string RunConclusion,
    string AckRequestId, long AckRequestCommentId,
    string AckTargetRepository, string AckTargetSha, long AckRunId, int AckAttempt,
    string AckRequestBodySha256);

public enum ReportingCompleteness
{
    Complete, Incomplete
}

public sealed record ResultVerdict(
    bool AcceptedPass, ReportingCompleteness Reporting, string ReasonCode);

public static class ResultValidator
{
    public static ResultContract Parse(string json, RequestContract request,
        ProfilePolicy profilePolicy, string expectedExecutionRepository,
        long expectedRequestCommentId)
    {
        using var document = ContractJson.ParseStrict(json);
        var root = document.RootElement;
        ContractJson.Object(root, "schema_version", "request_id", "request_comment_id",
            "request_body_sha256", "execution_repo", "run", "workflow_sha", "profile",
            "target", "runner", "timing", "checks", "execution_status", "disk",
            "artifacts", "logs", "report");
        ContractJson.Exact(Text(root, "schema_version"), "grl.result.v1", "schema_version");
        var requestId = Text(root, "request_id");
        ContractJson.Exact(requestId, request.RequestId, "request_id");
        var commentId = ContractJson.Long(ContractJson.Required(root, "request_comment_id"), "request_comment_id", 1);
        if (commentId != expectedRequestCommentId)
            throw new ContractException("REQUEST_COMMENT_MISMATCH", "Wrong request comment identity.");
        var bodySha = Text(root, "request_body_sha256");
        ValidateSha256(bodySha, "request_body_sha256");
        ContractJson.Exact(bodySha, request.FencedBodySha256, "request_body_sha256");
        var executionRepo = Text(root, "execution_repo");
        ContractJson.Exact(executionRepo, expectedExecutionRepository, "execution_repo");
        if (!RequestEnvelope.RepositoryPattern().IsMatch(executionRepo))
            throw new ContractException("INVALID_REPOSITORY", "execution_repo is not canonical.");

        var run = ContractJson.Required(root, "run");
        ContractJson.Object(run, "id", "attempt", "url");
        var runId = ContractJson.Long(ContractJson.Required(run, "id"), "run.id", 1);
        var attempt = ContractJson.Int(ContractJson.Required(run, "attempt"), "run.attempt", 1);
        var runUrl = Text(run, "url");
        if (!string.Equals(runUrl, $"https://github.com/{executionRepo}/actions/runs/{runId}", StringComparison.Ordinal))
            throw new ContractException("RUN_URL_MISMATCH", "The run URL must identify the execution repository and run.");
        var workflowSha = Text(root, "workflow_sha");
        ValidateSha(workflowSha, "workflow_sha");

        var profile = ContractJson.Required(root, "profile");
        ContractJson.Object(profile, "id", "definition_sha");
        var profileId = Text(profile, "id");
        var definitionSha = Text(profile, "definition_sha");
        ContractJson.Exact(profileId, request.ProfileId, "profile.id");
        ContractJson.Exact(definitionSha, request.DefinitionSha, "profile.definition_sha");
        ContractJson.Exact(definitionSha, profilePolicy.TrustedDefinitionSha,
            "trusted profile.definition_sha");
        ValidateSha(definitionSha, "profile.definition_sha");

        var target = ContractJson.Required(root, "target");
        ContractJson.Object(target, "repository", "requested_sha", "tested_sha",
            "containing_branch", "branch_head_at_admission");
        var repository = Text(target, "repository");
        ContractJson.Exact(repository, request.Repository, "target.repository");
        var requestedSha = Text(target, "requested_sha");
        var testedSha = Text(target, "tested_sha");
        ContractJson.Exact(requestedSha, request.RequestedSha, "target.requested_sha");
        ContractJson.Exact(testedSha, requestedSha, "target.tested_sha");
        ValidateSha(requestedSha, "target.requested_sha");
        ValidateSha(testedSha, "target.tested_sha");
        var branch = Text(target, "containing_branch");
        if (branch.Contains("..", StringComparison.Ordinal) || branch.StartsWith('/') ||
            branch.EndsWith('/') || branch.Any(char.IsWhiteSpace))
            throw new ContractException("INVALID_BRANCH", "Invalid containing branch.");
        ValidateSha(Text(target, "branch_head_at_admission"), "target.branch_head_at_admission");

        var runner = ContractJson.Required(root, "runner");
        ContractJson.Object(runner, "name", "version", "os", "arch", "identity_class", "elevated");
        _ = Text(runner, "name");
        _ = Text(runner, "version");
        ContractJson.Exact(Text(runner, "os"), "Windows", "runner.os");
        ContractJson.Exact(Text(runner, "arch"), "X64", "runner.arch");
        var identityClass = Text(runner, "identity_class");
        if (identityClass is not ("portable-user" or "service-account"))
            throw new ContractException("INVALID_IDENTITY_CLASS", "Unknown runner identity class.");
        var elevated = ContractJson.Bool(ContractJson.Required(runner, "elevated"), "runner.elevated");

        var timing = ContractJson.Required(root, "timing");
        ContractJson.Object(timing, "admitted_at", "started_at", "finished_at", "phases");
        var admittedAt = Timestamp(timing, "admitted_at");
        var startedAt = Timestamp(timing, "started_at");
        var finishedAt = Timestamp(timing, "finished_at");
        if (admittedAt > startedAt || startedAt > finishedAt)
            throw new ContractException("INVALID_TIMING", "Timing is out of order.");
        var phases = ContractJson.Required(timing, "phases");
        if (phases.ValueKind != JsonValueKind.Array)
            throw new ContractException("INVALID_TYPE", "timing.phases must be an array.");
        foreach (var phase in phases.EnumerateArray())
        {
            ContractJson.Object(phase, "name", "duration_s");
            _ = Text(phase, "name");
            _ = ContractJson.FiniteNonnegative(ContractJson.Required(phase, "duration_s"), "duration_s");
        }

        var checksValue = ContractJson.Required(root, "checks");
        if (checksValue.ValueKind != JsonValueKind.Array)
            throw new ContractException("INVALID_TYPE", "checks must be an array.");
        var checks = new List<ResultCheck>();
        foreach (var check in checksValue.EnumerateArray())
        {
            ContractJson.Object(check, "name", "exit_code", "duration_s", "tests");
            var name = Text(check, "name");
            var exitCode = SignedInt(ContractJson.Required(check, "exit_code"), "exit_code");
            _ = ContractJson.FiniteNonnegative(ContractJson.Required(check, "duration_s"), "duration_s");
            var tests = ContractJson.Required(check, "tests");
            ContractJson.Object(tests, "passed", "failed", "skipped", "errored", "source");
            var counts = new CheckCounts(
                Count(tests, "passed"), Count(tests, "failed"),
                Count(tests, "skipped"), Count(tests, "errored"));
            _ = Text(tests, "source");
            checks.Add(new(name, exitCode, counts));
        }

        var statusText = Text(root, "execution_status");
        if (!Enum.GetNames<ExecutionStatus>().Contains(statusText, StringComparer.Ordinal) ||
            !Enum.TryParse<ExecutionStatus>(statusText, false, out var status))
            throw new ContractException("UNKNOWN_STATUS", "Unknown execution_status.");

        var disk = ContractJson.Required(root, "disk");
        ContractJson.Object(disk, "free_before_gib", "free_after_gib", "reserve_gib");
        _ = ContractJson.FiniteNonnegative(ContractJson.Required(disk, "free_before_gib"), "free_before_gib");
        _ = ContractJson.FiniteNonnegative(ContractJson.Required(disk, "free_after_gib"), "free_after_gib");
        _ = ContractJson.Int(ContractJson.Required(disk, "reserve_gib"), "reserve_gib", 0);

        var artifacts = ContractJson.Required(root, "artifacts");
        if (artifacts.ValueKind != JsonValueKind.Array)
            throw new ContractException("INVALID_TYPE", "artifacts must be an array.");
        foreach (var artifact in artifacts.EnumerateArray())
        {
            ContractJson.Object(artifact, "name", "url");
            _ = Text(artifact, "name");
            ValidateHttps(Text(artifact, "url"), "artifact.url");
        }
        var logs = ContractJson.Required(root, "logs");
        ContractJson.Object(logs, "url");
        ContractJson.Exact(Text(logs, "url"), runUrl, "logs.url");

        var report = ContractJson.Required(root, "report");
        ContractJson.Object(report, "attempts", "status_posted", "comment_posted");
        _ = ContractJson.Int(ContractJson.Required(report, "attempts"), "report.attempts", 1);
        var statusPosted = ContractJson.Bool(ContractJson.Required(report, "status_posted"), "report.status_posted");
        var commentPosted = ContractJson.Bool(ContractJson.Required(report, "comment_posted"), "report.comment_posted");

        if (status == ExecutionStatus.PASS)
        {
            var totalPassed = checks.Aggregate(0L, (sum, check) => sum + check.Tests.Passed);
            if (profilePolicy.MinimumRequiredTests < 1 || totalPassed < profilePolicy.MinimumRequiredTests ||
                checks.Count == 0 || checks.Any(check => check.ExitCode != 0 ||
                check.Tests.Failed != 0 || check.Tests.Errored != 0) || elevated)
                throw new ContractException("INVALID_PASS", "PASS contradicts test, exit or runner evidence.");
        }

        return new(requestId, commentId, bodySha, executionRepo, runId, attempt,
            workflowSha, profileId, definitionSha, repository, requestedSha,
            testedSha, elevated, status, checks.AsReadOnly(), statusPosted, commentPosted);
    }

    public static ResultVerdict Observe(ResultContract result, ResultObservation observation)
    {
        var reportingComplete = observation.AckPresent && observation.RunConcluded &&
            observation.ResultCommentPresent && result.StatusPosted && result.CommentPosted &&
            observation.ResultCommentAuthor == "github-actions[bot]" &&
            observation.AckAuthor == "github-actions[bot]" &&
            observation.AckRequestId == result.RequestId &&
            observation.AckRequestCommentId == result.RequestCommentId &&
            observation.AckTargetRepository == result.Repository &&
            observation.AckTargetSha == result.RequestedSha &&
            observation.AckRunId == result.RunId && observation.AckAttempt == result.RunAttempt &&
            observation.AckRequestBodySha256 == result.RequestBodySha256;
        if (!reportingComplete)
            return new(false, ReportingCompleteness.Incomplete, "REPORTING_INCOMPLETE");
        if (result.Status != ExecutionStatus.PASS)
            return new(false, ReportingCompleteness.Complete, result.Status.ToString());
        if (observation.RunConclusion != "success")
            return new(false, ReportingCompleteness.Complete, "RUN_NOT_SUCCESSFUL");
        return new(true, ReportingCompleteness.Complete, "PASS");
    }

    private static string Text(JsonElement parent, string name) =>
        ContractJson.String(ContractJson.Required(parent, name), name);

    private static int Count(JsonElement parent, string name) =>
        ContractJson.Int(ContractJson.Required(parent, name), name, 0);

    private static int SignedInt(JsonElement value, string name)
    {
        if (value.ValueKind != JsonValueKind.Number || !value.TryGetInt32(out var result))
            throw new ContractException("INVALID_NUMBER", $"{name} must be an integer.");
        return result;
    }

    private static DateTimeOffset Timestamp(JsonElement parent, string name)
    {
        if (!RequestEnvelope.TryParseUtc(Text(parent, name), out var value))
            throw new ContractException("INVALID_TIME", $"{name} must be UTC Z.");
        return value;
    }

    private static void ValidateSha(string value, string name)
    {
        if (!RequestEnvelope.ShaPattern().IsMatch(value))
            throw new ContractException("INVALID_SHA", $"{name} must be lowercase 40-character SHA.");
    }

    private static void ValidateSha256(string value, string name)
    {
        if (value.Length != 64 || value.Any(c => c is not (>= '0' and <= '9' or >= 'a' and <= 'f')))
            throw new ContractException("INVALID_SHA256", $"{name} must be lowercase SHA-256.");
    }

    private static void ValidateHttps(string value, string name)
    {
        if (!Uri.TryCreate(value, UriKind.Absolute, out var uri) || uri.Scheme != Uri.UriSchemeHttps)
            throw new ContractException("INVALID_URL", $"{name} must be an HTTPS URL.");
    }
}
