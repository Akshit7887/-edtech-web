using EdTechApi.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace EdTechApi.Controllers;

[ApiController]
[Route("api/reports")]
[EnableRateLimiting("ApiPolicy")]
public class ReportsController : ControllerBase
{
    private readonly IReportsService _reportsService;

    public ReportsController(IReportsService reportsService)
    {
        _reportsService = reportsService;
    }

    [HttpPost("send/{examId:int}")]
    public async Task<IActionResult> SendReports(int examId)
    {
        RequireTeacher();
        var result = await _reportsService.SendParentReportsAsync(examId, GetUserId());
        return Ok(new { success = true, message = "Parent reports sent successfully", data = result });
    }

    [HttpGet("pending/{examId:int}")]
    public async Task<IActionResult> GetPending(int examId)
    {
        RequireTeacher();
        var result = await _reportsService.GetPendingParentReportsAsync(examId, GetUserId());
        return Ok(new { success = true, data = result });
    }

    [HttpPost("test-sms")]
    public IActionResult TestSms([FromBody] TestSmsBody body)
    {
        return Ok(new { success = true, message = "SMS logged (no provider configured, use Twilio/ZeptoMail)" });
    }

    [HttpPost("test-email")]
    public IActionResult TestEmail([FromBody] TestEmailBody body)
    {
        return Ok(new { success = true, message = "Email logged (no provider configured, use SMTP/Resend)" });
    }

    private int GetUserId()
    {
        return (int)(HttpContext.Items["UserId"] ?? throw new AppException(401, "Authentication required"));
    }

    private void RequireTeacher()
    {
        var role = HttpContext.Items["UserRole"] as string;
        if (role != "teacher")
            throw new AppException(403, "Access denied. Teacher role required.");
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
