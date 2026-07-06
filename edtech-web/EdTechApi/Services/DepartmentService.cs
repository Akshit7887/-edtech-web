using Dapper;
using EdTechApi.Data;
using EdTechApi.DTOs;
using EdTechApi.Models;

namespace EdTechApi.Services;

public class DepartmentService : IDepartmentService
{
    private readonly IDbConnectionFactory _db;

    public DepartmentService(IDbConnectionFactory db)
    {
        _db = db;
    }

    public async Task<List<Department>> GetAllAsync()
    {
        using var conn = _db.CreateConnection();
        var depts = await conn.QueryAsync<Department>(@"
            SELECT d.*,
                   u.""name"" AS head_name,
                   (SELECT COUNT(*) FROM ""Users"" WHERE ""department_id"" = d.""id"" AND ""role"" = 'teacher') AS teacher_count,
                   (SELECT COUNT(*) FROM ""Users"" WHERE ""department_id"" = d.""id"" AND ""role"" = 'student') AS student_count
            FROM ""Departments"" d
            LEFT JOIN ""Users"" u ON u.""id"" = d.""head_id""
            ORDER BY d.""name""");
        return depts.ToList();
    }

    public async Task<Department?> GetByIdAsync(int id)
    {
        using var conn = _db.CreateConnection();
        return await conn.QueryFirstOrDefaultAsync<Department>(@"
            SELECT d.*,
                   u.""name"" AS head_name,
                   (SELECT COUNT(*) FROM ""Users"" WHERE ""department_id"" = d.""id"" AND ""role"" = 'teacher') AS teacher_count,
                   (SELECT COUNT(*) FROM ""Users"" WHERE ""department_id"" = d.""id"" AND ""role"" = 'student') AS student_count
            FROM ""Departments"" d
            LEFT JOIN ""Users"" u ON u.""id"" = d.""head_id""
            WHERE d.""id"" = @Id", new { Id = id });
    }

    public async Task<Department> CreateAsync(CreateDepartmentRequest request)
    {
        using var conn = _db.CreateConnection();
        var dept = await conn.QuerySingleAsync<Department>(@"
            INSERT INTO ""Departments"" (""name"", ""description"", ""head_id"")
            VALUES (@Name, @Description, @HeadId)
            RETURNING *", new { request.Name, request.Description, request.HeadId });
        return dept;
    }

    public async Task<Department?> UpdateAsync(int id, UpdateDepartmentRequest request)
    {
        using var conn = _db.CreateConnection();
        var existing = await conn.QueryFirstOrDefaultAsync<Department>(
            "SELECT * FROM \"Departments\" WHERE \"id\" = @Id", new { Id = id });
        if (existing == null) return null;

        var sql = @"UPDATE ""Departments"" SET
            ""name"" = COALESCE(@Name, ""name""),
            ""description"" = COALESCE(@Description, ""description""),
            ""head_id"" = @HeadId,
            ""updated_at"" = NOW()
            WHERE ""id"" = @Id RETURNING *";
        var updated = await conn.QuerySingleAsync<Department>(sql, new
        {
            Id = id,
            Name = request.Name ?? existing.Name,
            Description = request.Description ?? existing.Description,
            HeadId = request.HeadId
        });
        return updated;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        using var conn = _db.CreateConnection();
        await conn.ExecuteAsync(
            "UPDATE \"Users\" SET \"department_id\" = NULL WHERE \"department_id\" = @Id", new { Id = id });
        var rows = await conn.ExecuteAsync(
            "DELETE FROM \"Departments\" WHERE \"id\" = @Id", new { Id = id });
        return rows > 0;
    }

    public async Task AssignUserToDepartmentAsync(int userId, int departmentId)
    {
        using var conn = _db.CreateConnection();
        await conn.ExecuteAsync(
            "UPDATE \"Users\" SET \"department_id\" = @DepartmentId WHERE \"id\" = @UserId",
            new { UserId = userId, DepartmentId = departmentId });
    }

    public async Task RemoveUserFromDepartmentAsync(int userId)
    {
        using var conn = _db.CreateConnection();
        await conn.ExecuteAsync(
            "UPDATE \"Users\" SET \"department_id\" = NULL WHERE \"id\" = @Id", new { Id = userId });
    }

    public async Task<List<Models.User>> GetDepartmentUsersAsync(int departmentId, string? role = null)
    {
        using var conn = _db.CreateConnection();
        var sql = "SELECT * FROM \"Users\" WHERE \"department_id\" = @DepartmentId";
        if (!string.IsNullOrEmpty(role))
            sql += " AND \"role\" = @Role";
        sql += " ORDER BY \"name\"";
        var users = await conn.QueryAsync<Models.User>(sql, new { DepartmentId = departmentId, Role = role });
        return users.ToList();
    }
}
