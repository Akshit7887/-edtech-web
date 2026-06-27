using EdTechApi.Services;
using Microsoft.AspNetCore.Mvc;

namespace EdTechApi.Controllers;

[ApiController]
[Route("auth/google")]
public class GoogleAuthController : ControllerBase
{
    private readonly IGoogleAuthService _googleAuth;
    private readonly IConfiguration _config;

    public GoogleAuthController(IGoogleAuthService googleAuth, IConfiguration config)
    {
        _googleAuth = googleAuth;
        _config = config;
    }

    [HttpGet("login")]
    public IActionResult Login()
    {
        var url = _googleAuth.GetAuthorizationUrl();
        return Redirect(url);
    }

    [HttpGet("callback")]
    public async Task<IActionResult> Callback([FromQuery] string code, [FromQuery] string? error)
    {
        var frontendFallback = "http://localhost:8081/supabase-callback.html";
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
            var result = await _googleAuth.HandleCallbackAsync(code);
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
            var frontend = _config["Google:FrontendRedirect"] ?? "http://localhost:8081/supabase-callback.html";
            return Redirect($"{frontend}?error={Uri.EscapeDataString(ex.Message)}");
        }
    }
}
