using EdTechApi.DTOs;
using EdTechApi.Middleware;
using EdTechApi.Models;
using EdTechApi.Services;
using Microsoft.AspNetCore.Mvc;

namespace EdTechApi.Controllers;

[ApiController]
[Route("api/teacher")]
[RequireRole("teacher")]
public class TeacherController : ControllerBase
{
    private readonly ITeacherService _teacherService;

    public TeacherController(ITeacherService teacherService)
    {
        _teacherService = teacherService;
    }

    [HttpGet("students")]
    public async Task<IActionResult> GetAllStudents([FromQuery] int page = 1, [FromQuery] int limit = 50)
    {
        var teacherId = GetUserId();
        var result = await _teacherService.GetAllStudentsAsync(teacherId, page, limit);
        return Ok(new { success = true, data = result });
    }

    [HttpPost("students")]
    public async Task<IActionResult> CreateStudent([FromBody] CreateStudentRequest request)
    {
        var teacherId = GetUserId();
        if (string.IsNullOrEmpty(request.Email) || !request.Email.Contains('@'))
            return BadRequest(new { success = false, error = "Valid email is required" });
        if (string.IsNullOrEmpty(request.Name))
            return BadRequest(new { success = false, error = "Name is required" });
        var result = await _teacherService.CreateStudentAsync(teacherId, request.Name, request.Email);
        return Created(string.Empty, new { success = true, data = result });
    }

    [HttpGet("students/search-by-sid")]
    public async Task<IActionResult> SearchStudentBySid([FromQuery] string student_id)
    {
        if (string.IsNullOrEmpty(student_id) || student_id.Length < 3)
            return BadRequest(new { success = false, error = "Enter at least 3 digits of the student ID" });

        var result = await _teacherService.SearchStudentBySidAsync(student_id);
        if (result == null)
            return NotFound(new { success = false, error = "No student found with that ID" });

        return Ok(new { success = true, data = result });
    }

    [HttpGet("students/{studentId:int}")]
    public async Task<IActionResult> GetStudentDetail(int studentId)
    {
        var detail = await _teacherService.GetStudentDetailAsync(studentId);
        return Ok(new { success = true, data = detail });
    }

    [HttpDelete("students/{studentId:int}")]
    public async Task<IActionResult> DeleteStudent(int studentId)
    {
        await _teacherService.DeleteStudentAsync(studentId);
        return Ok(new { success = true, message = "Student deleted" });
    }

    [HttpGet("questions/{examId:int}")]
    public async Task<IActionResult> GetQuestionBank(int examId)
    {
        var teacherId = GetUserId();
        var bank = await _teacherService.GetQuestionBankAsync(examId, teacherId);
        return Ok(new { success = true, data = bank });
    }

    [HttpPost("questions/{examId:int}")]
    public async Task<IActionResult> AddQuestion(int examId, [FromBody] QuestionPool question)
    {
        var teacherId = GetUserId();
        var result = await _teacherService.AddQuestionAsync(examId, teacherId, question);
        return Created(string.Empty, new { success = true, data = result });
    }

    [HttpPut("questions/{questionId:int}")]
    public async Task<IActionResult> UpdateQuestion(int questionId, [FromBody] QuestionPool question)
    {
        var teacherId = GetUserId();
        var result = await _teacherService.UpdateQuestionAsync(questionId, teacherId, question);
        return Ok(new { success = true, data = result });
    }

    [HttpDelete("questions/{questionId:int}")]
    public async Task<IActionResult> DeleteQuestion(int questionId)
    {
        var teacherId = GetUserId();
        await _teacherService.DeleteQuestionAsync(questionId, teacherId);
        return Ok(new { success = true, message = "Question deleted" });
    }

    [HttpPost("classes")]
    public async Task<IActionResult> CreateClass([FromBody] CreateClassRequest request)
    {
        var teacherId = GetUserId();
        var cls = await _teacherService.CreateClassAsync(teacherId, request);
        return Created(string.Empty, new { success = true, data = cls });
    }

