using Dapper;
using EdTechApi.Data;
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
    private readonly IDbConnectionFactory _db;

    public ReportsController(IReportsService reportsService, IDbConnectionFactory db)
    {
        _reportsService = reportsService;
        _db = db;
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

    [HttpPost("test-sms")]
    public async Task<IActionResult> TestSms([FromBody] TestSmsBody body)
    {
        var userId = GetUserId();
        using var conn = _db.CreateConnection();
        var now = DateTime.UtcNow;
        await conn.ExecuteAsync(
            @"INSERT INTO ""Notifications"" (""user_id"", ""title"", ""message"", ""type"", ""created_at"")
              VALUES (@UserId, @Title, @Message, @Type, @CreatedAt)",
            new { UserId = userId, Title = "Test SMS", Message = body.Message, Type = "sms", CreatedAt = now });
        return Ok(new { success = true, message = "Test SMS notification sent" });
    }

    [HttpPost("test-email")]
    public async Task<IActionResult> TestEmail([FromBody] TestEmailBody body)
    {
        var userId = GetUserId();
        using var conn = _db.CreateConnection();
        var now = DateTime.UtcNow;
        await conn.ExecuteAsync(
            @"INSERT INTO ""Notifications"" (""user_id"", ""title"", ""message"", ""type"", ""created_at"")
              VALUES (@UserId, @Title, @Message, @Type, @CreatedAt)",
            new { UserId = userId, Title = "Test Email", Message = body.Message, Type = "email", CreatedAt = now });
        return Ok(new { success = true, message = "Test email notification sent" });
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
