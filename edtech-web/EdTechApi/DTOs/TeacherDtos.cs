using System.Text.Json.Serialization;

namespace EdTechApi.DTOs;

public class StudentListItem
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("email")]
    public string? Email { get; set; }

    [JsonPropertyName("phone")]
    public string? Phone { get; set; }

    [JsonPropertyName("created_at")]
    public DateTime CreatedAt { get; set; }

    [JsonPropertyName("class_id")]
    public int? ClassId { get; set; }

    [JsonPropertyName("class_name")]
    public string? ClassName { get; set; }
}

public class StudentDetailResponse
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("email")]
    public string? Email { get; set; }

    [JsonPropertyName("phone")]
    public string? Phone { get; set; }

    [JsonPropertyName("role")]
    public string Role { get; set; } = string.Empty;

    [JsonPropertyName("exam_history")]
    public List<StudentExamHistoryItem> ExamHistory { get; set; } = new();
}

public class StudentExamHistoryItem
{
    [JsonPropertyName("exam_id")]
    public int ExamId { get; set; }

    [JsonPropertyName("exam_title")]
    public string ExamTitle { get; set; } = string.Empty;

    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("score")]
    public decimal? Score { get; set; }

    [JsonPropertyName("total_questions")]
    public int TotalQuestions { get; set; }

    [JsonPropertyName("submitted_at")]
    public DateTime? SubmittedAt { get; set; }
}

public class CreateClassRequest
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("subject")]
    public string? Subject { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }
}

public class AddStudentsRequest
{
    [JsonPropertyName("student_ids")]
    public List<int> StudentIds { get; set; } = new();
}

public class ClassListItem
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("subject")]
    public string? Subject { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("student_count")]
    public int StudentCount { get; set; }

    [JsonPropertyName("created_at")]
    public DateTime CreatedAt { get; set; }
}

public class AnnouncementRequest
{
    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;
}

public class ParentContactRequest
{
    [JsonPropertyName("parent_name")]
    public string ParentName { get; set; } = string.Empty;

    [JsonPropertyName("parent_phone")]
    public string? ParentPhone { get; set; }

    [JsonPropertyName("parent_email")]
    public string? ParentEmail { get; set; }

    [JsonPropertyName("relationship")]
    public string? Relationship { get; set; }
}

public class ParentContactItem
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("student_id")]
    public int StudentId { get; set; }

    [JsonPropertyName("student_name")]
    public string StudentName { get; set; } = string.Empty;

    [JsonPropertyName("parent_name")]
    public string ParentName { get; set; } = string.Empty;

    [JsonPropertyName("parent_phone")]
    public string? ParentPhone { get; set; }

    [JsonPropertyName("parent_email")]
    public string? ParentEmail { get; set; }

    [JsonPropertyName("relationship")]
    public string? Relationship { get; set; }
}

public class ParentReportHistoryItem
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("student_id")]
    public int StudentId { get; set; }

    [JsonPropertyName("student_name")]
    public string StudentName { get; set; } = string.Empty;

    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;

    [JsonPropertyName("sent_via")]
    public string? SentVia { get; set; }

    [JsonPropertyName("sent_at")]
    public DateTime? SentAt { get; set; }

    [JsonPropertyName("delivery_status")]
    public string DeliveryStatus { get; set; } = string.Empty;
}
