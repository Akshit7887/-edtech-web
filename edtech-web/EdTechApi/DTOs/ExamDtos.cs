using System.Text.Json.Serialization;

namespace EdTechApi.DTOs;

public class CreateExamRequest
{
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
    [JsonPropertyName("subject")]
    public string Subject { get; set; } = string.Empty;

    [JsonPropertyName("topic")]
    public string Topic { get; set; } = string.Empty;

    [JsonPropertyName("question_count")]
    public int QuestionCount { get; set; }

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
