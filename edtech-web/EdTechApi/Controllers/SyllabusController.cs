using EdTechApi.Models;
using EdTechApi.Services;
using Microsoft.AspNetCore.Mvc;

namespace EdTechApi.Controllers;

[ApiController]
[Route("api/syllabus")]
public class SyllabusController : ControllerBase
{
    private readonly ISyllabusService _syllabusService;
    private readonly IWebHostEnvironment _env;

    private static readonly string[] AllowedExtensions = { ".pdf", ".doc", ".docx", ".txt" };
    private const long MaxFileSize = 20 * 1024 * 1024; // 20 MB

    public SyllabusController(ISyllabusService syllabusService, IWebHostEnvironment env)
    {
        _syllabusService = syllabusService;
        _env = env;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] string? search)
    {
        var userId = HttpContext.Items["UserId"] as int?;
        var files = await _syllabusService.GetAllAsync(search);
        return Ok(new { success = true, data = files });
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var userId = HttpContext.Items["UserId"] as int?;
        var file = await _syllabusService.GetByIdAsync(id);
        if (file == null)
            return NotFound(new { success = false, message = "Syllabus file not found" });
        return Ok(new { success = true, data = file });
    }

    [HttpPost("upload")]
    public async Task<IActionResult> Upload([FromForm] string title, [FromForm] string? description, [FromForm] IFormFile file)
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

        var uploadsDir = Path.Combine(_env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot"), "uploads", "syllabus");
        Directory.CreateDirectory(uploadsDir);

        var safeFileName = $"{Guid.NewGuid():N}_{file.FileName}";
        var filePath = Path.Combine(uploadsDir, safeFileName);

        await using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        var relativePath = $"/uploads/syllabus/{safeFileName}";

        var result = await _syllabusService.UploadAsync(
            title, description, file.FileName, relativePath,
            file.ContentType, file.Length, userId);

        return Created(string.Empty, new { success = true, message = "Syllabus file uploaded", data = result });
    }

    [HttpGet("{id}/download")]
    public async Task<IActionResult> Download(int id)
    {
        var file = await _syllabusService.GetByIdAsync(id);
        if (file == null)
            return NotFound(new { success = false, message = "File not found" });

        var filePath = Path.Combine(
            _env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot"),
            file.FilePath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));

        if (!System.IO.File.Exists(filePath))
            return NotFound(new { success = false, message = "File not found on disk" });

        var ext = Path.GetExtension(file.FileName)?.ToLowerInvariant();
        var contentType = ext switch
        {
            ".pdf" => "application/pdf",
            ".doc" => "application/msword",
            ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            ".txt" => "text/plain",
            _ => "application/octet-stream"
        };

        var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
        return File(stream, contentType, file.FileName);
    }

    [HttpDelete("{id}")]
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
