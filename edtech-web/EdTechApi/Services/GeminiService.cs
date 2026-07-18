using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace EdTechApi.Services;

public class GeminiQuestion
{
    public string question_text { get; set; } = "";
    public string option_a { get; set; } = "";
    public string option_b { get; set; } = "";
    public string? option_c { get; set; }
    public string? option_d { get; set; }
    public string correct_answer { get; set; } = "";
    public string difficulty { get; set; } = "medium";
}

public class GeminiExamResult
{
    public string title { get; set; } = "";
    public string syllabusText { get; set; } = "";
    public List<GeminiQuestion> questions { get; set; } = new();
}

public interface IGeminiService
{
    Task<List<GeminiQuestion>> GenerateQuestionsFromText(string text, int questionCount, string difficulty);
    Task<GeminiExamResult> GenerateFullExam(string subject, string topic, int questionCount, string difficulty);
    Task<List<GeminiQuestion>> GenerateQuestionsForStudent(string studentName, string syllabusText, int questionCount, string difficulty);
}

public class GeminiService : IGeminiService
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;
    private readonly ILogger<GeminiService> _logger;
    private readonly ICircuitBreakerService _circuitBreaker;

    private static readonly (string name, string version)[] Models = new[]
    {
        ("gemini-2.5-flash", "v1"),
        ("gemini-2.0-flash", "v1"),
        ("gemini-2.0-flash-lite", "v1"),
        ("gemini-2.5-pro", "v1"),
        ("gemini-2.0-flash", "v1beta"),
        ("gemini-2.0-flash-lite", "v1beta"),
    };

    public GeminiService(HttpClient httpClient, IConfiguration config, ILogger<GeminiService> logger, ICircuitBreakerService circuitBreaker)
    {
        _httpClient = httpClient;
        _apiKey = config["Gemini:ApiKey"] ?? throw new InvalidOperationException("Gemini API Key not configured");
        _logger = logger;
        _circuitBreaker = circuitBreaker;
    }

    public async Task<List<GeminiQuestion>> GenerateQuestionsFromText(string text, int questionCount, string difficulty)
    {
        const int maxTextLength = 50000;
        var truncated = text.Length > maxTextLength ? text[..maxTextLength] + "..." : text;

        var prompt = BuildQuestionPrompt(truncated, questionCount, difficulty);
        var result = await CallGeminiApi(prompt);

        var questions = JsonSerializer.Deserialize<List<GeminiQuestion>>(result, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        return questions?.Where(q => !string.IsNullOrEmpty(q.question_text) && !string.IsNullOrEmpty(q.option_a) && !string.IsNullOrEmpty(q.option_b) && !string.IsNullOrEmpty(q.correct_answer)).ToList() ?? new();
    }

    public async Task<GeminiExamResult> GenerateFullExam(string subject, string topic, int questionCount, string difficulty)
    {
        var prompt = $@"You are an exam generator. Create a complete exam for the subject ""{subject}"" on the topic ""{topic}"".

Return ONLY a JSON object. No extra text before or after.
Format:
{{
  ""title"": ""A compelling exam title that includes the subject and topic"",
  ""syllabusText"": ""A concise syllabus description covering the key concepts, formulas, and topics that this exam will test. Write 3-5 sentences."",
  ""questions"": [
    {{
      ""question_text"": ""Question text here"",
      ""option_a"": ""Option A"",
      ""option_b"": ""Option B"",
      ""option_c"": ""Option C"",
      ""option_d"": ""Option D"",
      ""correct_answer"": ""A"",
      ""difficulty"": ""{difficulty}""
    }}
  ]
}}

Important rules:
1. Return ONLY the JSON object, no additional text
2. All questions must be in English, relevant to ""{subject}"" and ""{topic}""
3. Each question must have exactly 4 options (A, B, C, D)
4. The correct_answer field must be one of: A, B, C, or D
5. The difficulty must be: easy, medium, or hard
6. Generate exactly {questionCount} questions with {difficulty} difficulty
7. The title must be 5-200 characters
8. The syllabusText must be 50-2000 characters
9. Generate a diverse set of questions covering different aspects of the topic";

        var responseText = await CallGeminiApi(prompt);
        var jsonText = ExtractFirstJsonObject(responseText);
        if (jsonText == null) throw new InvalidOperationException("Failed to parse JSON from AI response");

        var result = JsonSerializer.Deserialize<GeminiExamResult>(jsonText, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        if (result == null || string.IsNullOrEmpty(result.title))
            throw new InvalidOperationException("Missing required fields in AI response");

        result.questions = result.questions
            .Where(q => !string.IsNullOrEmpty(q.question_text) && !string.IsNullOrEmpty(q.option_a) && !string.IsNullOrEmpty(q.option_b) && !string.IsNullOrEmpty(q.correct_answer))
            .ToList();

        return result;
    }

    public async Task<List<GeminiQuestion>> GenerateQuestionsForStudent(string studentName, string syllabusText, int questionCount, string difficulty)
    {
        const int maxTextLength = 50000;
        var truncated = syllabusText.Length > maxTextLength ? syllabusText[..maxTextLength] + "..." : syllabusText;

        var prompt = $@"Generate {questionCount} unique multiple-choice questions from the following syllabus for a student named ""{studentName}"".
Each question should feel relevant and appropriate for this student's level.

Return ONLY a JSON array. No extra text.
Format:
[
  {{
    ""question_text"": ""Question text here"",
    ""option_a"": ""Option A text"",
    ""option_b"": ""Option B text"",
    ""option_c"": ""Option C text"",
    ""option_d"": ""Option D text"",
    ""correct_answer"": ""A"",
    ""difficulty"": ""{difficulty}""
  }}
]

Syllabus:
{truncated}

Important rules:
1. Return only the JSON array, no additional text or explanation
2. All questions must be in English
3. Each question must have exactly 4 options (A, B, C, D)
4. The correct_answer field must be one of: A, B, C, or D
5. The difficulty must be: easy, medium, or hard
6. Generate exactly {questionCount} questions
7. Make these questions unique and different from any other student's questions";

        var result = await CallGeminiApi(prompt);
        var questions = JsonSerializer.Deserialize<List<GeminiQuestion>>(result, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        return questions?.Where(q => !string.IsNullOrEmpty(q.question_text) && !string.IsNullOrEmpty(q.option_a) && !string.IsNullOrEmpty(q.option_b) && !string.IsNullOrEmpty(q.correct_answer)).ToList() ?? new();
    }

    private string BuildQuestionPrompt(string text, int questionCount, string difficulty)
    {
        return $@"Generate {questionCount} multiple-choice questions from the following syllabus.
Return ONLY a JSON array. No extra text.
Format:
[
  {{
    ""question_text"": ""Question text here"",
    ""option_a"": ""Option A text"",
    ""option_b"": ""Option B text"",
    ""option_c"": ""Option C text"",
    ""option_d"": ""Option D text"",
    ""correct_answer"": ""A"",
    ""difficulty"": ""{difficulty}""
  }}
]

Syllabus:
{text}

Important rules:
1. Return only the JSON array, no additional text or explanation
2. All questions must be in English
3. Each question must have exactly 4 options (A, B, C, D)
4. The correct_answer field must be one of: A, B, C, or D
5. The difficulty must be: easy, medium, or hard
6. Generate exactly {questionCount} questions";
    }

    private static string? ExtractFirstJsonObject(string text)
    {
        var start = text.IndexOf('{');
        if (start < 0) return null;
        var depth = 0;
        for (var i = start; i < text.Length; i++)
        {
            if (text[i] == '{') depth++;
            else if (text[i] == '}') { depth--; if (depth == 0) return text[start..(i + 1)]; }
        }
        return null;
    }

    private static string? ExtractFirstJsonArray(string text)
    {
        var start = text.IndexOf('[');
        if (start < 0) return null;
        var depth = 0;
        for (var i = start; i < text.Length; i++)
        {
            if (text[i] == '[') depth++;
            else if (text[i] == ']') { depth--; if (depth == 0) return text[start..(i + 1)]; }
        }
        return null;
    }

    private async Task<string> CallGeminiApi(string prompt)
    {
        if (string.IsNullOrEmpty(_apiKey))
            throw new InvalidOperationException("GEMINI_API_KEY not configured. Please ask the admin to set it up.");

        return await _circuitBreaker.ExecuteAsync("gemini-api", async () =>
        {
            var lastError = "";

            foreach (var (name, version) in Models)
            {
                try
                {
                    var url = $"https://generativelanguage.googleapis.com/{version}/models/{name}:generateContent?key={_apiKey}";
                    var payload = new { contents = new[] { new { parts = new[] { new { text = prompt } } } } };
                    var json = JsonSerializer.Serialize(payload);
                    var content = new StringContent(json, Encoding.UTF8, "application/json");

                    _logger.LogInformation("[AI] Trying model: {Model} ({Version})", name, version);
                    var response = await _httpClient.PostAsync(url, content);
                    response.EnsureSuccessStatusCode();

                    var responseBody = await response.Content.ReadAsStringAsync();
                    using var doc = JsonDocument.Parse(responseBody);
                    var text = doc.RootElement
                        .GetProperty("candidates")[0]
                        .GetProperty("content")
                        .GetProperty("parts")[0]
                        .GetProperty("text").GetString();

                    if (string.IsNullOrEmpty(text))
                    {
                        lastError = "Invalid response from Gemini API";
                        continue;
                    }

                    var firstBracket = text.IndexOf('[');
                    var firstBrace = text.IndexOf('{');
                    string? extracted = null;
                    if (firstBracket >= 0 && (firstBrace < 0 || firstBracket < firstBrace))
                        extracted = ExtractFirstJsonArray(text);
                    else if (firstBrace >= 0)
                        extracted = ExtractFirstJsonObject(text);

                    if (extracted == null)
                    {
                        lastError = "Failed to parse questions from AI response";
                        continue;
                    }

                    _logger.LogInformation("[AI] Successfully generated content using {Model}", name);
                    return extracted;
                }
                catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
                {
                    lastError = "Rate limited";
                    _logger.LogWarning("[AI] Rate limited on {Model}. Waiting 3s...", name);
                    await Task.Delay(3000);
                }
                catch (Exception ex)
                {
                    lastError = ex.Message;
                    _logger.LogWarning("[AI] Model {Model} failed: {Error}", name, ex.Message);
                }
            }

            throw new InvalidOperationException($"Failed to generate questions. {lastError}");
        });
    }
}
