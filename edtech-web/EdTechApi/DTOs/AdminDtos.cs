using System.Text.Json.Serialization;

namespace EdTechApi.DTOs;

public class AdminCreateUserRequest
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("email")]
    public string Email { get; set; } = string.Empty;

    [JsonPropertyName("phone")]
    public string? Phone { get; set; }

    [JsonPropertyName("password")]
    public string Password { get; set; } = string.Empty;

    [JsonPropertyName("role")]
    public string Role { get; set; } = "student";

    [JsonPropertyName("student_id")]
    public string? StudentId { get; set; }

    [JsonPropertyName("department_id")]
    public int? DepartmentId { get; set; }

    [JsonPropertyName("approval_status")]
    public string? ApprovalStatus { get; set; }
}

public class AdminUpdateUserRequest
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("email")]
    public string? Email { get; set; }

    [JsonPropertyName("phone")]
    public string? Phone { get; set; }

    [JsonPropertyName("password")]
    public string? Password { get; set; }

    [JsonPropertyName("role")]
    public string? Role { get; set; }

    [JsonPropertyName("student_id")]
    public string? StudentId { get; set; }

    [JsonPropertyName("department_id")]
    public int? DepartmentId { get; set; }

    [JsonPropertyName("approval_status")]
    public string? ApprovalStatus { get; set; }
}

public class AdminAddClassStudentRequest
{
    [JsonPropertyName("student_id")]
    public int StudentId { get; set; }
}