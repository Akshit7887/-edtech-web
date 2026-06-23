using System.Text.Json.Serialization;

namespace EdTechApi.Models;

public class StudentExamAssignment
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("student_id")]
    public int StudentId { get; set; }

    [JsonPropertyName("exam_id")]
    public int ExamId { get; set; }

    [JsonPropertyName("question_ids")]
    public List<int> QuestionIds { get; set; } = new();

    [JsonPropertyName("created_at")]
    public DateTime CreatedAt { get; set; }

    [JsonPropertyName("updated_at")]
    public DateTime UpdatedAt { get; set; }
}
