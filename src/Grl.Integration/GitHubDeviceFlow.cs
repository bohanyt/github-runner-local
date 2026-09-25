using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;

namespace Grl.Integration;

public enum GitHubFailure
{
    Denied, Expired, Disabled, BadClient, TransientNetwork, UnexpectedResponse,
    PublicRepository, AdminRequired
}

public sealed class GitHubIntegrationException(GitHubFailure failure, string message) : Exception(message)
{
    public GitHubFailure Failure { get; } = failure;
}

// The device code and access token are intentionally excluded from record-generated
// equality, logging and ToString output. Neither is persisted by this library.
public sealed class DeviceAuthorization : IDisposable
{
    private string? deviceCode;
    internal string Code => deviceCode ?? throw new ObjectDisposedException(nameof(DeviceAuthorization));
    internal DeviceAuthorization(string code, string userCode, Uri verificationUri, TimeSpan interval, DateTimeOffset expiresAt)
    {
        deviceCode = code;
        UserCode = userCode;
        VerificationUri = verificationUri;
        Interval = interval;
        ExpiresAt = expiresAt;
    }
    public string UserCode { get; }
    public Uri VerificationUri { get; }
    public TimeSpan Interval { get; }
    public DateTimeOffset ExpiresAt { get; }
    public void Dispose() => deviceCode = null;
    public override string ToString() => "DeviceAuthorization [redacted]";
}

public sealed class GitHubAccessToken : IDisposable
{
    private string? value;
    internal GitHubAccessToken(string value) => this.value = value;
    internal void Authorize(HttpRequestMessage request)
    {
        request.Headers.Authorization = new AuthenticationHeaderValue(
            "Bearer", value ?? throw new ObjectDisposedException(nameof(GitHubAccessToken)));
    }
    public void Dispose() => value = null;
    public override string ToString() => "GitHubAccessToken [redacted]";
}

public interface IAsyncDelay
{
    Task WaitAsync(TimeSpan delay, CancellationToken cancellationToken);
}

public sealed class SystemAsyncDelay : IAsyncDelay
{
    public Task WaitAsync(TimeSpan delay, CancellationToken cancellationToken) => Task.Delay(delay, cancellationToken);
}

public sealed record GitHubUser(long Id, string Login, Uri? AvatarUrl);
public sealed record GitHubInstallation(long Id, string AccountLogin);
public sealed record GitHubRepository(long Id, string FullName, bool IsPrivate, bool HasAdminPermission);

public sealed class GitHubDeviceFlow
{
    private static readonly Uri DeviceEndpoint = new("https://github.com/login/device/code");
    private static readonly Uri TokenEndpoint = new("https://github.com/login/oauth/access_token");
    private static readonly Uri ApiBase = new("https://api.github.com/");
    private readonly HttpClient http;
    private readonly IAsyncDelay delay;
    private readonly TimeProvider clock;
    private readonly string clientId;

    public GitHubDeviceFlow(HttpClient http, string clientId, IAsyncDelay? delay = null, TimeProvider? clock = null)
    {
        this.http = http;
        this.clientId = !string.IsNullOrWhiteSpace(clientId) ? clientId : throw new ArgumentException("A runtime client ID is required.", nameof(clientId));
        this.delay = delay ?? new SystemAsyncDelay();
        this.clock = clock ?? TimeProvider.System;
    }

