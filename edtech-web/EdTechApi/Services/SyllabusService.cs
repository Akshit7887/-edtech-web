using Dapper;
using EdTechApi.Data;
using EdTechApi.Models;

namespace EdTechApi.Services;

public interface ISyllabusService
{
    Task<List<SyllabusFile>> GetAllAsync(string? search = null);
    Task<SyllabusFile?> GetByIdAsync(int id);
    Task<SyllabusFile> UploadAsync(string title, string? description, string fileName, string filePath, string contentType, long fileSize, int? uploadedBy);
    Task<bool> DeleteAsync(int id, int? userId);
}

public class SyllabusService : ISyllabusService
{
    private readonly IDbConnectionFactory _db;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public SyllabusService(IDbConnectionFactory db, IHttpContextAccessor httpContextAccessor)
    {
        _db = db;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<List<SyllabusFile>> GetAllAsync(string? search = null)
    {
        using var conn = _db.CreateConnection();
        var sql = @"SELECT sf.*, u.""name"" AS uploader_name 
                     FROM ""SyllabusFiles"" sf
                     LEFT JOIN ""Users"" u ON u.""id"" = sf.""uploaded_by""";
        if (!string.IsNullOrWhiteSpace(search))
            sql += " WHERE sf.\"title\" ILIKE @Search OR sf.\"description\" ILIKE @Search";
        sql += " ORDER BY sf.\"created_at\" DESC";

        return (await conn.QueryAsync<SyllabusFile>(sql, new { Search = $"%{search}%" })).ToList();
    }

    public async Task<SyllabusFile?> GetByIdAsync(int id)
    {
        using var conn = _db.CreateConnection();
        var sql = @"SELECT sf.*, u.""name"" AS uploader_name 
                     FROM ""SyllabusFiles"" sf
                     LEFT JOIN ""Users"" u ON u.""id"" = sf.""uploaded_by""
                     WHERE sf.""id"" = @Id";
        return await conn.QueryFirstOrDefaultAsync<SyllabusFile>(sql, new { Id = id });
    }

    public async Task<SyllabusFile> UploadAsync(string title, string? description, string fileName, string filePath, string contentType, long fileSize, int? uploadedBy)
    {
        using var conn = _db.CreateConnection();
        var sql = @"INSERT INTO ""SyllabusFiles"" (""title"", ""description"", ""file_name"", ""file_path"", ""content_type"", ""file_size"", ""uploaded_by"", ""created_at"", ""updated_at"")
                     VALUES (@Title, @Description, @FileName, @FilePath, @ContentType, @FileSize, @UploadedBy, NOW(), NOW())
                     RETURNING *";
        return await conn.QueryFirstAsync<SyllabusFile>(sql, new { Title = title, Description = description, FileName = fileName, FilePath = filePath, ContentType = contentType, FileSize = fileSize, UploadedBy = uploadedBy });
    }

    public async Task<bool> DeleteAsync(int id, int? userId)
    {
        using var conn = _db.CreateConnection();
        var file = await conn.QueryFirstOrDefaultAsync<SyllabusFile>(
            "SELECT * FROM \"SyllabusFiles\" WHERE \"id\" = @Id", new { Id = id });
        if (file == null) return false;

        var affected = await conn.ExecuteAsync(
            "DELETE FROM \"SyllabusFiles\" WHERE \"id\" = @Id",
            new { Id = id });

        if (affected > 0)
        {
            try
            {
                var fullPath = Path.Combine(Directory.GetCurrentDirectory(), file.FilePath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
                if (File.Exists(fullPath))
                    File.Delete(fullPath);
            }
            catch { /* file cleanup best-effort */ }
        }
        return affected > 0;
    }
}
