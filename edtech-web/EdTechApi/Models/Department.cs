using System.Text.Json.Serialization;

namespace EdTechApi.Models;

public class Department
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("head_id")]
    public int? HeadId { get; set; }

    [JsonPropertyName("head_name")]
    public string? HeadName { get; set; }

    [JsonPropertyName("teacher_count")]
    public int TeacherCount { get; set; }

    [JsonPropertyName("student_count")]
    public int StudentCount { get; set; }

    [JsonPropertyName("created_at")]
    public DateTime CreatedAt { get; set; }

    [JsonPropertyName("updated_at")]
    public DateTime UpdatedAt { get; set; }
}
