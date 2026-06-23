using System.Text.Json.Serialization;

namespace EdTechApi.Models;

public class ParentContact
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("student_id")]
    public int StudentId { get; set; }

    [JsonPropertyName("parent_name")]
    public string ParentName { get; set; } = string.Empty;

    [JsonPropertyName("parent_phone")]
    public string ParentPhone { get; set; } = string.Empty;

    [JsonPropertyName("parent_email")]
    public string? ParentEmail { get; set; }

    [JsonPropertyName("teacher_id")]
    public int TeacherId { get; set; }

    [JsonPropertyName("created_at")]
    public DateTime CreatedAt { get; set; }

    [JsonPropertyName("updated_at")]
    public DateTime UpdatedAt { get; set; }

    [JsonIgnore]
    public string? StudentName { get; set; }
}
