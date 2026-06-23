using System.Text.Json.Serialization;

namespace EdTechApi.DTOs;

public class GenerateQuestionsRequest
{
    [JsonPropertyName("exam_id")]
    public int ExamId { get; set; }

    [JsonPropertyName("question_count")]
    public int QuestionCount { get; set; }

    [JsonPropertyName("difficulty")]
    public string? Difficulty { get; set; }

    [JsonPropertyName("syllabus_text")]
    public string? SyllabusText { get; set; }
}

public class StartExamSessionRequest
{
    [JsonPropertyName("student_id")]
    public int StudentId { get; set; }

    [JsonPropertyName("exam_id")]
    public int ExamId { get; set; }

    [JsonPropertyName("ip_address")]
    public string? IpAddress { get; set; }

    [JsonPropertyName("user_agent")]
    public string? UserAgent { get; set; }
}

public class SubmitExamRequest
{
    [JsonPropertyName("session_id")]
    public int SessionId { get; set; }

    [JsonPropertyName("answers")]
    public List<AnswerDto> Answers { get; set; } = new();
}

public class AnswerDto
{
    [JsonPropertyName("question_id")]
    public int QuestionId { get; set; }

    [JsonPropertyName("answer")]
    public string Answer { get; set; } = string.Empty;
}

public class ExamStatisticsResponse
{
    [JsonPropertyName("exam_title")]
    public string ExamTitle { get; set; } = string.Empty;

    [JsonPropertyName("subject")]
    public string Subject { get; set; } = string.Empty;

    [JsonPropertyName("total_students")]
    public int TotalStudents { get; set; }

    [JsonPropertyName("completed_count")]
    public int CompletedCount { get; set; }

    [JsonPropertyName("average_score")]
    public double AverageScore { get; set; }

    [JsonPropertyName("highest_score")]
    public int HighestScore { get; set; }

    [JsonPropertyName("lowest_score")]
    public int LowestScore { get; set; }

    [JsonPropertyName("pass_count")]
    public int PassCount { get; set; }

    [JsonPropertyName("fail_count")]
    public int FailCount { get; set; }

    [JsonPropertyName("pass_rate")]
    public int PassRate { get; set; }

    [JsonPropertyName("score_distribution")]
    public Dictionary<string, int> ScoreDistribution { get; set; } = new();

    [JsonPropertyName("max_possible_score")]
    public int MaxPossibleScore { get; set; }

    [JsonPropertyName("student_results")]
    public List<StudentResultItem> StudentResults { get; set; } = new();
}

public class StudentResultItem
{
    [JsonPropertyName("student_id")]
    public int StudentId { get; set; }

    [JsonPropertyName("student_name")]
    public string StudentName { get; set; } = string.Empty;

    [JsonPropertyName("email")]
    public string? Email { get; set; }

    [JsonPropertyName("score")]
    public decimal Score { get; set; }

    [JsonPropertyName("total_questions")]
    public int TotalQuestions { get; set; }

    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("submitted_at")]
    public DateTime? SubmittedAt { get; set; }

    [JsonPropertyName("time_used")]
    public int? TimeUsed { get; set; }
}

public class DisqualifyRequest
{
    [JsonPropertyName("reason")]
    public string Reason { get; set; } = string.Empty;
}

public class PracticeRequest
{
    [JsonPropertyName("exam_id")]
    public int ExamId { get; set; }
}

public class PracticeSubmitRequest
{
    [JsonPropertyName("exam_id")]
    public int ExamId { get; set; }

    [JsonPropertyName("answers")]
    public List<AnswerDto> Answers { get; set; } = new();
}

public class PersonalizedQuestionsRequest
{
    [JsonPropertyName("exam_id")]
    public int ExamId { get; set; }

    [JsonPropertyName("question_count")]
    public int QuestionCount { get; set; }

    [JsonPropertyName("difficulty")]
    public string? Difficulty { get; set; }
}
