using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace EdTechApi.DTOs;

public class CreateClassRequest
{
    [Required(ErrorMessage = "Class name is required")]
    [MinLength(2, ErrorMessage = "Class name must be at least 2 characters")]
    [MaxLength(255, ErrorMessage = "Class name must be at most 255 characters")]
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [MaxLength(100, ErrorMessage = "Subject must be at most 100 characters")]
    [JsonPropertyName("subject")]
    public string? Subject { get; set; }

    [MaxLength(1000, ErrorMessage = "Description must be at most 1000 characters")]
    [JsonPropertyName("description")]
    public string? Description { get; set; }
}

public class AddStudentsRequest
{
    [Required(ErrorMessage = "Student IDs are required")]
    [MinLength(1, ErrorMessage = "At least one student ID is required")]
    [MaxLength(500, ErrorMessage = "Too many student IDs (max 500)")]
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
    [Required(ErrorMessage = "Title is required")]
    [MinLength(3, ErrorMessage = "Title must be at least 3 characters")]
    [MaxLength(255, ErrorMessage = "Title must be at most 255 characters")]
    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [Required(ErrorMessage = "Message is required")]
    [MinLength(10, ErrorMessage = "Message must be at least 10 characters")]
    [MaxLength(5000, ErrorMessage = "Message must be at most 5000 characters")]
    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;
}

public class ParentContactRequest
{
    [Required(ErrorMessage = "Parent name is required")]
    [MinLength(2, ErrorMessage = "Parent name must be at least 2 characters")]
    [MaxLength(255, ErrorMessage = "Parent name must be at most 255 characters")]
    [JsonPropertyName("parent_name")]
    public string ParentName { get; set; } = string.Empty;

    [RegularExpression(@"^\+?[0-9\s\-\(\)]{7,20}$", ErrorMessage = "Invalid phone number format")]
    [JsonPropertyName("parent_phone")]
    public string? ParentPhone { get; set; }

    [EmailAddress(ErrorMessage = "Invalid email format")]
    [MaxLength(255, ErrorMessage = "Email must be at most 255 characters")]
    [JsonPropertyName("parent_email")]
    public string? ParentEmail { get; set; }

    [MaxLength(50, ErrorMessage = "Relationship must be at most 50 characters")]
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
