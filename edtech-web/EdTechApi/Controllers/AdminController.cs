using Dapper;
using EdTechApi.Data;
using EdTechApi.Middleware;
using Microsoft.AspNetCore.Mvc;

namespace EdTechApi.Controllers;

[ApiController]
[Route("api/admin")]
[RequireRole("admin")]
public class AdminController : ControllerBase
{
    private readonly IDbConnectionFactory _db;

    public AdminController(IDbConnectionFactory db)
    {
        _db = db;
    }

    [HttpGet("stats")]
    public async Task<IActionResult> GetStats()
    {
        using var conn = _db.CreateConnection();
        var totalUsers = await conn.QuerySingleAsync<int>("SELECT COUNT(*) FROM \"Users\"");
        var totalStudents = await conn.QuerySingleAsync<int>("SELECT COUNT(*) FROM \"Users\" WHERE \"role\" = 'student'");
        var totalTeachers = await conn.QuerySingleAsync<int>("SELECT COUNT(*) FROM \"Users\" WHERE \"role\" = 'teacher'");
        var totalExams = await conn.QuerySingleAsync<int>("SELECT COUNT(*) FROM \"Exams\"");
        var totalDepartments = await conn.QuerySingleAsync<int>("SELECT COUNT(*) FROM \"Departments\"");
        var totalClasses = await conn.QuerySingleAsync<int>("SELECT COUNT(*) FROM \"Classes\"");
        return Ok(new { success = true, data = new { totalUsers, totalStudents, totalTeachers, totalExams, totalDepartments, totalClasses } });
    }

    [HttpGet("users")]
    public async Task<IActionResult> GetUsers([FromQuery] int page = 1, [FromQuery] int limit = 50, [FromQuery] string? role = null)
    {
        using var conn = _db.CreateConnection();
        var offset = (page - 1) * limit;
        var where = "";
        if (!string.IsNullOrEmpty(role))
            where = " WHERE \"role\" = @Role";
        var total = await conn.QuerySingleAsync<int>($"SELECT COUNT(*) FROM \"Users\"{where}", new { Role = role });
        var users = await conn.QueryAsync($@"
            SELECT u.*, d.""name"" AS department_name
            FROM ""Users"" u
            LEFT JOIN ""Departments"" d ON d.""id"" = u.""department_id""
            {where}
            ORDER BY u.""created_at"" DESC
            LIMIT @Limit OFFSET @Offset", new { Role = role, Limit = limit, Offset = offset });
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

    [HttpDelete("users/{userId:int}")]
    public async Task<IActionResult> DeleteUser(int userId)
    {
        using var conn = _db.CreateConnection();
        await conn.ExecuteAsync("DELETE FROM \"Users\" WHERE \"id\" = @Id", new { Id = userId });
        return Ok(new { success = true, message = "User deleted" });
    }
}
