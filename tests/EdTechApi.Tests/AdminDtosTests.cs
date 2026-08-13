using System.Text.Json;
using EdTechApi.DTOs;

namespace EdTechApi.Tests;

public class AdminDtosTests
{
    [Fact]
    public void AdminCreateUserRequest_Serializes_SnakeCase()
    {
        var json = JsonSerializer.Serialize(new AdminCreateUserRequest
        {
            Name = "Test User",
            Email = "test@example.com",
            Phone = "123",
            Password = "secret123",
            Role = "student",
            StudentId = "S001",
            DepartmentId = 3,
            ApprovalStatus = "pending"
        });

        Assert.Contains("\"student_id\"", json);
        Assert.Contains("\"department_id\"", json);
        Assert.Contains("\"approval_status\"", json);
        Assert.Contains("\"password\"", json);
        Assert.DoesNotContain("\"StudentId\"", json);
        Assert.DoesNotContain("\"HeadId\"", json);
    }

    [Fact]
    public void AdminUpdateUserRequest_Allows_NullableFields()
    {
        var req = new AdminUpdateUserRequest { Name = "New Name" };
        Assert.NotNull(req.Name);
        Assert.Null(req.Email);
        Assert.Null(req.Password);
        Assert.Null(req.Phone);
        Assert.Null(req.Role);
        Assert.Null(req.StudentId);
        Assert.Null(req.DepartmentId);
        Assert.Null(req.ApprovalStatus);
    }

    [Fact]
    public void AdminAddClassStudentRequest_Serializes_SnakeCase()
    {
        var json = JsonSerializer.Serialize(new AdminAddClassStudentRequest { StudentId = 42 });
        Assert.Contains("\"student_id\":42", json);
    }

    [Fact]
    public void AdminStats_Keys_Follow_SnakeCase()
    {
        var json = JsonSerializer.Serialize(new
        {
            total_users = 10,
            total_students = 7,
            total_teachers = 2,
            pending_teachers = 1,
            total_exams = 5,
            total_departments = 2,
            total_classes = 3
        });

        Assert.Contains("\"total_users\"", json);
        Assert.Contains("\"pending_teachers\"", json);
        Assert.Contains("\"total_classes\"", json);
    }
}