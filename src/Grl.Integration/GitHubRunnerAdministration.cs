using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Grl.Integration;

public enum RunnerAdminFailure { InvalidRepository, Transient, Unexpected, IdentityChanged }

public sealed class RunnerAdminException(RunnerAdminFailure failure, string message) : Exception(message)
{
    public RunnerAdminFailure Failure { get; } = failure;
}

public sealed record RepositoryRunner(long Id, string Name, bool Online, bool Busy);

public sealed class RunnerOneHourToken : IDisposable
{
    private string? value;
    internal RunnerOneHourToken(string value) => this.value = value;
    internal string Value => value ?? throw new ObjectDisposedException(nameof(RunnerOneHourToken));
    public void Dispose() => value = null;
    public override string ToString() => "RunnerOneHourToken [redacted]";
}

public interface IRunnerAdministration
{
    GitHubRepository Repository { get; }
    Task<IReadOnlyList<RepositoryRunner>> ListAsync(CancellationToken ct);
    Task<RunnerOneHourToken> CreateRegistrationTokenAsync(CancellationToken ct);
    Task<RunnerOneHourToken> CreateRemoveTokenAsync(CancellationToken ct);
    Task DeleteStaleAsync(long id, string confirmedName, CancellationToken ct);
}

public sealed class GitHubRunnerAdministration : IRunnerAdministration
{
    private static readonly Uri ApiBase = new("https://api.github.com/");
    private readonly HttpClient http;
    private readonly GitHubAccessToken accessToken;
    private readonly TimeProvider clock;
    private readonly string path;

    public GitHubRunnerAdministration(HttpClient http, GitHubAccessToken accessToken, GitHubRepository selected,
        TimeProvider? clock = null)
    {
        ArgumentNullException.ThrowIfNull(http);
        ArgumentNullException.ThrowIfNull(accessToken);
        ArgumentNullException.ThrowIfNull(selected);
        if (selected.Id <= 0 || !selected.IsPrivate || !selected.HasAdminPermission ||
            !Regex.IsMatch(selected.FullName, @"\A[A-Za-z0-9](?:[A-Za-z0-9-]{0,38})/[A-Za-z0-9_.-]{1,100}\z", RegexOptions.CultureInvariant) ||
            selected.FullName.Contains("..", StringComparison.Ordinal))
            throw new RunnerAdminException(RunnerAdminFailure.InvalidRepository, "A canonical private administration target is required.");
        this.http = http;
        this.accessToken = accessToken;
        this.clock = clock ?? TimeProvider.System;
        Repository = selected;
        path = $"repos/{selected.FullName}/actions/runners";
    }

    public GitHubRepository Repository { get; }

    public async Task<IReadOnlyList<RepositoryRunner>> ListAsync(CancellationToken ct)
    {
        var all = new List<RepositoryRunner>();
        for (var page = 1; page <= 100; page++)
        {
            using var response = await SendAsync(HttpMethod.Get, $"{path}?per_page=100&page={page}", ct);
            using var doc = await ReadJsonAsync(response, HttpStatusCode.OK, ct);
            var root = doc.RootElement;
            if (!root.TryGetProperty("runners", out var array) || array.ValueKind != JsonValueKind.Array ||
                array.GetArrayLength() > 100 || !root.TryGetProperty("total_count", out var count) ||
                !count.TryGetInt32(out var total) || total < 0 || total > 10_000)
                throw Unexpected();
            foreach (var item in array.EnumerateArray())
            {
                if (item.ValueKind != JsonValueKind.Object ||
                    !item.TryGetProperty("id", out var id) || !id.TryGetInt64(out var numericId) || numericId <= 0 ||
                    !item.TryGetProperty("name", out var name) || name.ValueKind != JsonValueKind.String ||
                    string.IsNullOrWhiteSpace(name.GetString()) || name.GetString()!.Length > 128 ||
                    !item.TryGetProperty("status", out var status) || status.ValueKind != JsonValueKind.String ||
                    status.GetString() is not ("online" or "offline") ||
                    !item.TryGetProperty("busy", out var busy) || busy.ValueKind is not (JsonValueKind.True or JsonValueKind.False))
                    throw Unexpected();
                all.Add(new RepositoryRunner(numericId, name.GetString()!, status.GetString() == "online", busy.GetBoolean()));
            }
            if (array.GetArrayLength() < 100)
            {
                if (all.Count != total || all.Select(x => x.Id).Distinct().Count() != all.Count) throw Unexpected();
                return all;
            }
        }
        throw Unexpected();
    }

    public Task<RunnerOneHourToken> CreateRegistrationTokenAsync(CancellationToken ct) =>
        CreateTokenAsync("registration-token", ct);

