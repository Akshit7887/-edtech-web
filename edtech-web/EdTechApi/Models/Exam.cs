using System.Text.Json.Serialization;

namespace EdTechApi.Models;

public class Exam
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("teacher_id")]
    public int TeacherId { get; set; }

    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("subject")]
    public string Subject { get; set; } = string.Empty;

    [JsonPropertyName("syllabus_text")]
    public string? SyllabusText { get; set; }

    [JsonPropertyName("syllabus_pdf_path")]
    public string? SyllabusPdfPath { get; set; }

    [JsonPropertyName("duration_minutes")]
    public int DurationMinutes { get; set; }

    [JsonPropertyName("total_questions")]
    public int TotalQuestions { get; set; }

    [JsonPropertyName("deep_link_code")]
    public string DeepLinkCode { get; set; } = string.Empty;

    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("scheduled_at")]
    public DateTime? ScheduledAt { get; set; }

    [JsonPropertyName("scheduled_end_at")]
    public DateTime? ScheduledEndAt { get; set; }

    [JsonPropertyName("allow_reattempt")]
    public bool AllowReattempt { get; set; }

    [JsonPropertyName("created_at")]
    public DateTime CreatedAt { get; set; }

    [JsonPropertyName("updated_at")]
    public DateTime UpdatedAt { get; set; }

    [JsonIgnore]
    public string? TeacherName { get; set; }
}
