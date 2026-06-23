using System.Text.Json.Serialization;

namespace EdTechApi.Models;

public class ClassStudent
{
    [JsonPropertyName("class_id")]
    public int ClassId { get; set; }

    [JsonPropertyName("student_id")]
    public int StudentId { get; set; }
}
