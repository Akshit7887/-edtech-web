using System.Text.Json.Serialization;

namespace EdTechApi.Models;

public class Attendance
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("student_id")]
    public int StudentId { get; set; }

    [JsonPropertyName("exam_id")]
    public int ExamId { get; set; }

    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("marked_at")]
    public DateTime? MarkedAt { get; set; }

    [JsonPropertyName("marked_by")]
    public string MarkedBy { get; set; } = string.Empty;

    [JsonPropertyName("created_at")]
    public DateTime CreatedAt { get; set; }

    [JsonPropertyName("updated_at")]
    public DateTime UpdatedAt { get; set; }

    [JsonIgnore]
    public string? StudentName { get; set; }

    [JsonIgnore]
    public string? StudentEmail { get; set; }
}
