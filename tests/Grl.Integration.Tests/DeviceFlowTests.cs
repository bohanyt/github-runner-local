using System.Net;
using System.Text;
using Grl.Integration;

namespace Grl.Integration.Tests;

public sealed class DeviceFlowTests
{
    private const string Device = """
        {"device_code":"device-secret","user_code":"ABCD-EFGH","verification_uri":"https://github.com/login/device","expires_in":900,"interval":2}
        """;
    private const string Success = """
        {"access_token":"ghu_access-secret","token_type":"bearer","refresh_token":"ghr_refresh-secret"}
        """;

    [Fact]
    public async Task DeviceRequestAndSuccessUseRuntimeClientId()
    {
        var handler = new QueueHandler(Json(Device), Json(Success));
        var delay = new RecordingDelay();
        var flow = new GitHubDeviceFlow(new HttpClient(handler), "runtime-client", delay);
        using var authorization = await flow.RequestDeviceCodeAsync(default);
        Assert.Equal("ABCD-EFGH", authorization.UserCode);
        Assert.Equal("https://github.com/login/device", authorization.VerificationUri.ToString());
        using var token = await flow.PollForTokenAsync(authorization, default);
        Assert.Equal([TimeSpan.FromSeconds(2)], delay.Intervals);
        Assert.Contains("client_id=runtime-client", handler.Bodies[0]);
        Assert.Contains("device_code=device-secret", handler.Bodies[1]);
        Assert.DoesNotContain("device-secret", authorization.ToString());
        Assert.DoesNotContain("ghu_access-secret", token.ToString());
        Assert.DoesNotContain("ghr_refresh-secret", token.ToString());
        await Assert.ThrowsAsync<ObjectDisposedException>(() => flow.PollForTokenAsync(authorization, default));
    }

    [Fact]
    public async Task PendingThenSuccessPreservesInterval()
    {
        var (flow, delay) = Flow(Json(Device), Json("""{"error":"authorization_pending"}""", HttpStatusCode.BadRequest), Json(Success));
        using var session = await flow.RequestDeviceCodeAsync(default);
        using var token = await flow.PollForTokenAsync(session, default);
        Assert.Equal([TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(2)], delay.Intervals);
    }

    [Fact]
    public async Task SlowDownAddsFiveSeconds()
    {
        var (flow, delay) = Flow(Json(Device), Json("""{"error":"slow_down"}""", HttpStatusCode.BadRequest), Json(Success));
        using var session = await flow.RequestDeviceCodeAsync(default);
        using var token = await flow.PollForTokenAsync(session, default);
        Assert.Equal([TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(7)], delay.Intervals);
    }

    [Theory]
    [InlineData("access_denied", GitHubFailure.Denied)]
    [InlineData("expired_token", GitHubFailure.Expired)]
    [InlineData("device_flow_disabled", GitHubFailure.Disabled)]
    [InlineData("incorrect_client_credentials", GitHubFailure.BadClient)]
    public async Task PollErrorsAreClassifiedWithoutResponseSecrets(string error, GitHubFailure expected)
    {
        var (flow, _) = Flow(Json(Device), Json($$"""{"error":"{{error}}","access_token":"ghu_do-not-print"}""", HttpStatusCode.BadRequest));
        using var session = await flow.RequestDeviceCodeAsync(default);
        var exception = await Assert.ThrowsAsync<GitHubIntegrationException>(() => flow.PollForTokenAsync(session, default));
        Assert.Equal(expected, exception.Failure);
        Assert.DoesNotContain("ghu_", exception.ToString());
    }

