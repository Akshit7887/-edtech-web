using Dapper;
using EdTechApi.Data;
using EdTechApi.Models;

namespace EdTechApi.Services;

public interface ISyllabusService
{
    Task<List<SyllabusFile>> GetAllAsync(string? search = null, int? classId = null);
    Task<List<SyllabusFile>> GetStudentFilesAsync(int studentId);
    Task<SyllabusFile?> GetByIdAsync(int id);
    Task<SyllabusFile> UploadAsync(string title, string? description, string fileName, byte[] fileData, string contentType, long fileSize, int? uploadedBy, int? classId);
    Task<bool> DeleteAsync(int id, int? userId);
    Task<bool> UpdateClassAsync(int id, int? classId);
}

public class SyllabusService : ISyllabusService
{
    private readonly IDbConnectionFactory _db;

    public SyllabusService(IDbConnectionFactory db)
    {
        _db = db;
    }

    public async Task<List<SyllabusFile>> GetAllAsync(string? search = null, int? classId = null)
    {
        using var conn = _db.CreateConnection();
        var sql = @"SELECT sf.""id"", sf.""title"", sf.""description"", sf.""file_name"", sf.""content_type"", sf.""file_size"", sf.""uploaded_by"", sf.""class_id"", u.""name"" AS uploader_name, c.""name"" AS class_name, c.""subject"" AS class_subject, sf.""created_at"", sf.""updated_at""
                     FROM ""SyllabusFiles"" sf
                     LEFT JOIN ""Users"" u ON u.""id"" = sf.""uploaded_by""
                     LEFT JOIN ""Classes"" c ON c.""id"" = sf.""class_id""
                     WHERE (@ClassId IS NULL OR sf.""class_id"" = @ClassId)";
        if (!string.IsNullOrWhiteSpace(search))
            sql += " AND (sf.\"title\" ILIKE @Search OR sf.\"description\" ILIKE @Search)";
        sql += " ORDER BY sf.\"created_at\" DESC";

        return (await conn.QueryAsync<SyllabusFile>(sql, new { Search = $"%{search}%", ClassId = classId })).ToList();
    }

    public async Task<List<SyllabusFile>> GetStudentFilesAsync(int studentId)
    {
        using var conn = _db.CreateConnection();
        var sql = @"SELECT sf.""id"", sf.""title"", sf.""description"", sf.""file_name"", sf.""content_type"", sf.""file_size"", sf.""uploaded_by"", sf.""class_id"", u.""name"" AS uploader_name, c.""name"" AS class_name, c.""subject"" AS class_subject, sf.""created_at"", sf.""updated_at""
                     FROM ""SyllabusFiles"" sf
                     LEFT JOIN ""Users"" u ON u.""id"" = sf.""uploaded_by""
                     JOIN ""Classes"" c ON c.""id"" = sf.""class_id""
                     JOIN ""ClassStudents"" cs ON cs.""class_id"" = sf.""class_id""
                     WHERE cs.""student_id"" = @StudentId
                     ORDER BY c.""name"" ASC, sf.""created_at"" DESC";

        return (await conn.QueryAsync<SyllabusFile>(sql, new { StudentId = studentId })).ToList();
    }

    public async Task<SyllabusFile?> GetByIdAsync(int id)
    {
        using var conn = _db.CreateConnection();
        var sql = @"SELECT sf.*, u.""name"" AS uploader_name, c.""name"" AS class_name, c.""subject"" AS class_subject
                     FROM ""SyllabusFiles"" sf
                     LEFT JOIN ""Users"" u ON u.""id"" = sf.""uploaded_by""
                     LEFT JOIN ""Classes"" c ON c.""id"" = sf.""class_id""
                     WHERE sf.""id"" = @Id";
        return await conn.QueryFirstOrDefaultAsync<SyllabusFile>(sql, new { Id = id });
    }

    public async Task<SyllabusFile> UploadAsync(string title, string? description, string fileName, byte[] fileData, string contentType, long fileSize, int? uploadedBy, int? classId)
    {
        using var conn = _db.CreateConnection();
        var sql = @"INSERT INTO ""SyllabusFiles"" (""title"", ""description"", ""file_name"", ""file_data"", ""content_type"", ""file_size"", ""uploaded_by"", ""class_id"", ""created_at"", ""updated_at"")
                     VALUES (@Title, @Description, @FileName, @FileData, @ContentType, @FileSize, @UploadedBy, @ClassId, NOW(), NOW())
                     RETURNING *";
        return await conn.QueryFirstAsync<SyllabusFile>(sql, new { Title = title, Description = description, FileName = fileName, FileData = fileData, ContentType = contentType, FileSize = fileSize, UploadedBy = uploadedBy, ClassId = classId });
    }

    public async Task<bool> UpdateClassAsync(int id, int? classId)
    {
        using var conn = _db.CreateConnection();
        var affected = await conn.ExecuteAsync(
            "UPDATE \"SyllabusFiles\" SET \"class_id\" = @ClassId, \"updated_at\" = NOW() WHERE \"id\" = @Id",
            new { Id = id, ClassId = classId });
        return affected > 0;
    }

    public async Task<bool> DeleteAsync(int id, int? userId)
    {
        using var conn = _db.CreateConnection();
        var affected = await conn.ExecuteAsync(
            "DELETE FROM \"SyllabusFiles\" WHERE \"id\" = @Id",
            new { Id = id });
        return affected > 0;
    }
}
