using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace EdTechApi.DTOs;

public class CreateExamRequest
{
    [Required(ErrorMessage = "Title is required")]
    [MinLength(3, ErrorMessage = "Title must be at least 3 characters")]
    [MaxLength(255, ErrorMessage = "Title must be at most 255 characters")]
    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [Required(ErrorMessage = "Subject is required")]
    [MinLength(2, ErrorMessage = "Subject must be at least 2 characters")]
    [MaxLength(100, ErrorMessage = "Subject must be at most 100 characters")]
    [JsonPropertyName("subject")]
    public string Subject { get; set; } = string.Empty;

    [MaxLength(50000, ErrorMessage = "Syllabus text must be at most 50000 characters")]
    [JsonPropertyName("syllabus_text")]
    public string? SyllabusText { get; set; }

    [MaxLength(500, ErrorMessage = "Syllabus PDF path must be at most 500 characters")]
    [JsonPropertyName("syllabus_pdf_path")]
    public string? SyllabusPdfPath { get; set; }

    [Range(1, 480, ErrorMessage = "Duration must be between 1 and 480 minutes")]
    [JsonPropertyName("duration_minutes")]
    public int DurationMinutes { get; set; }

    [Range(1, 500, ErrorMessage = "Total questions must be between 1 and 500")]
    [JsonPropertyName("total_questions")]
    public int TotalQuestions { get; set; }

    [RegularExpression("^(draft|active|closed)$", ErrorMessage = "Status must be 'draft', 'active', or 'closed'")]
    [JsonPropertyName("status")]
    public string? Status { get; set; }
}

public class UpdateExamRequest
{
    [JsonPropertyName("title")]
    public string? Title { get; set; }

    [JsonPropertyName("subject")]
    public string? Subject { get; set; }

    [JsonPropertyName("syllabus_text")]
    public string? SyllabusText { get; set; }

    [JsonPropertyName("duration_minutes")]
    public int? DurationMinutes { get; set; }

    [JsonPropertyName("total_questions")]
    public int? TotalQuestions { get; set; }

    [JsonPropertyName("status")]
    public string? Status { get; set; }
}

public class ExamListResponse
{
    [JsonPropertyName("data")]
    public List<ExamItem> Data { get; set; } = new();

    [JsonPropertyName("pagination")]
    public PaginationInfo Pagination { get; set; } = new();
}

public class ExamItem
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("subject")]
    public string Subject { get; set; } = string.Empty;

    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("duration_minutes")]
    public int DurationMinutes { get; set; }

    [JsonPropertyName("total_questions")]
    public int TotalQuestions { get; set; }

    [JsonPropertyName("students_attended")]
    public int StudentsAttended { get; set; }

    [JsonPropertyName("teacher_name")]
    public string? TeacherName { get; set; }

    [JsonPropertyName("created_at")]
    public DateTime CreatedAt { get; set; }
}

public class ExamDetailResponse
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("subject")]
    public string Subject { get; set; } = string.Empty;

    [JsonPropertyName("syllabus_text")]
    public string? SyllabusText { get; set; }

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

    [JsonPropertyName("questions")]
    public List<QuestionItem>? Questions { get; set; }

    [JsonPropertyName("total_questions_count")]
    public int TotalQuestionsCount { get; set; }

    [JsonPropertyName("question_offset")]
    public int QuestionOffset { get; set; }

    [JsonPropertyName("has_more")]
    public bool HasMore { get; set; }

    [JsonPropertyName("attendance")]
    public string? Attendance { get; set; }

    [JsonPropertyName("created_at")]
    public DateTime CreatedAt { get; set; }
}

public class QuestionItem
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

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
    public string? CorrectAnswer { get; set; }

    [JsonPropertyName("difficulty")]
    public string Difficulty { get; set; } = string.Empty;

    [JsonPropertyName("points")]
    public int Points { get; set; }

    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;
}

public class BulkImportRequest
{
    [Required(ErrorMessage = "CSV text is required")]
    [MinLength(10, ErrorMessage = "CSV text must be at least 10 characters")]
    [MaxLength(500000, ErrorMessage = "CSV text must be at most 500000 characters")]
    [JsonPropertyName("csv_text")]
    public string CsvText { get; set; } = string.Empty;
}

public class BulkImportResponse
{
    [JsonPropertyName("imported")]
    public int Imported { get; set; }

    [JsonPropertyName("errors")]
    public List<string> Errors { get; set; } = new();
}

public class PaginationInfo
{
    [JsonPropertyName("page")]
    public int Page { get; set; }

    [JsonPropertyName("limit")]
    public int Limit { get; set; }

    [JsonPropertyName("total")]
    public int Total { get; set; }

    [JsonPropertyName("total_pages")]
    public int TotalPages { get; set; }
}

public class AiCreateExamRequest
{
    [Required(ErrorMessage = "Subject is required")]
    [MinLength(2, ErrorMessage = "Subject must be at least 2 characters")]
    [MaxLength(100, ErrorMessage = "Subject must be at most 100 characters")]
    [JsonPropertyName("subject")]
    public string Subject { get; set; } = string.Empty;

    [Required(ErrorMessage = "Topic is required")]
    [MinLength(2, ErrorMessage = "Topic must be at least 2 characters")]
    [MaxLength(200, ErrorMessage = "Topic must be at most 200 characters")]
    [JsonPropertyName("topic")]
    public string Topic { get; set; } = string.Empty;

    [Range(1, 100, ErrorMessage = "Question count must be between 1 and 100")]
    [JsonPropertyName("question_count")]
    public int QuestionCount { get; set; }

    [RegularExpression("^(easy|medium|hard)$", ErrorMessage = "Difficulty must be 'easy', 'medium', or 'hard'")]
    [JsonPropertyName("difficulty")]
    public string? Difficulty { get; set; }
}

public class AiCreateExamResponse
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    [JsonPropertyName("message")]
    public string? Message { get; set; }

    [JsonPropertyName("exam")]
    public ExamItem? Exam { get; set; }
}
