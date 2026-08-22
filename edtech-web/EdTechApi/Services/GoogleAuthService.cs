using System.Security.Cryptography;
using System.Text.Json;
using EdTechApi.DTOs;

namespace EdTechApi.Services;

public interface IGoogleAuthService
{
    string GetAuthorizationUrl(string role = "student");
    Task<VerifyOtpResponse> HandleCallbackAsync(string code, string state);
    Task<VerifyOtpResponse> VerifyIdTokenAsync(string idToken, string role = "student");
}

public class GoogleAuthService : IGoogleAuthService
{
    private readonly IConfiguration _config;
    private readonly IAuthService _auth;
    private readonly HttpClient _http;
    private readonly ILogger<GoogleAuthService> _logger;
    private readonly IRedisCacheService _cache;
    private static readonly TimeSpan StateExpiry = TimeSpan.FromMinutes(10);

    public GoogleAuthService(IConfiguration config, IAuthService auth, HttpClient http, ILogger<GoogleAuthService> logger, IRedisCacheService cache)
    {
        _config = config;
        _auth = auth;
        _http = http;
        _logger = logger;
        _cache = cache;
    }

    private string GetGoogleConfig(string key, string envVar)
    {
        var val = _config[key];
        if (!string.IsNullOrEmpty(val)) return val;
        return Environment.GetEnvironmentVariable(envVar) ?? "";
    }

    private string GenerateSecureState(string role)
    {
        var bytes = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(bytes);
        var state = Convert.ToBase64String(bytes).Replace("+", "-").Replace("/", "_").Replace("=", "");
        var stateData = $"{state}:{role}";
        return stateData;
    }

    private (string state, string role) ParseState(string stateData)
    {
        var parts = stateData.Split(':', 2);
        if (parts.Length != 2) return (stateData, "student");
        return (parts[0], parts[1]);
    }

    public string GetAuthorizationUrl(string role = "student")
    {
        var clientId = GetGoogleConfig("Google:ClientId", "GOOGLE_CLIENT_ID");
        var redirectUri = GetGoogleConfig("Google:RedirectUri", "GOOGLE_REDIRECT_URI");
        var stateData = GenerateSecureState(role);
        
        // Store state with expiry for validation
        var stateKey = $"oauth_state:{stateData}";
        _ = Task.Run(async () =>
        {
            if (_cache.IsConnected)
            {
                await _cache.SetAsync(stateKey, role, StateExpiry);
            }
        });
        
        return "https://accounts.google.com/o/oauth2/auth?" +
               $"client_id={Uri.EscapeDataString(clientId)}&" +
               $"redirect_uri={Uri.EscapeDataString(redirectUri)}&" +
               "response_type=code&" +
               $"state={Uri.EscapeDataString(stateData)}&" +
               "scope=openid%20email%20profile&" +
               "access_type=offline";
    }

    public async Task<VerifyOtpResponse> HandleCallbackAsync(string code, string state)
    {
        // Validate state
        var stateKey = $"oauth_state:{state}";
        string? role = null;
        if (_cache.IsConnected)
        {
            role = await _cache.GetAsync<string>(stateKey);
            if (role != null)
            {
                await _cache.RemoveAsync(stateKey);
            }
        }
        
        if (string.IsNullOrEmpty(role))
        {
            // Fallback: parse role from state if cache missed
            var parsed = ParseState(state);
            role = parsed.role;
        }
        
        if (role != "teacher" && role != "student")
            role = "student";

        var clientId = GetGoogleConfig("Google:ClientId", "GOOGLE_CLIENT_ID");
        var clientSecret = GetGoogleConfig("Google:ClientSecret", "GOOGLE_CLIENT_SECRET");
        var redirectUri = GetGoogleConfig("Google:RedirectUri", "GOOGLE_REDIRECT_URI");

        var tokenParams = new Dictionary<string, string>
        {
            { "code", code },
            { "client_id", clientId },
            { "client_secret", clientSecret },
            { "redirect_uri", redirectUri },
            { "grant_type", "authorization_code" }
        };

        var tokenRes = await _http.PostAsync("https://oauth2.googleapis.com/token",
            new FormUrlEncodedContent(tokenParams));

        if (!tokenRes.IsSuccessStatusCode)
        {
            var err = await tokenRes.Content.ReadAsStringAsync();
            _logger.LogError("Google token exchange failed: {Error}", err);
            throw new AppException(401, "Failed to authenticate with Google");
        }

        var tokenJson = await tokenRes.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(tokenJson);
        var idToken = doc.RootElement.GetProperty("id_token").GetString() ?? "";

        // Verify the ID token using Google's tokeninfo endpoint
        var infoRes = await _http.GetAsync($"https://oauth2.googleapis.com/tokeninfo?id_token={idToken}");
        if (!infoRes.IsSuccessStatusCode)
        {
            throw new AppException(401, "Invalid Google token");
        }

        var infoJson = await infoRes.Content.ReadAsStringAsync();
        using var infoDoc = JsonDocument.Parse(infoJson);
        var root = infoDoc.RootElement;

        var email = root.GetProperty("email").GetString() ?? "";
        var name = root.TryGetProperty("name", out var n) ? n.GetString() : null;
        name ??= root.TryGetProperty("given_name", out var gn) ? gn.GetString() ?? "User" : email.Split('@')[0];
        var googleSub = root.GetProperty("sub").GetString() ?? "";

        return await _auth.ExternalAuthSessionAsync(email, name, role, googleSub);
    }

    public async Task<VerifyOtpResponse> VerifyIdTokenAsync(string idToken, string role = "student")
    {
        var infoRes = await _http.GetAsync($"https://oauth2.googleapis.com/tokeninfo?id_token={idToken}");
        if (!infoRes.IsSuccessStatusCode)
        {
            throw new AppException(401, "Invalid Google token");
        }

        var infoJson = await infoRes.Content.ReadAsStringAsync();
        using var infoDoc = JsonDocument.Parse(infoJson);
        var root = infoDoc.RootElement;

        var email = root.GetProperty("email").GetString() ?? "";
        var name = root.TryGetProperty("name", out var n) ? n.GetString() : null;
        name ??= root.TryGetProperty("given_name", out var gn) ? gn.GetString() ?? "User" : email.Split('@')[0];
        var googleSub = root.GetProperty("sub").GetString() ?? "";

        return await _auth.ExternalAuthSessionAsync(email, name, role, googleSub);
    }
}
