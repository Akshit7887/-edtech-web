using System.Threading.RateLimiting;
using EdTechApi.DTOs;
using EdTechApi.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace EdTechApi.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("generate-otp")]
    [EnableRateLimiting("AuthPolicy")]
    public async Task<IActionResult> GenerateOtp([FromBody] GenerateOtpRequest request)
    {
        var result = await _authService.GenerateOtpAsync(request.Identifier, request.Role, request.Password);
        return Ok(new { success = true, message = result.Message, data = new { result.UserId, result.Identifier, result.OtpCode } });
    }

    [HttpPost("verify-otp")]
    [EnableRateLimiting("OtpPolicy")]
    public async Task<IActionResult> VerifyOtp([FromBody] VerifyOtpRequest request)
    {
        var result = await _authService.VerifyOtpAsync(request.Identifier, request.OtpCode, request.Role);
        return Ok(new { success = true, message = "OTP verified successfully", data = result });
    }

    [HttpPost("send-register-otp")]
    [EnableRateLimiting("AuthPolicy")]
    public async Task<IActionResult> SendRegisterOtp([FromBody] RegisterRequest request)
    {
        var result = await _authService.SendRegisterOtpAsync(request.Name, request.Identifier, request.Password, request.Role);
        return Ok(result);
    }

    [HttpPost("verify-register-otp")]
    [EnableRateLimiting("OtpPolicy")]
    public async Task<IActionResult> VerifyRegisterOtp([FromBody] VerifyRegisterOtpRequest request)
    {
        var result = await _authService.VerifyRegisterOtpAsync(request.Identifier, request.OtpCode);
        return Created(string.Empty, new { success = true, message = "Account created and verified successfully", data = result });
    }

    [HttpPost("forgot-password")]
    [EnableRateLimiting("AuthPolicy")]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
    {
        var result = await _authService.ForgotPasswordAsync(request.Identifier);
        return Ok(result);
    }

    [HttpPost("reset-password")]
    [EnableRateLimiting("OtpPolicy")]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
    {
        var result = await _authService.ResetPasswordAsync(request.Identifier, request.OtpCode, request.NewPassword);
        return Ok(result);
    }

    [HttpPost("refresh-token")]
    [EnableRateLimiting("ApiPolicy")]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest request)
    {
        var result = await _authService.RefreshTokenAsync(request.Token);
        return Ok(new { success = true, data = result });
    }

    [HttpPut("profile")]
    [EnableRateLimiting("ApiPolicy")]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequest request)
    {
        var userId = GetUserId();
        var result = await _authService.UpdateProfileAsync(userId, request);
        return Ok(new { success = true, message = "Profile updated successfully", data = result });
    }

    [HttpPost("change-password")]
    [EnableRateLimiting("ApiPolicy")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
    {
        var userId = GetUserId();
        var result = await _authService.ChangePasswordAsync(userId, request.CurrentPassword, request.NewPassword);
        return Ok(result);
    }

    [HttpDelete("profile")]
    [EnableRateLimiting("ApiPolicy")]
    public async Task<IActionResult> DeleteProfile()
    {
        var userId = GetUserId();
        await _authService.DeleteProfileAsync(userId);
        return Ok(new { success = true, message = "Profile deleted successfully" });
    }

    [HttpPost("external-session")]
    [EnableRateLimiting("AuthPolicy")]
    public async Task<IActionResult> ExternalSession([FromBody] ExternalAuthRequest request)
    {
        var result = await _authService.ExternalAuthSessionAsync(request.Email, request.Name, request.Role, request.ExternalUserId);
        return Ok(result);
    }

    private int GetUserId()
    {
        return (int)(HttpContext.Items["UserId"] ?? throw new AppException(401, "Authentication required"));
    }
}
