using EdTechApi.DTOs;
using EdTechApi.Middleware;
using EdTechApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace EdTechApi.Controllers;

[ApiController]
[Route("api/exams")]
[EnableRateLimiting("ApiPolicy")]
[RequireAuth]
public class ExamController : ControllerBase
{
    private readonly IExamService _examService;
    private readonly IQuestionService _questionService;
    private readonly IGeminiService _geminiService;

    public ExamController(IExamService examService, IQuestionService questionService, IGeminiService geminiService)
    {
        _examService = examService;
        _questionService = questionService;
        _geminiService = geminiService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAllExams([FromQuery] int page = 1, [FromQuery] int limit = 20)
    {
        var userId = GetUserId();
        var role = HttpContext.Items["UserRole"] as string ?? "";
        var result = await _examService.GetAllExamsAsync(userId, role, page, limit);
        return Ok(new { success = true, data = result.Data, pagination = result.Pagination });
    }

    [AllowAnonymous]
    [HttpGet("deep-link/{code}")]
    public async Task<IActionResult> ResolveDeepLink(string code)
    {
        var exam = await _examService.ResolveDeepLinkAsync(code);
        return Ok(new { success = true, data = exam });
    }

    [RequireRole("teacher")]
    [HttpPost("ai-create")]
    public async Task<IActionResult> AiCreateExam([FromBody] AiCreateExamRequest request)
    {
        var teacherId = GetUserId();

        if (string.IsNullOrWhiteSpace(request.Subject))
            return BadRequest(new { success = false, error = "Subject is required" });
        if (string.IsNullOrWhiteSpace(request.Topic))
            return BadRequest(new { success = false, error = "Topic is required" });

        var count = request.QuestionCount > 0 ? request.QuestionCount : 10;
        var generated = await _geminiService.GenerateFullExam(request.Subject.Trim(), request.Topic.Trim(), count, request.Difficulty ?? "medium");

        var exam = await _examService.CreateExamAsync(new CreateExamRequest
        {
            Title = generated.title,
            Subject = request.Subject.Trim(),
            SyllabusText = generated.syllabusText,
            DurationMinutes = 30,
            TotalQuestions = generated.questions.Count,
            Status = "draft"
        }, teacherId);

        return Created(string.Empty, new { success = true, message = $"Exam \"{generated.title}\" created with {generated.questions.Count} AI-generated questions" });
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetExamById(int id, [FromQuery] int questionOffset = 0, [FromQuery] int questionLimit = 100)
    {
        var userId = GetUserId();
        var role = HttpContext.Items["UserRole"] as string ?? "";
        var exam = await _examService.GetExamByIdAsync(id, userId, role, questionOffset, questionLimit);
        return Ok(new { success = true, data = exam });
    }

    [RequireRole("teacher")]
    [HttpPost]
    public async Task<IActionResult> CreateExam([FromBody] CreateExamRequest request)
    {
        var teacherId = GetUserId();
        var exam = await _examService.CreateExamAsync(request, teacherId);
        return Created(string.Empty, new { success = true, message = "Exam created successfully", data = exam });
    }

    [RequireRole("teacher")]
    [HttpPut("{id:int}")]
    public async Task<IActionResult> UpdateExam(int id, [FromBody] UpdateExamRequest request)
    {
        var teacherId = GetUserId();
        var exam = await _examService.UpdateExamAsync(id, request, teacherId);
        return Ok(new { success = true, message = "Exam updated successfully", data = exam });
    }

    [RequireRole("teacher")]
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteExam(int id)
    {
        var teacherId = GetUserId();
        await _examService.DeleteExamAsync(id, teacherId);
        return Ok(new { success = true, message = "Exam deleted successfully" });
    }

    [RequireRole("teacher")]
    [HttpPost("{id:int}/activate")]
    public async Task<IActionResult> ActivateExam(int id)
    {
        var teacherId = GetUserId();
        var exam = await _examService.ActivateExamAsync(id, teacherId);
        return Ok(new { success = true, message = "Exam activated successfully", data = exam });
    }

    [RequireRole("teacher")]
    [HttpGet("{id}/deep-link")]
    public async Task<IActionResult> GenerateDeepLink(int id)
    {
        var teacherId = GetUserId();
        var deepLink = await _examService.GenerateDeepLinkAsync(id);
        return Ok(new { success = true, data = new { deepLink } });
    }

    [RequireRole("teacher")]
    [HttpGet("{id}/statistics")]
    public async Task<IActionResult> GetStatistics(int id)
    {
        var teacherId = GetUserId();
        var stats = await _questionService.GetExamStatisticsAsync(id, teacherId);
        return Ok(new { success = true, data = stats });
    }

    [RequireRole("teacher")]
    [HttpPost("{id}/publish-questions")]
    public async Task<IActionResult> PublishQuestions(int id)
    {
        var teacherId = GetUserId();
        var result = await _examService.PublishQuestionsAsync(id, teacherId);
        return Ok(new { success = true, data = result });
    }

    [RequireRole("teacher")]
    [HttpPost("{id}/bulk-import")]
    public async Task<IActionResult> BulkImport(int id, [FromBody] BulkImportRequest request)
    {
        var teacherId = GetUserId();
        if (string.IsNullOrEmpty(request.CsvText))
            return BadRequest(new { success = false, error = "CSV text is required" });

        var result = await _examService.BulkImportStudentsAsync(id, request.CsvText);
        return Ok(new { success = true, data = result });
    }

    [RequireRole("teacher")]
    [HttpGet("{id}/export-pdf")]
    public async Task<IActionResult> ExportPdf(int id)
    {
        var teacherId = GetUserId();
        var pdfBytes = await _examService.ExportResultsPdfAsync(id, teacherId);
        return File(pdfBytes, "application/pdf", $"exam-{id}-results.pdf");
    }

    [RequireRole("teacher")]
    [HttpGet("{id}/attendance")]
    public async Task<IActionResult> GetAttendance(int id)
    {
        var teacherId = GetUserId();
        var report = await _examService.GetAttendanceReportAsync(id, teacherId);
        return Ok(new { success = true, data = report });
    }

    private int GetUserId()
    {
        return (int)(HttpContext.Items["UserId"] ?? throw new AppException(401, "Authentication required"));
    }
}
