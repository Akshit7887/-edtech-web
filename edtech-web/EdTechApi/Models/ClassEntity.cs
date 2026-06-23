using System.Text.Json.Serialization;

namespace EdTechApi.Models;

public class ClassEntity
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("teacher_id")]
    public int TeacherId { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("subject")]
    public string? Subject { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("created_at")]
    public DateTime CreatedAt { get; set; }

    [JsonPropertyName("updated_at")]
    public DateTime UpdatedAt { get; set; }
}
