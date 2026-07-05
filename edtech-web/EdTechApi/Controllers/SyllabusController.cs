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

    public SyllabusController(ISyllabusService syllabusService, IWebHostEnvironment env)
    {
        _syllabusService = syllabusService;
        _env = env;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] string? search)
    {
        var files = await _syllabusService.GetAllAsync(search);
        return Ok(new { success = true, data = files });
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var file = await _syllabusService.GetByIdAsync(id);
        if (file == null)
            return NotFound(new { success = false, message = "Syllabus file not found" });
        return Ok(new { success = true, data = file });
    }

    [HttpPost("upload")]
    public async Task<IActionResult> Upload([FromForm] string title, [FromForm] string? description, [FromForm] IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest(new { success = false, message = "No file provided" });

        if (string.IsNullOrWhiteSpace(title))
            return BadRequest(new { success = false, message = "Title is required" });

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
            file.ContentType, file.Length, null);

        return Created(string.Empty, new { success = true, message = "Syllabus file uploaded", data = result });
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var deleted = await _syllabusService.DeleteAsync(id, null);
        if (!deleted)
            return NotFound(new { success = false, message = "File not found" });

        return Ok(new { success = true, message = "Syllabus file deleted" });
    }
}
