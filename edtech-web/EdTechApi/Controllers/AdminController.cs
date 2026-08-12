using Dapper;
using EdTechApi.Data;
using EdTechApi.DTOs;
using EdTechApi.Middleware;
using EdTechApi.Models;
using EdTechApi.Services;
using Microsoft.AspNetCore.Mvc;

namespace EdTechApi.Controllers;

[ApiController]
[Route("api/admin")]
[RequireRole("admin")]
public class AdminController : ControllerBase
{
    private readonly IDbConnectionFactory _db;
    private readonly IEmailService _email;
    private readonly ILogger<AdminController> _logger;

    public AdminController(IDbConnectionFactory db, IEmailService email, ILogger<AdminController> logger)
    {
        _db = db;
        _email = email;
        _logger = logger;
    }

    [HttpGet("stats")]
    public async Task<IActionResult> GetStats()
    {
        using var conn = _db.CreateConnection();
        var totalUsers = await conn.QuerySingleAsync<int>("SELECT COUNT(*) FROM \"Users\"");
        var totalStudents = await conn.QuerySingleAsync<int>("SELECT COUNT(*) FROM \"Users\" WHERE \"role\" = 'student'");
        var totalTeachers = await conn.QuerySingleAsync<int>("SELECT COUNT(*) FROM \"Users\" WHERE \"role\" = 'teacher'");
        var pendingTeachers = await conn.QuerySingleAsync<int>("SELECT COUNT(*) FROM \"Users\" WHERE \"role\" = 'teacher' AND \"approval_status\" != 'approved'");
        var totalExams = await conn.QuerySingleAsync<int>("SELECT COUNT(*) FROM \"Exams\"");
        var totalDepartments = await conn.QuerySingleAsync<int>("SELECT COUNT(*) FROM \"Departments\"");
        var totalClasses = await conn.QuerySingleAsync<int>("SELECT COUNT(*) FROM \"Classes\"");
        return Ok(new { success = true, data = new { totalUsers, totalStudents, totalTeachers, pendingTeachers, totalExams, totalDepartments, totalClasses } });
    }