    [HttpGet("classes")]
    public async Task<IActionResult> GetClasses()
    {
        var teacherId = GetUserId();
        var classes = await _teacherService.GetClassesAsync(teacherId);
        return Ok(new { success = true, data = classes });
    }

    [HttpGet("classes/{classId:int}")]
    public async Task<IActionResult> GetClassDetail(int classId)
    {
        var teacherId = GetUserId();
        var detail = await _teacherService.GetClassDetailAsync(classId, teacherId);
        return Ok(new { success = true, data = detail });
    }

    [HttpPost("classes/{classId:int}/students")]
    public async Task<IActionResult> AddStudentsToClass(int classId, [FromBody] AddStudentsRequest request)
    {
        var teacherId = GetUserId();
        var result = await _teacherService.AddStudentsToClassAsync(classId, teacherId, request.StudentIds);
        return Ok(new { success = true, data = result });
    }

    [HttpDelete("classes/{classId:int}/students/{studentId:int}")]
    public async Task<IActionResult> RemoveStudentFromClass(int classId, int studentId)
    {
        var teacherId = GetUserId();
        await _teacherService.RemoveStudentFromClassAsync(classId, teacherId, studentId);
        return Ok(new { success = true });
    }

    [HttpDelete("classes/{classId:int}")]
    public async Task<IActionResult> DeleteClass(int classId)
    {
        var teacherId = GetUserId();
        await _teacherService.DeleteClassAsync(classId, teacherId);
        return Ok(new { success = true });
    }

    [HttpPost("announcement")]
    public async Task<IActionResult> SendAnnouncement([FromBody] AnnouncementRequest request)
    {
        var teacherId = GetUserId();
        if (string.IsNullOrEmpty(request.Title) || string.IsNullOrEmpty(request.Message))
            return BadRequest(new { success = false, error = "Title and message are required" });

        var result = await _teacherService.SendAnnouncementAsync(teacherId, request);
        return Ok(new { success = true, data = result });
    }

    [HttpPut("schedule/{examId:int}")]
    public async Task<IActionResult> ScheduleExam(int examId, [FromBody] ScheduleBody body)
    {
        var teacherId = GetUserId();
        if (string.IsNullOrEmpty(body.ScheduledAt))
            return BadRequest(new { success = false, error = "Scheduled date is required" });

        var exam = await _teacherService.ScheduleExamAsync(examId, teacherId, body.ScheduledAt);
        return Ok(new { success = true, data = exam });
    }

    [HttpGet("parent-contacts")]
    public async Task<IActionResult> GetParentContacts()
    {
        var teacherId = GetUserId();
        var contacts = await _teacherService.GetParentContactsAsync(teacherId);
        return Ok(new { success = true, data = contacts });
    }

    [HttpPost("parent-contacts/{studentId:int}")]
    public async Task<IActionResult> CreateParentContact(int studentId, [FromBody] ParentContactRequest request)
    {
        var teacherId = GetUserId();
        var result = await _teacherService.CreateOrUpdateParentContactAsync(studentId, teacherId, request);
        return Ok(new { success = true, data = result });
    }

    [HttpDelete("parent-contacts/{studentId:int}")]
    public async Task<IActionResult> DeleteParentContact(int studentId)
    {
        var teacherId = GetUserId();
        await _teacherService.DeleteParentContactAsync(studentId, teacherId);
        return Ok(new { success = true, message = "Parent contact deleted" });
    }

    [HttpGet("report-history/{examId:int}")]
    public async Task<IActionResult> GetReportHistory(int examId)
    {
        var teacherId = GetUserId();
        var reports = await _teacherService.GetParentReportHistoryAsync(examId, teacherId);
        return Ok(new { success = true, data = reports });
    }

    private int GetUserId()
    {
        return (int)(HttpContext.Items["UserId"] ?? throw new AppException(401, "Authentication required"));
    }
}

public class ScheduleBody
{
    public string ScheduledAt { get; set; } = "";
}

public class CreateStudentRequest
{
    public string Name { get; set; } = "";
    public string Email { get; set; } = "";
}
