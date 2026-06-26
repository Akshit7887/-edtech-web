using EdTechApi.DTOs;
using EdTechApi.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace EdTechApi.Controllers;

[ApiController]
[Route("api/questions")]
[EnableRateLimiting("ApiPolicy")]
public class QuestionController : ControllerBase
{
    private readonly IQuestionService _questionService;
    private readonly IGeminiService _geminiService;

    public QuestionController(IQuestionService questionService, IGeminiService geminiService)
    {
        _questionService = questionService;
        _geminiService = geminiService;
    }

    [HttpPost("generate")]
    public async Task<IActionResult> GenerateQuestions([FromBody] GenerateQuestionsRequest request)
    {
        RequireTeacher();
        var result = await _geminiService.GenerateQuestionsFromText(request.SyllabusText ?? "", request.QuestionCount, request.Difficulty ?? "medium");
        return Ok(new { success = true, message = "Questions generated successfully", data = new { questions = result, count = result.Count } });
    }

    [HttpPost("assign")]
    public async Task<IActionResult> AssignQuestions([FromBody] AssignQuestionsBody request)
    {
        RequireTeacher();
        if (request.StudentIds == null || request.StudentIds.Count == 0)
            return BadRequest(new { success = false, error = "At least one student ID is required" });

        var result = await _questionService.AssignQuestionsToStudentsAsync(request.ExamId, request.StudentIds);
        return Ok(new { success = true, data = new { assignments = result } });
    }

    [HttpPost("create-session")]
    public async Task<IActionResult> CreateSession([FromBody] StartExamSessionRequest request)
    {
        var userId = GetUserId();
        if (request.StudentId != userId)
            return StatusCode(403, new { success = false, error = "Access denied" });

        var session = await _questionService.CreateExamSessionAsync(request.StudentId, request.ExamId, request.IpAddress ?? "0.0.0.0", request.UserAgent ?? "mobile");
        return Ok(new { success = true, message = "Session created", data = session });
    }

    [HttpPost("submit")]
    public async Task<IActionResult> SubmitExam([FromBody] SubmitExamRequest request)
    {
        var result = await _questionService.SubmitExamAnswersAsync(request.SessionId, request.Answers);
        return Ok(new { success = true, message = "Exam submitted", data = result });
    }

    [HttpGet("session/{studentId:int}/{examId:int}")]
    public async Task<IActionResult> GetSession(int studentId, int examId)
    {
        var (userId, role) = GetUserInfo();
        if (studentId != userId && role != "teacher")
            return StatusCode(403, new { success = false, error = "Access denied" });

        var session = await _questionService.GetExamSessionAsync(studentId, examId);
        return Ok(new { success = true, data = session });
    }

    [HttpPost("disqualify/{sessionId:int}")]
    public async Task<IActionResult> Disqualify(int sessionId, [FromBody] DisqualifyRequest request)
    {
        var result = await _questionService.DisqualifySessionAsync(sessionId, request.Reason ?? "Disqualified by system");
        return Ok(new { success = true, message = "Session disqualified", data = result });
    }

    [HttpGet("statistics/{examId:int}")]
    public async Task<IActionResult> GetStatistics(int examId)
    {
        var teacherId = RequireTeacher();
        var stats = await _questionService.GetExamStatisticsAsync(examId, teacherId);
        return Ok(new { success = true, data = stats });
    }

    [HttpPost("generate-personalized")]
    public async Task<IActionResult> GeneratePersonalized([FromBody] PersonalizedQuestionsRequest request)
    {
        var teacherId = RequireTeacher();
        var count = request.QuestionCount > 0 ? request.QuestionCount : 10;
        var result = await _questionService.GenerateAndAssignPersonalizedQuestionsAsync(request.ExamId, teacherId, count, request.Difficulty ?? "medium");
        return Ok(new { success = true, message = $"Generated {result} personalized questions", data = result });
    }

    [HttpGet("my-results/{studentId:int}")]
    public async Task<IActionResult> GetMyResults(int studentId)
    {
        var (userId, role) = GetUserInfo();
        var session = await _questionService.GetExamSessionAsync(studentId, 0);
        return Ok(new { success = true, data = session });
    }

    private (int userId, string role) GetUserInfo()
    {
        return ((int)(HttpContext.Items["UserId"] ?? 0), HttpContext.Items["UserRole"] as string ?? "");
    }

    private int GetUserId()
    {
        return (int)(HttpContext.Items["UserId"] ?? throw new AppException(401, "Authentication required"));
    }

    private int RequireTeacher()
    {
        var role = HttpContext.Items["UserRole"] as string;
        if (role != "teacher")
            throw new AppException(403, "Access denied. Teacher role required.");
        return (int)(HttpContext.Items["UserId"] ?? throw new AppException(401, "Authentication required"));
    }
}

public class AssignQuestionsBody
{
    public int ExamId { get; set; }
    public List<int> StudentIds { get; set; } = new();
}
