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

    [HttpDelete("users/{userId:int}")]
    public async Task<IActionResult> DeleteUser(int userId)
    {
        using var conn = _db.CreateConnection();
        await conn.ExecuteAsync("DELETE FROM \"Users\" WHERE \"id\" = @Id", new { Id = userId });
        return Ok(new { success = true, message = "User deleted" });
    }
}
