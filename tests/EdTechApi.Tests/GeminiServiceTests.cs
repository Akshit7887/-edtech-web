namespace EdTechApi.Tests;

public class GeminiServiceTests
{
    // Access private static methods via reflection to test JSON extraction
    private static string? InvokeExtractFirstJsonObject(string text)
    {
        var method = typeof(Services.GeminiService).Assembly.GetType("EdTechApi.Services.GeminiService")
            ?.GetMethod("ExtractFirstJsonObject", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        return method?.Invoke(null, new object[] { text }) as string;
    }

    private static string? InvokeExtractFirstJsonArray(string text)
    {
        var method = typeof(Services.GeminiService).Assembly.GetType("EdTechApi.Services.GeminiService")
            ?.GetMethod("ExtractFirstJsonArray", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        return method?.Invoke(null, new object[] { text }) as string;
    }

    [Fact]
    public void ExtractFirstJsonObject_FromCleanJson_ReturnsObject()
    {
        var json = @"{
            ""title"": ""Test"",
            ""questions"": []
        }";
        var result = InvokeExtractFirstJsonObject(json);
        Assert.NotNull(result);
        Assert.Contains("Test", result);
    }

    [Fact]
    public void ExtractFirstJsonObject_FromMarkdownWrapped_ReturnsObject()
    {
        var text = @"Here is the exam:
```json
{
  ""title"": ""Math Exam"",
  ""syllabusText"": ""Algebra and Geometry"",
  ""questions"": []
}
```
That's all.";
        var result = InvokeExtractFirstJsonObject(text);
        Assert.NotNull(result);
        Assert.Contains("Math Exam", result);
    }

    [Fact]
    public void ExtractFirstJsonObject_FromTextWithPrefix_ReturnsObject()
    {
        var text = @"Some introductory text
{
  ""title"": ""Physics Quiz"",
  ""questions"": [{ ""question_text"": ""Q1"", ""option_a"": ""A"", ""option_b"": ""B"", ""correct_answer"": ""A"" }]
}
trailing text";
        var result = InvokeExtractFirstJsonObject(text);
        Assert.NotNull(result);
        Assert.Contains("Physics Quiz", result);
    }

    [Fact]
    public void ExtractFirstJsonObject_NestedBraces_ReturnsCorrectObject()
    {
        var json = @"{""outer"": {""inner"": ""value""}, ""done"": true}";
        var result = InvokeExtractFirstJsonObject(json);
        Assert.NotNull(result);
        Assert.Equal(json, result);
    }

    [Fact]
    public void ExtractFirstJsonObject_NoBrace_ReturnsNull()
    {
        var result = InvokeExtractFirstJsonObject("just plain text without JSON");
        Assert.Null(result);
    }

    [Fact]
    public void ExtractFirstJsonArray_FromCleanJson_ReturnsArray()
    {
        var json = @"[{""question_text"": ""Q1"", ""option_a"": ""A"", ""option_b"": ""B"", ""correct_answer"": ""A""}]";
        var result = InvokeExtractFirstJsonArray(json);
        Assert.NotNull(result);
        Assert.StartsWith("[", result);
        Assert.EndsWith("]", result);
    }

    [Fact]
    public void ExtractFirstJsonArray_FromMarkdownWrapped_ReturnsArray()
    {
        var text = "Here are the questions:\n```json\n[{\"question_text\": \"Q1\", \"option_a\": \"A\", \"option_b\": \"B\", \"correct_answer\": \"A\"}]\n```";
        var result = InvokeExtractFirstJsonArray(text);
        Assert.NotNull(result);
    }

    [Fact]
    public void ExtractFirstJsonArray_NoBracket_ReturnsNull()
    {
        var result = InvokeExtractFirstJsonArray("no array here");
        Assert.Null(result);
    }
}
