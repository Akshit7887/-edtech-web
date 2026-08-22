using EdTechApi.DTOs;
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
        if (!string.IsNullOrEmpty(error))
        {
            var frontendError = GetFrontendRedirectUrl();
            return Redirect($"{frontendError}?error={Uri.EscapeDataString(error)}");
        }

        if (string.IsNullOrEmpty(code))
        {
            return BadRequest(new { success = false, message = "Missing authorization code" });
        }

        if (string.IsNullOrEmpty(state))
        {
            var frontend = GetFrontendRedirectUrl();
            return Redirect($"{frontend}?error={Uri.EscapeDataString("Invalid OAuth state parameter")}");
        }

        try
        {
            var result = await _googleAuth.HandleCallbackAsync(code, state);
            var frontendUrl = GetFrontendRedirectUrl();
            var redirectUrl = $"{frontendUrl}?token={Uri.EscapeDataString(result.Token)}" +
                              $"&user_id={result.User.Id}" +
                              $"&name={Uri.EscapeDataString(result.User.Name)}" +
                              $"&role={result.User.Role}" +
                              $"&email={Uri.EscapeDataString(result.User.Email ?? "")}";
            return Redirect(redirectUrl);
        }
        catch (AppException ex)
        {
            var frontend = GetFrontendRedirectUrl();
            return Redirect($"{frontend}?error={Uri.EscapeDataString(ex.Message)}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error in Google OAuth callback");
            var frontend = GetFrontendRedirectUrl();
            return Redirect($"{frontend}?error={Uri.EscapeDataString("An unexpected error occurred during Google sign-in")}");
        }
    }

    private string GetFrontendRedirectUrl()
    {
        var val = _config["Google:FrontendRedirect"];
        if (!string.IsNullOrEmpty(val)) return val;
        var envVal = Environment.GetEnvironmentVariable("GOOGLE_FRONTEND_REDIRECT");
        if (!string.IsNullOrEmpty(envVal)) return envVal;
        throw new InvalidOperationException("GOOGLE_FRONTEND_REDIRECT environment variable or Google:FrontendRedirect config must be set");
    }

    [HttpPost("signin")]
    public async Task<IActionResult> SignIn([FromBody] GoogleSignInRequest request)
    {
        try
        {
            var selectedRole = !string.IsNullOrEmpty(request.Role) && (request.Role == "teacher" || request.Role == "student") ? request.Role : "student";
            var result = await _googleAuth.VerifyIdTokenAsync(request.IdToken, selectedRole);
            return Ok(result);
        }
        catch (AppException ex)
        {
            return StatusCode(ex.StatusCode, new { success = false, error = ex.Message });
        }
    }

    [HttpGet("config")]
    public IActionResult GetConfig()
    {
        var clientId = Environment.GetEnvironmentVariable("GOOGLE_CLIENT_ID") ?? _config["Google:ClientId"] ?? "";
        return Ok(new { clientId });
    }
}
