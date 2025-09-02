using System.Text.Encodings.Web;
using System.Text.Json;
using DivarExtensionDemo.Infrastructure.Divar.Models;
using DivarExtensionDemo.Services.Interfaces;
using StackExchange.Redis;

namespace DivarExtensionDemo.Services;

public sealed class DivarService : IDivarService
{
    private const string StateCacheKey = "sso:states";
    private static string? _fallbackUrl;
    private const string AuthorizationRequestUrl = "https://oauth.divar.ir/oauth2/auth";
    private const string AccessTokenRequestUrl = "https://oauth.divar.ir/oauth2/token";

    private readonly IConfiguration _configuration;
    private readonly IDatabase _database;
    private readonly IHttpClientFactory _httpClientFactory;

    public DivarService(
        IConfiguration configuration,
        IDatabase database,
        IHttpContextAccessor httpContextAccessor,
        IHttpClientFactory httpClientFactory
    )
    {
        _configuration = configuration;
        _database = database;
        _httpClientFactory = httpClientFactory;

        if (httpContextAccessor.HttpContext!.Request.Headers.ContainsKey("X-Frontend-Host"))
        {
            _fallbackUrl = $"{httpContextAccessor.HttpContext!.Request.Headers
                .FirstOrDefault(h => h.Key == "X-Frontend-Host").Value}/auth/token";
        }
        else
        {
            _fallbackUrl = "https://localhost:7203/auth/token";
            //throw new ArgumentException("The host header is null!");
        }
    }

    public async Task<string> GenerateSsoAuthUrlAsync(string postToken, CancellationToken cancellationToken)
    {
        var clientId = _configuration.GetSection("Divar:Extension:ClientId").Value!;

        var state = Guid.CreateVersion7().ToString();
        var states = await _database.StringGetAsync(StateCacheKey);
        List<string> retrievedStates = [];
        if (states.HasValue) retrievedStates = JsonSerializer.Deserialize<List<string>>(states!)!;
        retrievedStates!.Add(state);
        await _database.StringSetAsync(StateCacheKey, JsonSerializer.Serialize(retrievedStates));

        var queries = string.Join('&', new Dictionary<string, string>
        {
            { "response_type", "code" },
            { "redirect_uri", _fallbackUrl! },
            { "client_id", clientId },
            { "scope", "USER_PHONE" },
            { "state", state }
        }.Select(q => $"{q.Key}={q.Value}"));

        return $"{AuthorizationRequestUrl}?{queries}";
    }

    public async Task<string> HandleFallbackAsync(
        string state,
        string code,
        CancellationToken cancellationToken
    )
    {
        var states = await _database.StringGetAsync(StateCacheKey);
        var retrievedStates = JsonSerializer.Deserialize<List<string>>(states!);
        if (!retrievedStates!.Contains(state)) throw new ArgumentException("State is invalid!");

        var divarApiKey = _configuration.GetSection("Divar:Extension:ApiKey").Value!;
        var clientId = _configuration.GetSection("Divar:Extension:ClientId").Value!;
        var clientSecret = _configuration.GetSection("Divar:Extension:ClientSecret").Value!;
        var request = new HttpRequestMessage(HttpMethod.Post, AccessTokenRequestUrl);
        request.Headers.Add("Accept", "application/json");
        request.Headers.Add("X-API-Key", divarApiKey);
        request.Content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            { "grant_type", "authorization_code" },
            { "code", code },
            { "client_id", clientId },
            { "client_secret", clientSecret },
            { "redirect_uri", _fallbackUrl! }
        });
        var client = _httpClientFactory.CreateClient();
        var clientResponse = await client.SendAsync(request, cancellationToken);
        clientResponse.EnsureSuccessStatusCode();

        var responseAsText = await clientResponse.Content.ReadAsStringAsync(cancellationToken);
        var response = JsonSerializer.Deserialize<AccessTokenResponse>(responseAsText, new JsonSerializerOptions
        {
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
            PropertyNameCaseInsensitive = true
        });

        retrievedStates.Remove(state);
        await _database.StringSetAsync(StateCacheKey, JsonSerializer.Serialize(retrievedStates));

        return response!.AccessToken;
    }
}