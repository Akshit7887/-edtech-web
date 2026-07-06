using System.Text.Json;
using EdTechApi.DTOs;

namespace EdTechApi.Services;

public interface IGoogleAuthService
{
    string GetAuthorizationUrl(string role = "student");
    Task<VerifyOtpResponse> HandleCallbackAsync(string code, string role = "student");
}

public class GoogleAuthService : IGoogleAuthService
{
    private readonly IConfiguration _config;
    private readonly IAuthService _auth;
    private readonly HttpClient _http;
    private readonly ILogger<GoogleAuthService> _logger;

    public GoogleAuthService(IConfiguration config, IAuthService auth, HttpClient http, ILogger<GoogleAuthService> logger)
    {
        _config = config;
        _auth = auth;
        _http = http;
        _logger = logger;
    }

    private string GetGoogleConfig(string key, string envVar)
    {
        var val = _config[key];
        if (!string.IsNullOrEmpty(val)) return val;
        return Environment.GetEnvironmentVariable(envVar) ?? "";
    }

    public string GetAuthorizationUrl(string role = "student")
    {
        var clientId = GetGoogleConfig("Google:ClientId", "GOOGLE_CLIENT_ID");
        var redirectUri = GetGoogleConfig("Google:RedirectUri", "GOOGLE_REDIRECT_URI");
        return "https://accounts.google.com/o/oauth2/auth?" +
               $"client_id={Uri.EscapeDataString(clientId)}&" +
               $"redirect_uri={Uri.EscapeDataString(redirectUri)}&" +
               "response_type=code&" +
               $"state={Uri.EscapeDataString(role)}&" +
               "scope=openid%20email%20profile&" +
               "access_type=offline";
    }

    public async Task<VerifyOtpResponse> HandleCallbackAsync(string code, string role = "student")
    {
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
}
