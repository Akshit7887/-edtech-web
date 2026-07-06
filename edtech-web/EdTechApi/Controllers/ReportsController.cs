using EdTechApi.Middleware;
using EdTechApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace EdTechApi.Controllers;

[ApiController]
[Route("api/reports")]
[EnableRateLimiting("ApiPolicy")]
[RequireAuth]
public class ReportsController : ControllerBase
{
    private readonly IReportsService _reportsService;

    public ReportsController(IReportsService reportsService)
    {
        _reportsService = reportsService;
    }

    [RequireRole("teacher")]
    [HttpPost("send/{examId:int}")]
    public async Task<IActionResult> SendReports(int examId)
    {
        var result = await _reportsService.SendParentReportsAsync(examId, GetUserId());
        return Ok(new { success = true, message = "Parent reports sent successfully", data = result });
    }

    [RequireRole("teacher")]
    [HttpGet("pending/{examId:int}")]
    public async Task<IActionResult> GetPending(int examId)
    {
        var result = await _reportsService.GetPendingParentReportsAsync(examId, GetUserId());
        return Ok(new { success = true, data = result });
    }

    [AllowAnonymous]
    [HttpPost("test-sms")]
    public IActionResult TestSms([FromBody] TestSmsBody body)
    {
        return Ok(new { success = true, message = "SMS logged (no provider configured, use Twilio/ZeptoMail)" });
    }

    [AllowAnonymous]
    [HttpPost("test-email")]
    public IActionResult TestEmail([FromBody] TestEmailBody body)
    {
        return Ok(new { success = true, message = "Email logged (no provider configured, use SMTP/Resend)" });
    }

    private int GetUserId()
    {
        return (int)(HttpContext.Items["UserId"] ?? throw new AppException(401, "Authentication required"));
    }
}

public class TestSmsBody
{
    public string PhoneNumber { get; set; } = "";
    public string Message { get; set; } = "";
}

public class TestEmailBody
{
    public string Email { get; set; } = "";
    public string Message { get; set; } = "";
    public string ExamTitle { get; set; } = "";
    public string StudentName { get; set; } = "";
}