    public async Task<DeviceAuthorization> RequestDeviceCodeAsync(CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, DeviceEndpoint)
        {
            Content = new FormUrlEncodedContent(new Dictionary<string, string> { ["client_id"] = clientId })
        };
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        using var response = await SendAsync(request, cancellationToken);
        using var body = await ReadJsonAsync(response, cancellationToken);
        if (!response.IsSuccessStatusCode)
            throw ErrorFromResponse(body.RootElement, response.StatusCode);
        var root = body.RootElement;
        var code = RequiredString(root, "device_code");
        var userCode = RequiredString(root, "user_code");
        var uriText = RequiredString(root, "verification_uri");
        if (!Uri.TryCreate(uriText, UriKind.Absolute, out var uri) || uri.Scheme != Uri.UriSchemeHttps)
            throw Unexpected();
        var interval = RequiredPositiveInt(root, "interval");
        var expires = RequiredPositiveInt(root, "expires_in");
        return new DeviceAuthorization(code, userCode, uri, TimeSpan.FromSeconds(interval), clock.GetUtcNow().AddSeconds(expires));
    }

    public async Task<GitHubAccessToken> PollForTokenAsync(DeviceAuthorization authorization, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(authorization);
        var interval = authorization.Interval;
        try
        {
            while (clock.GetUtcNow() < authorization.ExpiresAt)
            {
                await delay.WaitAsync(interval, cancellationToken);
                cancellationToken.ThrowIfCancellationRequested();
                using var request = new HttpRequestMessage(HttpMethod.Post, TokenEndpoint)
                {
                    Content = new FormUrlEncodedContent(new Dictionary<string, string>
                    {
                        ["client_id"] = clientId,
                        ["device_code"] = authorization.Code,
                        ["grant_type"] = "urn:ietf:params:oauth:grant-type:device_code"
                    })
                };
                request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                using var response = await SendAsync(request, cancellationToken);
                using var body = await ReadJsonAsync(response, cancellationToken);
                var root = body.RootElement;
                if (TryString(root, "error", out var error))
                {
                    switch (error)
                    {
                        case "authorization_pending": continue;
                        case "slow_down": interval += TimeSpan.FromSeconds(5); continue;
                        default: throw ErrorFromResponse(root, response.StatusCode);
                    }
                }
                if (!response.IsSuccessStatusCode) throw Unexpected();
                var token = RequiredString(root, "access_token");
                if (!string.Equals(RequiredString(root, "token_type"), "bearer", StringComparison.OrdinalIgnoreCase))
                    throw Unexpected();
                // refresh_token, if present, is never read into a domain object.
                return new GitHubAccessToken(token);
            }
            throw new GitHubIntegrationException(GitHubFailure.Expired, "The device code expired.");
        }
        finally
        {
            authorization.Dispose();
        }
    }

    public async Task<GitHubUser> GetCurrentUserAsync(GitHubAccessToken token, CancellationToken ct)
    {
        using var doc = await ApiGetAsync("user", token, ct);
        var root = doc.RootElement;
        Uri? avatar = null;
        if (TryString(root, "avatar_url", out var rawAvatar) && Uri.TryCreate(rawAvatar, UriKind.Absolute, out var parsed))
            avatar = parsed;
        return new GitHubUser(RequiredLong(root, "id"), RequiredString(root, "login"), avatar);
    }

    public async Task<IReadOnlyList<GitHubInstallation>> GetInstallationsAsync(GitHubAccessToken token, CancellationToken ct)
    {
        var result = new List<GitHubInstallation>();
        for (var page = 1; page <= 100; page++)
        {
            using var doc = await ApiGetAsync($"user/installations?per_page=100&page={page}", token, ct);
            var array = RequiredArray(doc.RootElement, "installations");
            foreach (var x in array.EnumerateArray())
            {
                if (!x.TryGetProperty("account", out var account)) throw Unexpected();
                result.Add(new GitHubInstallation(RequiredLong(x, "id"), RequiredString(account, "login")));
            }
            if (array.GetArrayLength() < 100) return result;
        }
        throw Unexpected();
    }

    public async Task<IReadOnlyList<GitHubRepository>> GetInstallationRepositoriesAsync(long installationId, GitHubAccessToken token, CancellationToken ct)
    {
        if (installationId <= 0) throw new ArgumentOutOfRangeException(nameof(installationId));
        var result = new List<GitHubRepository>();
        for (var page = 1; page <= 100; page++)
        {
            using var doc = await ApiGetAsync($"user/installations/{installationId}/repositories?per_page=100&page={page}", token, ct);
            var array = RequiredArray(doc.RootElement, "repositories");
            result.AddRange(array.EnumerateArray().Select(ParseRepository));
            if (array.GetArrayLength() < 100) return result;
        }
        throw Unexpected();
    }

    public async Task<GitHubRepository> GetRepositoryAsync(string owner, string name, GitHubAccessToken token, CancellationToken ct)
    {
        if (!SafeSlug(owner) || !SafeSlug(name)) throw new ArgumentException("A simple repository owner and name are required.");
        using var doc = await ApiGetAsync($"repos/{owner}/{name}", token, ct);
        return ParseRepository(doc.RootElement);
    }

    public async Task<GitHubRepository> RequirePrivateAdminRepositoryAsync(string owner, string name, GitHubAccessToken token, CancellationToken ct)
    {
        var repository = await GetRepositoryAsync(owner, name, token, ct);
        if (!repository.IsPrivate)
            throw new GitHubIntegrationException(GitHubFailure.PublicRepository, "The target repository must be private.");
        if (!repository.HasAdminPermission)
            throw new GitHubIntegrationException(GitHubFailure.AdminRequired, "Repository administration permission is required.");
        return repository;
    }

    private async Task<JsonDocument> ApiGetAsync(string relativePath, GitHubAccessToken token, CancellationToken ct)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, new Uri(ApiBase, relativePath));
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
        request.Headers.UserAgent.ParseAdd("github-runner-local-C-source");
        token.Authorize(request);
        using var response = await SendAsync(request, ct);
        var doc = await ReadJsonAsync(response, ct);
        if (response.IsSuccessStatusCode) return doc;
        doc.Dispose();
        throw response.StatusCode is HttpStatusCode.TooManyRequests or HttpStatusCode.BadGateway or HttpStatusCode.ServiceUnavailable or HttpStatusCode.GatewayTimeout
            ? new GitHubIntegrationException(GitHubFailure.TransientNetwork, "GitHub is temporarily unavailable.")
            : Unexpected();
    }

    private async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
    {
        try { return await http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct); }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
        catch (OperationCanceledException) { throw new GitHubIntegrationException(GitHubFailure.TransientNetwork, "GitHub request timed out."); }
        catch (HttpRequestException) { throw new GitHubIntegrationException(GitHubFailure.TransientNetwork, "GitHub could not be reached."); }
    }

    private static async Task<JsonDocument> ReadJsonAsync(HttpResponseMessage response, CancellationToken ct)
    {
        try
        {
            await using var stream = await response.Content.ReadAsStreamAsync(ct);
            using var bounded = new MemoryStream();
            var buffer = new byte[8192];
            int read;
            while ((read = await stream.ReadAsync(buffer, ct)) != 0)
            {
                if (bounded.Length + read > 1_048_576) throw Unexpected();
                bounded.Write(buffer, 0, read);
            }
            bounded.Position = 0;
            var document = await JsonDocument.ParseAsync(bounded, cancellationToken: ct);
            if (document.RootElement.ValueKind != JsonValueKind.Object)
            {
                document.Dispose();
                throw Unexpected();
            }
            return document;
        }
        catch (JsonException) { throw Unexpected(); }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested) { throw new GitHubIntegrationException(GitHubFailure.TransientNetwork, "GitHub response timed out."); }
        catch (HttpRequestException) { throw new GitHubIntegrationException(GitHubFailure.TransientNetwork, "GitHub could not be reached."); }
        catch (IOException) { throw new GitHubIntegrationException(GitHubFailure.TransientNetwork, "GitHub response was interrupted."); }
    }

    private static GitHubRepository ParseRepository(JsonElement x)
    {
        if (!x.TryGetProperty("permissions", out var permissions) || permissions.ValueKind != JsonValueKind.Object ||
            !permissions.TryGetProperty("admin", out var admin) ||
            admin.ValueKind is not (JsonValueKind.True or JsonValueKind.False))
            throw Unexpected();
        if (!x.TryGetProperty("private", out var privateValue) ||
            privateValue.ValueKind is not (JsonValueKind.True or JsonValueKind.False))
            throw Unexpected();
        return new GitHubRepository(RequiredLong(x, "id"), RequiredString(x, "full_name"),
            privateValue.GetBoolean(), admin.GetBoolean());
    }

    private static GitHubIntegrationException ErrorFromResponse(JsonElement root, HttpStatusCode status)
    {
        if (TryString(root, "error", out var error))
            return error switch
            {
                "access_denied" => new(GitHubFailure.Denied, "Device authorization was denied."),
                "expired_token" => new(GitHubFailure.Expired, "The device code expired."),
                "device_flow_disabled" => new(GitHubFailure.Disabled, "Device flow is disabled for this App."),
                "incorrect_client_credentials" => new(GitHubFailure.BadClient, "The client ID was rejected."),
                _ => Unexpected()
            };
        return (int)status >= 500 || status == HttpStatusCode.TooManyRequests
            ? new(GitHubFailure.TransientNetwork, "GitHub is temporarily unavailable.")
            : Unexpected();
    }

    private static GitHubIntegrationException Unexpected() =>
        new(GitHubFailure.UnexpectedResponse, "GitHub returned an unexpected response.");
    private static bool TryString(JsonElement root, string key, out string value)
    {
        value = "";
        if (root.ValueKind != JsonValueKind.Object || !root.TryGetProperty(key, out var item) ||
            item.ValueKind != JsonValueKind.String) return false;
        value = item.GetString() ?? "";
        return value.Length > 0;
    }
    private static string RequiredString(JsonElement root, string key) =>
        TryString(root, key, out var value) ? value : throw Unexpected();
    private static int RequiredPositiveInt(JsonElement root, string key) =>
        root.TryGetProperty(key, out var item) && item.TryGetInt32(out var value) && value > 0
            ? value : throw Unexpected();
    private static long RequiredLong(JsonElement root, string key) =>
        root.TryGetProperty(key, out var item) && item.TryGetInt64(out var value) && value > 0
            ? value : throw Unexpected();
    private static JsonElement RequiredArray(JsonElement root, string key) =>
        root.TryGetProperty(key, out var item) && item.ValueKind == JsonValueKind.Array
            ? item : throw Unexpected();
    private static bool SafeSlug(string value) =>
        !string.IsNullOrWhiteSpace(value) && value.Length <= 100 &&
        value.All(c => char.IsAsciiLetterOrDigit(c) || c is '-' or '_' or '.');
}
