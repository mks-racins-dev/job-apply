using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

using JobMagnet.Web.Session;

namespace JobMagnet.Web.Api;

public class JobMagnetApiClient(HttpClient http, AuthSession session)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    public Task<ApiResult<TokenResponse>> RegisterAsync(Credentials credentials) =>
        SendAsync<TokenResponse>(HttpMethod.Post, "auth/register", JsonContent.Create(credentials, options: JsonOptions));

    public Task<ApiResult<TokenResponse>> LoginAsync(Credentials credentials) =>
        SendAsync<TokenResponse>(HttpMethod.Post, "auth/login", JsonContent.Create(credentials, options: JsonOptions));

    public Task<ApiResult<AccountInfo>> GetAccountAsync() => SendAsync<AccountInfo>(HttpMethod.Get, "me");

    public Task<ApiResult<object>> SetAvatarAsync(int avatarId) =>
        SendAsync<object>(HttpMethod.Put, "me/avatar", JsonContent.Create(new AvatarUpdate(avatarId), options: JsonOptions));

    public Task<ApiResult<ProfileResponse>> GetProfileAsync() => SendAsync<ProfileResponse>(HttpMethod.Get, "profile");

    public Task<ApiResult<ProfileResponse>> SaveProfileAsync(ProfileData profile) =>
        SendAsync<ProfileResponse>(HttpMethod.Put, "profile", JsonContent.Create(profile, options: JsonOptions));

    public Task<ApiResult<MatchPage>> GetMatchesAsync(MatchQuery query) =>
        SendAsync<MatchPage>(
            HttpMethod.Get,
            $"matches?minScore={query.MinScore}&page={query.Page}&pageSize={query.PageSize}"
                + $"&sort={Uri.EscapeDataString(query.Sort)}&dir={(query.Descending ? "desc" : "asc")}"
                + $"&filter={Uri.EscapeDataString(query.Filter)}");

    public Task<ApiResult<MatchItem>> SubmitJobAsync(SubmitJobRequest request, CancellationToken cancellationToken) =>
        SendAsync<MatchItem>(HttpMethod.Post, "jobs", JsonContent.Create(request, options: JsonOptions), cancellationToken);

    public Task<ApiResult<JobFeed>> GetJobFeedAsync() => SendAsync<JobFeed>(HttpMethod.Get, "jobs/feed");

    public Task<ApiResult<object>> UpdateMatchStatusAsync(Guid matchId, MatchStatus status) =>
        SendAsync<object>(HttpMethod.Patch, $"matches/{matchId}", JsonContent.Create(new StatusUpdate(status), options: JsonOptions));

    private async Task<ApiResult<T>> SendAsync<T>(
        HttpMethod method, string path, HttpContent? content = null, CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(method, path) { Content = content };
        if (session.Token is { } token)
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        HttpResponseMessage response;
        try
        {
            response = await http.SendAsync(request, cancellationToken);
        }
        catch (HttpRequestException)
        {
            return ApiResult<T>.Failure("The Job Magnet API is not reachable.", 0);
        }

        using (response)
        {
            var statusCode = (int)response.StatusCode;
            if (response.StatusCode == HttpStatusCode.Unauthorized && session.IsSignedIn)
            {
                // Expired or invalid token: drop it so the layout sends the user back to sign-in.
                await session.SignOutAsync();
            }

            if (!response.IsSuccessStatusCode)
            {
                return ApiResult<T>.Failure(await ReadErrorAsync(response, cancellationToken), statusCode);
            }

            if (response.StatusCode == HttpStatusCode.NoContent)
            {
                return ApiResult<T>.Success(default, statusCode);
            }

            var value = await response.Content.ReadFromJsonAsync<T>(JsonOptions, cancellationToken);
            return ApiResult<T>.Success(value, statusCode);
        }
    }

    private static async Task<string> ReadErrorAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        var message = response.StatusCode switch
        {
            HttpStatusCode.Unauthorized => "Wrong email or password, or your session expired.",
            HttpStatusCode.NotFound => "Not found.",
            _ => TryReadMessage(body)
        };

        return string.IsNullOrWhiteSpace(message) ? $"Request failed ({(int)response.StatusCode})." : message;
    }

    // The API returns either a bare JSON string or a ValidationProblem with an "errors" map.
    private static string TryReadMessage(string body)
    {
        try
        {
            using var document = JsonDocument.Parse(body);
            var root = document.RootElement;
            if (root.ValueKind == JsonValueKind.String)
            {
                return root.GetString() ?? string.Empty;
            }

            if (root.TryGetProperty("errors", out var errors) && errors.ValueKind == JsonValueKind.Object)
            {
                return string.Join(' ', errors.EnumerateObject()
                    .SelectMany(e => e.Value.EnumerateArray())
                    .Select(m => m.GetString()));
            }

            return root.TryGetProperty("title", out var title) ? title.GetString() ?? string.Empty : string.Empty;
        }
        catch (JsonException)
        {
            return body;
        }
    }
}
