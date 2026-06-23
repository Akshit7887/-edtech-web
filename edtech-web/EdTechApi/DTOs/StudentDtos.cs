using System.Text.Json.Serialization;

namespace EdTechApi.DTOs;

public class StudentAnalyticsResponse
{
    [JsonPropertyName("total_exams")]
    public int TotalExams { get; set; }

    [JsonPropertyName("completed_exams")]
    public int CompletedExams { get; set; }

    [JsonPropertyName("average_score")]
    public double AverageScore { get; set; }

    [JsonPropertyName("highest_score")]
    public int HighestScore { get; set; }

    [JsonPropertyName("best_rank")]
    public int BestRank { get; set; }

    [JsonPropertyName("exam_performances")]
    public List<ExamPerformanceItem>? ExamPerformances { get; set; }
}

public class ExamPerformanceItem
{
    [JsonPropertyName("exam_title")]
    public string ExamTitle { get; set; } = string.Empty;

    [JsonPropertyName("score")]
    public decimal? Score { get; set; }

    [JsonPropertyName("total_questions")]
    public int TotalQuestions { get; set; }

    [JsonPropertyName("percentage")]
    public double Percentage { get; set; }

    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("submitted_at")]
    public DateTime? SubmittedAt { get; set; }
}

public class ExamReviewResponse
{
    [JsonPropertyName("exam_id")]
    public int ExamId { get; set; }

    [JsonPropertyName("exam_title")]
    public string ExamTitle { get; set; } = string.Empty;

    [JsonPropertyName("subject")]
    public string Subject { get; set; } = string.Empty;

    [JsonPropertyName("total_questions")]
    public int TotalQuestions { get; set; }

    [JsonPropertyName("score")]
    public decimal Score { get; set; }

    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("submitted_at")]
    public DateTime? SubmittedAt { get; set; }

    [JsonPropertyName("time_used")]
    public int? TimeUsed { get; set; }

    [JsonPropertyName("questions")]
    public List<ReviewQuestionItem> Questions { get; set; } = new();
}

public class ReviewQuestionItem
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("question_text")]
    public string QuestionText { get; set; } = string.Empty;

    [JsonPropertyName("student_answer")]
    public string StudentAnswer { get; set; } = string.Empty;

    [JsonPropertyName("correct_answer")]
    public string CorrectAnswer { get; set; } = string.Empty;

    [JsonPropertyName("is_correct")]
    public bool IsCorrect { get; set; }

    [JsonPropertyName("option_a")]
    public string OptionA { get; set; } = string.Empty;

    [JsonPropertyName("option_b")]
    public string OptionB { get; set; } = string.Empty;

    [JsonPropertyName("option_c")]
    public string? OptionC { get; set; }

    [JsonPropertyName("option_d")]
    public string? OptionD { get; set; }
}

public class NotificationItem
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;

    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("is_read")]
    public bool IsRead { get; set; }

    [JsonPropertyName("created_at")]
    public DateTime CreatedAt { get; set; }
}

public class MyResultItem
{
    [JsonPropertyName("session_id")]
    public int SessionId { get; set; }

    [JsonPropertyName("exam_id")]
    public int ExamId { get; set; }

    [JsonPropertyName("exam_title")]
    public string ExamTitle { get; set; } = string.Empty;

    [JsonPropertyName("subject")]
    public string Subject { get; set; } = string.Empty;

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