    [HttpGet("pending-teachers")]
    public async Task<IActionResult> GetPendingTeachers()
    {
        using var conn = _db.CreateConnection();
        var teachers = await conn.QueryAsync(@"
            SELECT ""id"", ""name"", ""email"", ""phone"", ""approval_status"", ""rejection_reason"", ""created_at""
            FROM ""Users""
            WHERE ""role"" = 'teacher' AND ""approval_status"" != 'approved'
            ORDER BY ""created_at"" DESC");
        return Ok(new { success = true, data = teachers });
    }

    [HttpPost("teachers/{teacherId:int}/approve")]
    public async Task<IActionResult> ApproveTeacher(int teacherId)
    {
        using var conn = _db.CreateConnection();
        var teacher = await conn.QueryFirstOrDefaultAsync<User>(
            "SELECT * FROM \"Users\" WHERE \"id\" = @Id AND \"role\" = 'teacher'", new { Id = teacherId });
        if (teacher == null)
            return NotFound(new { success = false, error = "Teacher not found" });

        await conn.ExecuteAsync(
            @"UPDATE ""Users"" SET ""approval_status"" = 'approved', ""rejection_reason"" = NULL, ""approved_at"" = @Now, ""updated_at"" = @Now WHERE ""id"" = @Id",
            new { Now = DateTime.UtcNow, Id = teacherId });

        await conn.ExecuteAsync(
            @"INSERT INTO ""Notifications"" (""user_id"", ""title"", ""message"", ""type"", ""created_at"")
              VALUES (@UserId, 'Account approved', 'Congratulations! Your teacher account has been approved by the admin. You can now log in.', 'admin', @Now)",
            new { UserId = teacherId, Now = DateTime.UtcNow });

        try
        {
            var html = "<div style='font-family:Arial,sans-serif;max-width:600px;margin:0 auto;'><h2 style='color:#333;'>EdTech Examination App</h2><p>Hello <strong>" + teacher.Name + "</strong>,</p><p>Your teacher account has been <strong style='color:#16a34a;'>approved</strong> by the admin. You can now log in and start using the platform.</p></div>";
            await _email.SendEmailAsync(teacher.Email ?? "", "Your teacher account has been approved - EdTech Examination App", html);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Approval email could not be sent to teacher {Id}", teacherId);
        }

        return Ok(new { success = true, message = "Teacher approved. They can now log in." });
    }

    [HttpPost("teachers/{teacherId:int}/reject")]
    public async Task<IActionResult> RejectTeacher(int teacherId, [FromBody] RejectTeacherRequest? request)
    {
        using var conn = _db.CreateConnection();
        var teacher = await conn.QueryFirstOrDefaultAsync<User>(
            "SELECT * FROM \"Users\" WHERE \"id\" = @Id AND \"role\" = 'teacher'", new { Id = teacherId });
        if (teacher == null)
            return NotFound(new { success = false, error = "Teacher not found" });

        var reason = string.IsNullOrWhiteSpace(request?.RejectionReason) ? null : request.RejectionReason.Trim();
        await conn.ExecuteAsync(
            @"UPDATE ""Users"" SET ""approval_status"" = 'rejected', ""rejection_reason"" = @Reason, ""approved_at"" = NULL, ""updated_at"" = @Now WHERE ""id"" = @Id",
            new { Reason = reason, Now = DateTime.UtcNow, Id = teacherId });

        var message = reason != null
            ? $"Your teacher account was rejected by the admin. Reason: {reason}"
            : "Your teacher account was rejected by the admin.";
        await conn.ExecuteAsync(
            @"INSERT INTO ""Notifications"" (""user_id"", ""title"", ""message"", ""type"", ""created_at"")
              VALUES (@UserId, 'Account rejected', @Message, 'admin', @Now)",
            new { UserId = teacherId, Message = message, Now = DateTime.UtcNow });

        try
        {
            var html = "<div style='font-family:Arial,sans-serif;max-width:600px;margin:0 auto;'><h2 style='color:#333;'>EdTech Examination App</h2><p>Hello <strong>" + teacher.Name + "</strong>,</p><p>Your teacher account was <strong style='color:#dc2626;'>rejected</strong> by the admin." + (reason != null ? " Reason: " + reason : "") + "</p></div>";
            await _email.SendEmailAsync(teacher.Email ?? "", "Update on your teacher account - EdTech Examination App", html);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Rejection email could not be sent to teacher {Id}", teacherId);
        }

        return Ok(new { success = true, message = "Teacher rejected. They will not be able to log in." });
    }

    [HttpGet("users")]
    public async Task<IActionResult> GetUsers([FromQuery] int page = 1, [FromQuery] int limit = 50, [FromQuery] string? role = null, [FromQuery] string? status = null)
    {
        using var conn = _db.CreateConnection();
        var offset = (page - 1) * limit;
        var where = "";
        var parameters = new DynamicParameters();
        if (!string.IsNullOrEmpty(role))
        {
            where = " WHERE \"role\" = @Role";
            parameters.Add("Role", role);
        }
        if (!string.IsNullOrEmpty(status))
        {
            if (string.IsNullOrEmpty(where)) where = " WHERE ";
            else where += " AND ";
            where += "\"approval_status\" = @Status";
            parameters.Add("Status", status);
        }
        var total = await conn.QuerySingleAsync<int>($"SELECT COUNT(*) FROM \"Users\"{where}", parameters);
        var pageParams = new DynamicParameters(parameters);
        pageParams.Add("Limit", limit);
        pageParams.Add("Offset", offset);
        var users = await conn.QueryAsync($@"
            SELECT u.*, d.""name"" AS department_name
            FROM ""Users"" u
            LEFT JOIN ""Departments"" d ON d.""id"" = u.""department_id""
            {where}
            ORDER BY u.""created_at"" DESC
            LIMIT @Limit OFFSET @Offset", pageParams);
        return Ok(new { success = true, data = users, pagination = new { page, limit, total, total_pages = (int)Math.Ceiling((double)total / limit) } });
    }

    [HttpGet("exams")]
    public async Task<IActionResult> GetExams([FromQuery] int page = 1, [FromQuery] int limit = 50)
    {
        using var conn = _db.CreateConnection();
        var offset = (page - 1) * limit;
        var total = await conn.QuerySingleAsync<int>("SELECT COUNT(*) FROM \"Exams\"");
        var exams = await conn.QueryAsync(@"
            SELECT e.*, u.""name"" AS teacher_name
            FROM ""Exams"" e
            LEFT JOIN ""Users"" u ON u.""id"" = e.""teacher_id""
            ORDER BY e.""created_at"" DESC
            LIMIT @Limit OFFSET @Offset", new { Limit = limit, Offset = offset });
        return Ok(new { success = true, data = exams, pagination = new { page, limit, total, total_pages = (int)Math.Ceiling((double)total / limit) } });
    }

    [HttpGet("db-snapshot")]
    public async Task<IActionResult> GetDbSnapshot()
    {
        using var conn = _db.CreateConnection();
        var tasks = new
        {
            users = await conn.QueryAsync("SELECT \"id\", \"name\", \"email\", \"role\", \"created_at\" FROM \"Users\" ORDER BY \"created_at\" DESC LIMIT 20"),
            exams = await conn.QueryAsync("SELECT \"id\", \"title\", \"subject\", \"status\", \"created_at\" FROM \"Exams\" ORDER BY \"created_at\" DESC LIMIT 20"),
            sessions = await conn.QueryAsync("SELECT \"id\", \"student_id\", \"exam_id\", \"score\", \"total_questions\", \"status\", \"submitted_at\", \"created_at\" FROM \"ExamSessions\" ORDER BY \"created_at\" DESC LIMIT 20"),
            assignments = await conn.QueryAsync("SELECT * FROM \"StudentExamAssignments\" ORDER BY \"created_at\" DESC LIMIT 20"),
            notifications = await conn.QueryAsync("SELECT \"id\", \"user_id\", \"title\", \"message\", \"type\", \"is_read\", \"created_at\" FROM \"Notifications\" ORDER BY \"created_at\" DESC LIMIT 20"),
            stats = new
            {
                totalUsers = await conn.QuerySingleAsync<int>("SELECT COUNT(*) FROM \"Users\""),
                totalStudents = await conn.QuerySingleAsync<int>("SELECT COUNT(*) FROM \"Users\" WHERE \"role\" = 'student'"),
                totalTeachers = await conn.QuerySingleAsync<int>("SELECT COUNT(*) FROM \"Users\" WHERE \"role\" = 'teacher'"),
                totalExams = await conn.QuerySingleAsync<int>("SELECT COUNT(*) FROM \"Exams\""),
                totalSessions = await conn.QuerySingleAsync<int>("SELECT COUNT(*) FROM \"ExamSessions\""),
                totalClasses = await conn.QuerySingleAsync<int>("SELECT COUNT(*) FROM \"Classes\""),
                activeExams = await conn.QuerySingleAsync<int>("SELECT COUNT(*) FROM \"Exams\" WHERE \"status\" = 'active'"),
                completedSessions = await conn.QuerySingleAsync<int>("SELECT COUNT(*) FROM \"ExamSessions\" WHERE \"status\" = 'completed'")
            }
        };
        return Ok(new { success = true, data = tasks });
    }

    [HttpGet("classes")]
    public async Task<IActionResult> GetClasses([FromQuery] int page = 1, [FromQuery] int limit = 50)
    {
        using var conn = _db.CreateConnection();
        var offset = (page - 1) * limit;
        var total = await conn.QuerySingleAsync<int>("SELECT COUNT(*) FROM \"Classes\"");
        var classes = await conn.QueryAsync(@"
            SELECT c.*, u.""name"" AS teacher_name,
                   (SELECT COUNT(*) FROM ""ClassStudents"" WHERE ""class_id"" = c.""id"") AS student_count
            FROM ""Classes"" c
            LEFT JOIN ""Users"" u ON u.""id"" = c.""teacher_id""
            ORDER BY c.""created_at"" DESC
            LIMIT @Limit OFFSET @Offset",
            new { Limit = limit, Offset = offset });
        return Ok(new { success = true, data = classes, pagination = new { page, limit, total, total_pages = (int)Math.Ceiling((double)total / limit) } });
    }

    [HttpGet("classes/{classId:int}")]
    public async Task<IActionResult> GetClassDetail(int classId)
    {
        using var conn = _db.CreateConnection();

        var cls = await conn.QueryFirstOrDefaultAsync(@"
            SELECT c.*, u.""name"" AS teacher_name,
                   (SELECT COUNT(*) FROM ""ClassStudents"" WHERE ""class_id"" = c.""id"") AS student_count
            FROM ""Classes"" c
            LEFT JOIN ""Users"" u ON u.""id"" = c.""teacher_id""
            WHERE c.""id"" = @Id", new { Id = classId });

        if (cls == null) return NotFound(new { success = false, error = "Class not found" });

        var students = await conn.QueryAsync(@"
            SELECT u.""id"", u.""name"", u.""email"", u.""student_id"", u.""phone""
            FROM ""Users"" u
            JOIN ""ClassStudents"" cs ON cs.""student_id"" = u.""id""
            WHERE cs.""class_id"" = @ClassId
            ORDER BY u.""name"" ASC", new { ClassId = classId });

        return Ok(new { success = true, data = new { cls = cls, students = students } });
    }

    [HttpDelete("classes/{classId:int}")]
    public async Task<IActionResult> DeleteClass(int classId)
    {
        using var conn = _db.CreateConnection();
        await conn.ExecuteAsync("DELETE FROM \"ClassStudents\" WHERE \"class_id\" = @ClassId", new { ClassId = classId });
        await conn.ExecuteAsync("DELETE FROM \"Classes\" WHERE \"id\" = @Id", new { Id = classId });
        return Ok(new { success = true, message = "Class deleted" });
    }

    [HttpPost("backfill-student-ids")]
    public async Task<IActionResult> BackfillStudentIds()
    {
        using var conn = _db.CreateConnection();
        var studentsWithoutId = (await conn.QueryAsync<User>(
            "SELECT * FROM \"Users\" WHERE \"role\" = 'student' AND \"student_id\" IS NULL")).AsList();
        var count = 0;
        var random = new Random();
        foreach (var student in studentsWithoutId)
        {
            string sid;
            do
            {
                sid = random.Next(0, 1000000000).ToString("D10");
            } while (await conn.QueryFirstOrDefaultAsync<string>(
                "SELECT 1 FROM \"Users\" WHERE \"student_id\" = @Sid", new { Sid = sid }) != null);
            await conn.ExecuteAsync(
                "UPDATE \"Users\" SET \"student_id\" = @Sid WHERE \"id\" = @Id",
                new { Sid = sid, Id = student.Id });
            count++;
        }
        return Ok(new { success = true, message = $"Backfilled {count} student(s)" });
    }

    [HttpDelete("users/{userId:int}")]
    public async Task<IActionResult> DeleteUser(int userId)
    {
        using var conn = _db.CreateConnection();
        await conn.ExecuteAsync("DELETE FROM \"Users\" WHERE \"id\" = @Id", new { Id = userId });
        return Ok(new { success = true, message = "User deleted" });
    }
}
