using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace EdTechApi.DTOs;

public class GenerateQuestionsRequest
{
    [Required(ErrorMessage = "Exam ID is required")]
    [Range(1, int.MaxValue, ErrorMessage = "Invalid exam ID")]
    [JsonPropertyName("exam_id")]
    public int ExamId { get; set; }

    [Range(1, 100, ErrorMessage = "Question count must be between 1 and 100")]
    [JsonPropertyName("question_count")]
    public int QuestionCount { get; set; }

    [RegularExpression("^(easy|medium|hard)$", ErrorMessage = "Difficulty must be 'easy', 'medium', or 'hard'")]
    [JsonPropertyName("difficulty")]
    public string? Difficulty { get; set; }

    [MaxLength(50000, ErrorMessage = "Syllabus text must be at most 50000 characters")]
    [JsonPropertyName("syllabus_text")]
    public string? SyllabusText { get; set; }
}

public class StartExamSessionRequest
{
    [Required(ErrorMessage = "Student ID is required")]
    [Range(1, int.MaxValue, ErrorMessage = "Invalid student ID")]
    [JsonPropertyName("student_id")]
    public int StudentId { get; set; }

    [Required(ErrorMessage = "Exam ID is required")]
    [Range(1, int.MaxValue, ErrorMessage = "Invalid exam ID")]
    [JsonPropertyName("exam_id")]
    public int ExamId { get; set; }

    [MaxLength(45, ErrorMessage = "IP address must be at most 45 characters")]
    [JsonPropertyName("ip_address")]
    public string? IpAddress { get; set; }

    [MaxLength(500, ErrorMessage = "User agent must be at most 500 characters")]
    [JsonPropertyName("user_agent")]
    public string? UserAgent { get; set; }
}

public class SubmitExamRequest
{
    [Required(ErrorMessage = "Session ID is required")]
    [Range(1, int.MaxValue, ErrorMessage = "Invalid session ID")]
    [JsonPropertyName("session_id")]
    public int SessionId { get; set; }

    [Required(ErrorMessage = "Answers are required")]
    [MinLength(1, ErrorMessage = "At least one answer is required")]
    [MaxLength(500, ErrorMessage = "Too many answers (max 500)")]
    [JsonPropertyName("answers")]
    public List<AnswerDto> Answers { get; set; } = new();
}

public class AnswerDto
{
    [Required(ErrorMessage = "Question ID is required")]
    [Range(1, int.MaxValue, ErrorMessage = "Invalid question ID")]
    [JsonPropertyName("question_id")]
    public int QuestionId { get; set; }

    [Required(ErrorMessage = "Answer is required")]
    [RegularExpression("^[A-D]$", ErrorMessage = "Answer must be A, B, C, or D")]
    [MaxLength(1, ErrorMessage = "Answer must be a single character")]
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
    [Required(ErrorMessage = "Reason is required")]
    [MinLength(3, ErrorMessage = "Reason must be at least 3 characters")]
    [MaxLength(500, ErrorMessage = "Reason must be at most 500 characters")]
    [JsonPropertyName("reason")]
    public string Reason { get; set; } = string.Empty;
}

public class PracticeRequest
{
    [Required(ErrorMessage = "Exam ID is required")]
    [Range(1, int.MaxValue, ErrorMessage = "Invalid exam ID")]
    [JsonPropertyName("exam_id")]
    public int ExamId { get; set; }
}

public class PracticeSubmitRequest
{
    [Required(ErrorMessage = "Exam ID is required")]
    [Range(1, int.MaxValue, ErrorMessage = "Invalid exam ID")]
    [JsonPropertyName("exam_id")]
    public int ExamId { get; set; }

    [Required(ErrorMessage = "Answers are required")]
    [MinLength(1, ErrorMessage = "At least one answer is required")]
    [MaxLength(500, ErrorMessage = "Too many answers (max 500)")]
    [JsonPropertyName("answers")]
    public List<AnswerDto> Answers { get; set; } = new();
}

public class PersonalizedQuestionsRequest
{
    [Required(ErrorMessage = "Exam ID is required")]
    [Range(1, int.MaxValue, ErrorMessage = "Invalid exam ID")]
    [JsonPropertyName("exam_id")]
    public int ExamId { get; set; }

    [Range(1, 50, ErrorMessage = "Question count must be between 1 and 50")]
    [JsonPropertyName("question_count")]
    public int QuestionCount { get; set; }

    [RegularExpression("^(easy|medium|hard)$", ErrorMessage = "Difficulty must be 'easy', 'medium', or 'hard'")]
    [JsonPropertyName("difficulty")]
    public string? Difficulty { get; set; }
}
