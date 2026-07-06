using System.Text.Json.Serialization;

namespace EdTechApi.DTOs;

public class CreateDepartmentRequest
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("head_id")]
    public int? HeadId { get; set; }
}

public class UpdateDepartmentRequest
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("head_id")]
    public int? HeadId { get; set; }
}

public class AssignDepartmentRequest
{
    [JsonPropertyName("user_id")]
    public int UserId { get; set; }

    [JsonPropertyName("department_id")]
    public int DepartmentId { get; set; }
}
