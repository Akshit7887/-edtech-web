using EdTechApi.Middleware;
using EdTechApi.Models;
using EdTechApi.Services;
using Microsoft.AspNetCore.Mvc;

namespace EdTechApi.Controllers;

[ApiController]
[Route("api/syllabus")]
public class SyllabusController : ControllerBase
{
    private readonly ISyllabusService _syllabusService;

    private static readonly string[] AllowedExtensions = { ".pdf", ".doc", ".docx", ".ppt", ".pptx", ".txt" };
    private const long MaxFileSize = 20 * 1024 * 1024;

    public SyllabusController(ISyllabusService syllabusService)
    {
        _syllabusService = syllabusService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] string? search, [FromQuery] int? class_id)
    {
        var files = await _syllabusService.GetAllAsync(search, class_id);
        return Ok(new { success = true, data = files });
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var file = await _syllabusService.GetByIdAsync(id);
        if (file == null)
            return NotFound(new { success = false, message = "Syllabus file not found" });
        return Ok(new { success = true, data = file });
    }

    [HttpGet("my")]
    [RequireAuth]
    public async Task<IActionResult> GetMyFiles()
    {
        var userId = HttpContext.Items["UserId"] as int?;
        if (userId == null)
            return Unauthorized(new { success = false, message = "Authentication required" });

        var files = await _syllabusService.GetStudentFilesAsync(userId.Value);
        return Ok(new { success = true, data = files });
    }

    [HttpPost("upload")]
    [RequireRole("teacher")]
    public async Task<IActionResult> Upload([FromForm] string title, [FromForm] string? description, [FromForm] int? classId, [FromForm] IFormFile file)
    {
        var userId = HttpContext.Items["UserId"] as int?;
        if (userId == null)
            return Unauthorized(new { success = false, message = "Authentication required" });

        if (file == null || file.Length == 0)
            return BadRequest(new { success = false, message = "No file provided" });

        if (string.IsNullOrWhiteSpace(title))
            return BadRequest(new { success = false, message = "Title is required" });

        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!AllowedExtensions.Contains(ext))
            return BadRequest(new { success = false, message = "Invalid file type. Allowed: " + string.Join(", ", AllowedExtensions) });

        if (file.Length > MaxFileSize)
            return BadRequest(new { success = false, message = "File exceeds maximum size of 20 MB" });

        using var ms = new MemoryStream();
        await file.CopyToAsync(ms);
        var fileData = ms.ToArray();

        var result = await _syllabusService.UploadAsync(
            title, description, file.FileName, fileData,
            file.ContentType, file.Length, userId, classId);

        return Created(string.Empty, new { success = true, message = "Syllabus file uploaded", data = result });
    }

    [HttpGet("{id:int}/download")]
    public async Task<IActionResult> Download(int id)
    {
        var file = await _syllabusService.GetByIdAsync(id);
        if (file == null)
            return NotFound(new { success = false, message = "File not found" });

        if (file.FileData == null || file.FileData.Length == 0)
            return NotFound(new { success = false, message = "File data not found" });

        var ext = Path.GetExtension(file.FileName)?.ToLowerInvariant();
        var contentType = GetContentType(ext);

        return File(file.FileData, contentType, file.FileName);
    }

    [HttpGet("{id:int}/view")]
    public async Task<IActionResult> View(int id)
    {
        var file = await _syllabusService.GetByIdAsync(id);
        if (file == null)
            return NotFound(new { success = false, message = "File not found" });

        if (file.FileData == null || file.FileData.Length == 0)
            return NotFound(new { success = false, message = "File data not found" });

        var ext = Path.GetExtension(file.FileName)?.ToLowerInvariant();
        var contentType = GetContentType(ext);

        return File(file.FileData, contentType);
    }

    private static string GetContentType(string? ext)
    {
        return ext switch
        {
            ".pdf" => "application/pdf",
            ".doc" => "application/msword",
            ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            ".ppt" => "application/vnd.ms-powerpoint",
            ".pptx" => "application/vnd.openxmlformats-officedocument.presentationml.presentation",
            ".txt" => "text/plain",
            _ => "application/octet-stream"
        };
    }

    [HttpPatch("{id:int}")]
    [RequireRole("teacher")]
    public async Task<IActionResult> UpdateClass(int id, [FromBody] UpdateSyllabusClassRequest request)
    {
        var userId = HttpContext.Items["UserId"] as int?;
        if (userId == null)
            return Unauthorized(new { success = false, message = "Authentication required" });

        var updated = await _syllabusService.UpdateClassAsync(id, request.ClassId);
        if (!updated)
            return NotFound(new { success = false, message = "File not found" });

        return Ok(new { success = true, message = "Syllabus file updated" });
    }

    [HttpDelete("{id:int}")]
    [RequireRole("teacher")]
    public async Task<IActionResult> Delete(int id)
    {
        var userId = HttpContext.Items["UserId"] as int?;
        if (userId == null)
            return Unauthorized(new { success = false, message = "Authentication required" });

        var deleted = await _syllabusService.DeleteAsync(id, userId);
        if (!deleted)
            return NotFound(new { success = false, message = "File not found" });

        return Ok(new { success = true, message = "Syllabus file deleted" });
    }
}

public class UpdateSyllabusClassRequest
{
    public int? ClassId { get; set; }
}