    [Fact]
    public async Task CancellationDiscardsDeviceCode()
    {
        var delay = new RecordingDelay { Cancel = true };
        var flow = new GitHubDeviceFlow(new HttpClient(new QueueHandler(Json(Device))), "client", delay);
        using var session = await flow.RequestDeviceCodeAsync(default);
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => flow.PollForTokenAsync(session, default));
        Assert.Equal("DeviceAuthorization [redacted]", session.ToString());
        delay.Cancel = false;
        await Assert.ThrowsAsync<ObjectDisposedException>(() => flow.PollForTokenAsync(session, default));
    }

    [Fact]
    public async Task NetworkFailuresAreTransient()
    {
        var flow = new GitHubDeviceFlow(new HttpClient(new ThrowingHandler()), "client", new RecordingDelay());
        var exception = await Assert.ThrowsAsync<GitHubIntegrationException>(() => flow.RequestDeviceCodeAsync(default));
        Assert.Equal(GitHubFailure.TransientNetwork, exception.Failure);
    }

    [Fact]
    public async Task HttpTimeoutIsTransient()
    {
        var flow = new GitHubDeviceFlow(new HttpClient(new TimeoutHandler()), "client", new RecordingDelay());
        var exception = await Assert.ThrowsAsync<GitHubIntegrationException>(() => flow.RequestDeviceCodeAsync(default));
        Assert.Equal(GitHubFailure.TransientNetwork, exception.Failure);
    }

    [Theory]
    [InlineData("{")]
    [InlineData("[]")]
    [InlineData("{}")]
    public async Task MalformedDeviceResponseIsRejected(string response)
    {
        var flow = new GitHubDeviceFlow(new HttpClient(new QueueHandler(Json(response))), "client");
        var exception = await Assert.ThrowsAsync<GitHubIntegrationException>(() => flow.RequestDeviceCodeAsync(default));
        Assert.Equal(GitHubFailure.UnexpectedResponse, exception.Failure);
    }

    [Fact]
    public async Task UnexpectedTokenResponseIsRejected()
    {
        var (flow, _) = Flow(Json(Device), Json("""{"token_type":"bearer"}"""));
        using var session = await flow.RequestDeviceCodeAsync(default);
        var exception = await Assert.ThrowsAsync<GitHubIntegrationException>(() => flow.PollForTokenAsync(session, default));
        Assert.Equal(GitHubFailure.UnexpectedResponse, exception.Failure);
    }

    [Fact]
    public async Task UserAndInstallationAndRepositoryModelsUseBearerToken()
    {
        var handler = new QueueHandler(
            Json(Device), Json(Success),
            Json("""{"id":3,"login":"owner","avatar_url":"https://avatars.githubusercontent.com/u/3"}"""),
            Json("""{"installations":[{"id":7,"account":{"login":"owner"}}]}"""),
            Json("""{"repositories":[{"id":9,"full_name":"owner/execution","private":true,"permissions":{"admin":true}}]}"""),
            Json("""{"id":9,"full_name":"owner/execution","private":true,"permissions":{"admin":true}}"""));
        var flow = new GitHubDeviceFlow(new HttpClient(handler), "client", new RecordingDelay());
        using var session = await flow.RequestDeviceCodeAsync(default);
        using var token = await flow.PollForTokenAsync(session, default);
        Assert.Equal("owner", (await flow.GetCurrentUserAsync(token, default)).Login);
        Assert.Equal(7, Assert.Single(await flow.GetInstallationsAsync(token, default)).Id);
        Assert.Equal(9, Assert.Single(await flow.GetInstallationRepositoriesAsync(7, token, default)).Id);
        Assert.Equal("owner/execution", (await flow.RequirePrivateAdminRepositoryAsync("owner", "execution", token, default)).FullName);
        Assert.All(handler.Authorization.Skip(2), x => Assert.Equal("Bearer", x));
    }

    [Fact]
    public async Task InstallationListingReadsLaterPages()
    {
        var firstPage = System.Text.Json.JsonSerializer.Serialize(new
        {
            installations = Enumerable.Range(1, 100).Select(id => new { id, account = new { login = "owner" } })
        });
        var handler = new QueueHandler(Json(Device), Json(Success), Json(firstPage),
            Json("""{"installations":[{"id":101,"account":{"login":"owner"}}]}"""));
        var flow = new GitHubDeviceFlow(new HttpClient(handler), "client", new RecordingDelay());
        using var session = await flow.RequestDeviceCodeAsync(default);
        using var token = await flow.PollForTokenAsync(session, default);
        Assert.Equal(101, (await flow.GetInstallationsAsync(token, default)).Count);
        Assert.EndsWith("page=2", handler.RequestUris[^1]);
    }

    [Theory]
    [InlineData(false, true, GitHubFailure.PublicRepository)]
    [InlineData(true, false, GitHubFailure.AdminRequired)]
    public async Task RepositoryMustBePrivateAndAdmin(bool isPrivate, bool admin, GitHubFailure expected)
    {
        var repositoryJson = System.Text.Json.JsonSerializer.Serialize(new
        {
            id = 9, full_name = "owner/repo", @private = isPrivate, permissions = new { admin }
        });
        var handler = new QueueHandler(Json(Device), Json(Success), Json(repositoryJson));
        var flow = new GitHubDeviceFlow(new HttpClient(handler), "client", new RecordingDelay());
        using var session = await flow.RequestDeviceCodeAsync(default);
        using var token = await flow.PollForTokenAsync(session, default);
        var error = await Assert.ThrowsAsync<GitHubIntegrationException>(
            () => flow.RequirePrivateAdminRepositoryAsync("owner", "repo", token, default));
        Assert.Equal(expected, error.Failure);
    }

    private static (GitHubDeviceFlow Flow, RecordingDelay Delay) Flow(params HttpResponseMessage[] responses)
    {
        var delay = new RecordingDelay();
        return (new GitHubDeviceFlow(new HttpClient(new QueueHandler(responses)), "client", delay), delay);
    }

    private static HttpResponseMessage Json(string json, HttpStatusCode status = HttpStatusCode.OK) =>
        new(status) { Content = new StringContent(json, Encoding.UTF8, "application/json") };

    private sealed class RecordingDelay : IAsyncDelay
    {
        public List<TimeSpan> Intervals { get; } = [];
        public bool Cancel { get; set; }
        public Task WaitAsync(TimeSpan delay, CancellationToken cancellationToken)
        {
            Intervals.Add(delay);
            if (Cancel) throw new OperationCanceledException();
            return Task.CompletedTask;
        }
    }

    private sealed class QueueHandler(params HttpResponseMessage[] responses) : HttpMessageHandler
    {
        private readonly Queue<HttpResponseMessage> queue = new(responses);
        public List<string> Bodies { get; } = [];
        public List<string?> Authorization { get; } = [];
        public List<string> RequestUris { get; } = [];
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            RequestUris.Add(request.RequestUri!.ToString());
            Bodies.Add(request.Content is null ? "" : await request.Content.ReadAsStringAsync(cancellationToken));
            Authorization.Add(request.Headers.Authorization?.Scheme);
            return queue.Dequeue();
        }
    }

    private sealed class ThrowingHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            throw new HttpRequestException("network offline");
    }

    private sealed class TimeoutHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            throw new TaskCanceledException("timeout");
    }
}
