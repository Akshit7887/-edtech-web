using EdTechApi.Services;
using Microsoft.AspNetCore.Mvc;

namespace EdTechApi.Controllers;

[ApiController]
[Route("auth/google")]
public class GoogleAuthController : ControllerBase
{
    private readonly IGoogleAuthService _googleAuth;
    private readonly IConfiguration _config;
    private readonly ILogger<GoogleAuthController> _logger;

    public GoogleAuthController(IGoogleAuthService googleAuth, IConfiguration config, ILogger<GoogleAuthController> logger)
    {
        _googleAuth = googleAuth;
        _config = config;
        _logger = logger;
    }

    [HttpGet("login")]
    public IActionResult Login([FromQuery] string? role)
    {
        var selectedRole = !string.IsNullOrEmpty(role) && (role == "teacher" || role == "student") ? role : "student";
        var url = _googleAuth.GetAuthorizationUrl(selectedRole);
        return Redirect(url);
    }

    [HttpGet("callback")]
    public async Task<IActionResult> Callback([FromQuery] string code, [FromQuery] string? error, [FromQuery] string? state)
    {
        var frontendFallback = "http://localhost:8081/google-callback.html";
        string GetFrontendUrl()
        {
            var val = _config["Google:FrontendRedirect"];
            if (!string.IsNullOrEmpty(val)) return val;
            return Environment.GetEnvironmentVariable("GOOGLE_FRONTEND_REDIRECT") ?? frontendFallback;
        }

        if (!string.IsNullOrEmpty(error))
        {
            return Redirect($"{GetFrontendUrl()}?error={Uri.EscapeDataString(error)}");
        }

        if (string.IsNullOrEmpty(code))
        {
            return BadRequest(new { success = false, message = "Missing authorization code" });
        }

        try
        {
            var selectedRole = !string.IsNullOrEmpty(state) && (state == "teacher" || state == "student") ? state : "student";
            var result = await _googleAuth.HandleCallbackAsync(code, selectedRole);
            var frontendUrl = GetFrontendUrl();
            var redirectUrl = $"{frontendUrl}?token={Uri.EscapeDataString(result.Token)}" +
                              $"&user_id={result.User.Id}" +
                              $"&name={Uri.EscapeDataString(result.User.Name)}" +
                              $"&role={result.User.Role}" +
                              $"&email={Uri.EscapeDataString(result.User.Email ?? "")}";
            return Redirect(redirectUrl);
        }
        catch (AppException ex)
        {
            var frontend = _config["Google:FrontendRedirect"] ?? "http://localhost:8081/google-callback.html";
            return Redirect($"{frontend}?error={Uri.EscapeDataString(ex.Message)}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error in Google OAuth callback");
            var frontend = _config["Google:FrontendRedirect"] ?? "http://localhost:8081/google-callback.html";
            return Redirect($"{frontend}?error={Uri.EscapeDataString("An unexpected error occurred during Google sign-in")}");
        }
    }
}