    public Task<RunnerOneHourToken> CreateRemoveTokenAsync(CancellationToken ct) =>
        CreateTokenAsync("remove-token", ct);

    public async Task DeleteStaleAsync(long id, string confirmedName, CancellationToken ct)
    {
        if (id <= 0 || string.IsNullOrWhiteSpace(confirmedName))
            throw new RunnerAdminException(RunnerAdminFailure.IdentityChanged, "Runner confirmation is invalid.");
        var current = (await ListAsync(ct)).SingleOrDefault(x => x.Id == id);
        if (current is null || current.Name != confirmedName || current.Online || current.Busy)
            throw new RunnerAdminException(RunnerAdminFailure.IdentityChanged, "Runner identity or state changed; removal refused.");
        using var response = await SendAsync(HttpMethod.Delete, $"{path}/{id}", ct);
        if (response.StatusCode != HttpStatusCode.NoContent) throw Classify(response.StatusCode);
    }

    private async Task<RunnerOneHourToken> CreateTokenAsync(string suffix, CancellationToken ct)
    {
        using var response = await SendAsync(HttpMethod.Post, $"{path}/{suffix}", ct);
        using var doc = await ReadJsonAsync(response, HttpStatusCode.Created, ct);
        var root = doc.RootElement;
        if (!root.TryGetProperty("token", out var value) || value.ValueKind != JsonValueKind.String ||
            string.IsNullOrWhiteSpace(value.GetString()) || value.GetString()!.Length > 512 ||
            !root.TryGetProperty("expires_at", out var expiry) || expiry.ValueKind != JsonValueKind.String ||
            !DateTimeOffset.TryParse(expiry.GetString(), out var expiresAt) ||
            expiresAt <= clock.GetUtcNow() || expiresAt > clock.GetUtcNow().AddHours(2))
            throw Unexpected();
        return new RunnerOneHourToken(value.GetString()!);
    }

    private async Task<HttpResponseMessage> SendAsync(HttpMethod method, string relative, CancellationToken ct)
    {
        using var request = new HttpRequestMessage(method, new Uri(ApiBase, relative));
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
        request.Headers.UserAgent.ParseAdd("github-runner-local-D-source");
        accessToken.Authorize(request);
        try
        {
            var response = await http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);
            if (response.RequestMessage?.RequestUri is Uri final && final != request.RequestUri)
            {
                response.Dispose();
                throw new RunnerAdminException(RunnerAdminFailure.Unexpected, "GitHub API redirect was refused.");
            }
            return response;
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
        catch (OperationCanceledException) { throw new RunnerAdminException(RunnerAdminFailure.Transient, "GitHub request timed out."); }
        catch (HttpRequestException) { throw new RunnerAdminException(RunnerAdminFailure.Transient, "GitHub could not be reached."); }
    }

    private static async Task<JsonDocument> ReadJsonAsync(HttpResponseMessage response, HttpStatusCode expected, CancellationToken ct)
    {
        if (response.StatusCode != expected) throw Classify(response.StatusCode);
        if (response.Content is null) throw Unexpected();
        try
        {
            await using var stream = await response.Content.ReadAsStreamAsync(ct);
            using var bounded = new MemoryStream();
            var buffer = new byte[8192];
            int read;
            while ((read = await stream.ReadAsync(buffer, ct)) > 0)
            {
                if (bounded.Length + read > 262_144) throw Unexpected();
                bounded.Write(buffer, 0, read);
            }
            bounded.Position = 0;
            var doc = await JsonDocument.ParseAsync(bounded, new JsonDocumentOptions { MaxDepth = 12 }, ct);
            if (doc.RootElement.ValueKind != JsonValueKind.Object) { doc.Dispose(); throw Unexpected(); }
            return doc;
        }
        catch (JsonException) { throw Unexpected(); }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested) { throw new RunnerAdminException(RunnerAdminFailure.Transient, "GitHub response timed out."); }
        catch (HttpRequestException) { throw new RunnerAdminException(RunnerAdminFailure.Transient, "GitHub response failed."); }
        catch (IOException) { throw new RunnerAdminException(RunnerAdminFailure.Transient, "GitHub response was interrupted."); }
    }

    private static RunnerAdminException Classify(HttpStatusCode status) =>
        (int)status is 408 or 429 or >= 500
            ? new(RunnerAdminFailure.Transient, "GitHub is temporarily unavailable.")
            : Unexpected();

    private static RunnerAdminException Unexpected() =>
        new(RunnerAdminFailure.Unexpected, "GitHub runner administration response was unexpected.");
}
