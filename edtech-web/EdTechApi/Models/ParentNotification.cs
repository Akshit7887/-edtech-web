using System.Text.Json.Serialization;

namespace EdTechApi.Models;

public class ParentNotification
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("exam_id")]
    public int ExamId { get; set; }

    [JsonPropertyName("student_id")]
    public int StudentId { get; set; }

    [JsonPropertyName("parent_contact_id")]
    public int ParentContactId { get; set; }

    [JsonPropertyName("sent_to")]
    public string SentTo { get; set; } = string.Empty;

    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;

    [JsonPropertyName("sent_at")]
    public DateTime? SentAt { get; set; }

    [JsonPropertyName("created_at")]
    public DateTime CreatedAt { get; set; }
}
