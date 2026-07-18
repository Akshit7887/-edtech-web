using System.Text.Json.Serialization;

namespace EdTechApi.Models;

public class SyllabusFile
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("file_name")]
    public string FileName { get; set; } = string.Empty;

    [JsonPropertyName("file_path")]
    public string? FilePath { get; set; }

    [JsonPropertyName("file_data")]
    public byte[]? FileData { get; set; }

    [JsonPropertyName("content_type")]
    public string ContentType { get; set; } = string.Empty;

    [JsonPropertyName("file_size")]
    public long FileSize { get; set; }

    [JsonPropertyName("uploaded_by")]
    public int? UploadedBy { get; set; }

    [JsonPropertyName("uploader_name")]
    public string? UploaderName { get; set; }

    [JsonPropertyName("created_at")]
    public DateTime CreatedAt { get; set; }

    [JsonPropertyName("updated_at")]
    public DateTime UpdatedAt { get; set; }
}
