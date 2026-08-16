using Dapper;
using EdTechApi.Data;
using EdTechApi.DTOs;
using EdTechApi.Middleware;
using EdTechApi.Models;
using EdTechApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EdTechApi.Controllers;

[ApiController]
[Route("api/questions")]
[RequireAuth]
public class QuestionController : ControllerBase
{
    private readonly IQuestionService _questionService;
    private readonly IGeminiService _geminiService;
    private readonly IDbConnectionFactory _db;

    public QuestionController(IQuestionService questionService, IGeminiService geminiService, IDbConnectionFactory db)
    {
        _questionService = questionService;
        _geminiService = geminiService;
        _db = db;
    }

    [RequireRole("teacher")]
    [HttpPost("generate")]
    public async Task<IActionResult> GenerateQuestions([FromBody] GenerateQuestionsRequest request)
    {
        var exam = await _questionService.GetExamForTeacherAsync(request.ExamId, GetUserId());
        if (exam == null)
            return NotFound(new { success = false, error = "Exam not found or access denied" });

        var result = await _geminiService.GenerateQuestionsFromText(request.SyllabusText ?? exam.SyllabusText ?? "", request.QuestionCount, request.Difficulty ?? "medium");
        if (result.Count == 0)
            return BadRequest(new { success = false, error = "Failed to generate questions, please try again" });

        using var conn = _db.CreateConnection();
        var now = DateTime.UtcNow;
        var savedIds = new List<int>();
        foreach (var q in result)
        {
            var saved = await conn.QuerySingleAsync<QuestionPool>(
                @"INSERT INTO ""QuestionPool"" (""exam_id"", ""question_text"", ""option_a"", ""option_b"", ""option_c"", ""option_d"", ""correct_answer"", ""difficulty"", ""points"", ""status"", ""created_at"", ""updated_at"")
                  VALUES (@ExamId, @QuestionText, @OptionA, @OptionB, @OptionC, @OptionD, @CorrectAnswer, @Difficulty, 1, 'pending', @Now, @Now) RETURNING *",
                new
                {
                    ExamId = request.ExamId,
                    QuestionText = q.question_text,
                    OptionA = q.option_a,
                    OptionB = q.option_b,
                    OptionC = q.option_c,
                    OptionD = q.option_d,
                    CorrectAnswer = q.correct_answer,
                    Difficulty = q.difficulty ?? request.Difficulty ?? "medium",
                    Now = now
                });
            savedIds.Add(saved.Id);
        }

        return Ok(new
        {
            success = true,
            message = $"Generated {savedIds.Count} questions and saved them as draft. Use Edit Questions to review and publish them.",
            data = new { questions = result, count = savedIds.Count, status = "pending" }
        });
    }

    [RequireRole("teacher")]
    [HttpPost("assign")]
    public async Task<IActionResult> AssignQuestions([FromBody] AssignQuestionsBody request)
    {
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
        var userId = GetUserId();
        var role = HttpContext.Items["UserRole"] as string ?? "";
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

    [RequireRole("teacher")]
    [HttpGet("statistics/{examId:int}")]
    public async Task<IActionResult> GetStatistics(int examId)
    {
        var teacherId = GetUserId();
        var stats = await _questionService.GetExamStatisticsAsync(examId, teacherId);
        return Ok(new { success = true, data = stats });
    }

    [RequireRole("teacher")]
    [HttpPost("generate-personalized")]
    public async Task<IActionResult> GeneratePersonalized([FromBody] PersonalizedQuestionsRequest request)
    {
        var teacherId = GetUserId();
        var count = request.QuestionCount > 0 ? request.QuestionCount : 10;
        var result = await _questionService.GenerateAndAssignPersonalizedQuestionsAsync(request.ExamId, teacherId, count, request.Difficulty ?? "medium");
        return Ok(new { success = true, message = $"Generated {result} personalized questions", data = result });
    }

    [HttpGet("my-results/{studentId:int}")]
    public async Task<IActionResult> GetMyResults(int studentId)
    {
        var userId = GetUserId();
        var results = await _questionService.GetStudentResultsAsync(studentId);
        return Ok(new { success = true, data = results });
    }

    private int GetUserId()
    {
        return (int)(HttpContext.Items["UserId"] ?? throw new AppException(401, "Authentication required"));
    }
}

public class AssignQuestionsBody
{
    public int ExamId { get; set; }
    public List<int> StudentIds { get; set; } = new();
}
