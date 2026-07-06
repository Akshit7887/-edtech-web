using EdTechApi.DTOs;
using EdTechApi.Middleware;
using EdTechApi.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace EdTechApi.Controllers;

[ApiController]
[Route("api/students")]
[EnableRateLimiting("ApiPolicy")]
[RequireAuth]
public class StudentController : ControllerBase
{
    private readonly IStudentService _studentService;

    public StudentController(IStudentService studentService)
    {
        _studentService = studentService;
    }

    [HttpGet("analytics/{studentId:int}")]
    public async Task<IActionResult> GetAnalytics(int studentId)
    {
        var userId = GetUserId();
        if (studentId != userId)
            return StatusCode(403, new { success = false, error = "Access denied" });

        var analytics = await _studentService.GetAnalyticsAsync(studentId);
        return Ok(new { success = true, data = analytics });
    }

    [HttpGet("review/{sessionId:int}")]
    public async Task<IActionResult> GetReview(int sessionId)
    {
        var userId = GetUserId();
        var review = await _studentService.GetExamReviewAsync(sessionId, userId);
        return Ok(new { success = true, data = review });
    }

    [HttpPost("practice/start")]
    public async Task<IActionResult> StartPractice([FromBody] PracticeRequest request)
    {
        var userId = GetUserId();
        var result = await _studentService.CreatePracticeSessionAsync(userId, request.ExamId);
        return Ok(new { success = true, data = result });
    }

    [HttpPost("practice/submit")]
    public async Task<IActionResult> SubmitPractice([FromBody] PracticeSubmitRequest request)
    {
        var userId = GetUserId();
        var result = await _studentService.SubmitPracticeAsync(userId, request.ExamId, request.Answers);
        return Ok(new { success = true, data = result });
    }

    [HttpGet("notifications")]
    public async Task<IActionResult> GetNotifications([FromQuery] int page = 1, [FromQuery] int limit = 50)
    {
        var userId = GetUserId();
        var result = await _studentService.GetNotificationsAsync(userId, page, limit);
        return Ok(new { success = true, data = result });
    }

    [HttpPut("notifications/{id:int}/read")]
    public async Task<IActionResult> MarkNotificationRead(int id)
    {
        var userId = GetUserId();
        var notification = await _studentService.MarkNotificationReadAsync(id, userId);
        return Ok(new { success = true, data = notification });
    }

    [HttpPut("notifications/read-all")]
    public async Task<IActionResult> MarkAllNotificationsRead()
    {
        var userId = GetUserId();
        var result = await _studentService.MarkAllNotificationsReadAsync(userId);
        return Ok(new { success = true, data = result });
    }

    private int GetUserId()
    {
        return (int)(HttpContext.Items["UserId"] ?? throw new AppException(401, "Authentication required"));
    }
}
