using System.Text.Json.Serialization;

namespace EdTechApi.Models;

public class QuestionPool
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("exam_id")]
    public int ExamId { get; set; }

    [JsonPropertyName("student_id")]
    public int? StudentId { get; set; }

    [JsonPropertyName("question_text")]
    public string QuestionText { get; set; } = string.Empty;

    [JsonPropertyName("option_a")]
    public string OptionA { get; set; } = string.Empty;

    [JsonPropertyName("option_b")]
    public string OptionB { get; set; } = string.Empty;

    [JsonPropertyName("option_c")]
    public string? OptionC { get; set; }

    [JsonPropertyName("option_d")]
    public string? OptionD { get; set; }

    [JsonPropertyName("correct_answer")]
    public string CorrectAnswer { get; set; } = string.Empty;

    [JsonPropertyName("difficulty")]
    public string Difficulty { get; set; } = string.Empty;

    [JsonPropertyName("points")]
    public int Points { get; set; }

    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("created_at")]
    public DateTime CreatedAt { get; set; }

    [JsonPropertyName("updated_at")]
    public DateTime UpdatedAt { get; set; }
}
