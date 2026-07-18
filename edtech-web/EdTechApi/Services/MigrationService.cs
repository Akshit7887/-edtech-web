using Dapper;
using EdTechApi.Data;
using EdTechApi.Models;

namespace EdTechApi.Services;

public interface IMigrationService
{
    Task ApplyMigrationsAsync();
}

public class MigrationService : IMigrationService
{
    private readonly IDbConnectionFactory _db;
    private readonly ILogger<MigrationService> _logger;

    public MigrationService(IDbConnectionFactory db, ILogger<MigrationService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task ApplyMigrationsAsync()
    {
        using var conn = _db.CreateConnection();
        await conn.OpenAsync();

        await conn.ExecuteAsync(
            @"CREATE TABLE IF NOT EXISTS ""_Migrations"" (""migration"" VARCHAR(255) PRIMARY KEY, ""applied_at"" TIMESTAMP WITH TIME ZONE DEFAULT NOW())");

        var migrations = new Dictionary<string, string>
        {
            ["006_cleanup_supabase_artifacts"] = @"
-- Fix broken uniqueness on StudentExamAssignments
ALTER TABLE ""StudentExamAssignments"" DROP CONSTRAINT IF EXISTS ""StudentExamAssignments_student_id_key"";
ALTER TABLE ""StudentExamAssignments"" DROP CONSTRAINT IF EXISTS ""StudentExamAssignments_exam_id_key"";

-- Disable Row Level Security on all tables (leftover from Supabase)
ALTER TABLE ""Users"" DISABLE ROW LEVEL SECURITY;
ALTER TABLE ""Exams"" DISABLE ROW LEVEL SECURITY;
ALTER TABLE ""QuestionPool"" DISABLE ROW LEVEL SECURITY;
ALTER TABLE ""StudentExamAssignments"" DISABLE ROW LEVEL SECURITY;
ALTER TABLE ""ExamSessions"" DISABLE ROW LEVEL SECURITY;
ALTER TABLE ""Attendance"" DISABLE ROW LEVEL SECURITY;
ALTER TABLE ""ParentContacts"" DISABLE ROW LEVEL SECURITY;
ALTER TABLE ""ParentNotifications"" DISABLE ROW LEVEL SECURITY;
ALTER TABLE ""Notifications"" DISABLE ROW LEVEL SECURITY;
ALTER TABLE ""Classes"" DISABLE ROW LEVEL SECURITY;
ALTER TABLE ""ClassStudents"" DISABLE ROW LEVEL SECURITY;
ALTER TABLE ""OtpTokens"" DISABLE ROW LEVEL SECURITY;
ALTER TABLE ""PendingRegistrations"" DISABLE ROW LEVEL SECURITY;
ALTER TABLE ""SyllabusFiles"" DISABLE ROW LEVEL SECURITY;

-- Drop unused auth_uid column from Users
ALTER TABLE ""Users"" DROP COLUMN IF EXISTS ""auth_uid"";",
            ["004_create_syllabus_files"] = @"
CREATE TABLE IF NOT EXISTS ""SyllabusFiles"" (
    ""id"" SERIAL PRIMARY KEY,
    ""title"" VARCHAR(255) NOT NULL,
    ""description"" TEXT,
    ""file_name"" VARCHAR(255) NOT NULL,
    ""file_path"" VARCHAR(500) NOT NULL,
    ""content_type"" VARCHAR(100) NOT NULL,
    ""file_size"" BIGINT NOT NULL,
    ""uploaded_by"" INTEGER REFERENCES ""Users""(""id"") ON DELETE SET NULL,
    ""created_at"" TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
    ""updated_at"" TIMESTAMP WITH TIME ZONE DEFAULT NOW()
);
CREATE INDEX IF NOT EXISTS idx_syllabus_files_uploaded_by ON ""SyllabusFiles""(""uploaded_by"");
CREATE INDEX IF NOT EXISTS idx_syllabus_files_created_at ON ""SyllabusFiles""(""created_at"" DESC);",
            ["007_create_departments"] = @"
CREATE TABLE IF NOT EXISTS ""Departments"" (
    ""id"" SERIAL PRIMARY KEY,
    ""name"" VARCHAR(255) NOT NULL UNIQUE,
    ""description"" TEXT,
    ""head_id"" INTEGER REFERENCES ""Users""(""id"") ON DELETE SET NULL,
    ""created_at"" TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
    ""updated_at"" TIMESTAMP WITH TIME ZONE DEFAULT NOW()
);
ALTER TABLE ""Users"" ADD COLUMN IF NOT EXISTS ""department_id"" INTEGER REFERENCES ""Departments""(""id"") ON DELETE SET NULL;
CREATE INDEX IF NOT EXISTS idx_users_department_id ON ""Users""(""department_id"");",
            ["005_syllabus_files_nullable_uploaded_by"] = @"
ALTER TABLE ""SyllabusFiles"" ALTER COLUMN ""uploaded_by"" DROP NOT NULL;
ALTER TABLE ""SyllabusFiles"" DROP CONSTRAINT IF EXISTS ""SyllabusFiles_uploaded_by_fkey"";
ALTER TABLE ""SyllabusFiles"" ADD CONSTRAINT ""SyllabusFiles_uploaded_by_fkey"" FOREIGN KEY (""uploaded_by"") REFERENCES ""Users""(""id"") ON DELETE SET NULL;",
            ["008_add_student_id"] = @"
ALTER TABLE ""Users"" ADD COLUMN IF NOT EXISTS ""student_id"" VARCHAR(10) UNIQUE;",
            ["009_syllabus_file_data"] = @"
ALTER TABLE ""SyllabusFiles"" ADD COLUMN IF NOT EXISTS ""file_data"" BYTEA;",

        };

        foreach (var (name, sql) in migrations)
        {
            try
            {
                var exists = await conn.QueryFirstOrDefaultAsync<string>(
                    "SELECT \"migration\" FROM \"_Migrations\" WHERE \"migration\" = @Name",
                    new { Name = name });

                if (exists != null) continue;

                using var tx = conn.BeginTransaction();
                await conn.ExecuteAsync(sql, transaction: tx);
                await conn.ExecuteAsync(
                    "INSERT INTO \"_Migrations\" (\"migration\") VALUES (@Name) ON CONFLICT DO NOTHING",
                    new { Name = name }, transaction: tx);

                tx.Commit();
                _logger.LogInformation("Migration applied: {Migration}", name);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Migration failed: {Migration}", name);
            }
        }

        // Backfill student_id for existing students who don't have one
        var studentsWithoutId = (await conn.QueryAsync<User>(
            "SELECT * FROM \"Users\" WHERE \"role\" = 'student' AND \"student_id\" IS NULL")).AsList();

        if (studentsWithoutId.Count > 0)
        {
            _logger.LogInformation("Backfilling student_id for {Count} existing students", studentsWithoutId.Count);
            var random = new Random();
            foreach (var student in studentsWithoutId)
            {
                string sid;
                do
                {
                    sid = random.Next(0, 1000000000).ToString("D10");
                } while (await conn.QueryFirstOrDefaultAsync<string>(
                    "SELECT 1 FROM \"Users\" WHERE \"student_id\" = @Sid",
                    new { Sid = sid }) != null);

                await conn.ExecuteAsync(
                    "UPDATE \"Users\" SET \"student_id\" = @Sid WHERE \"id\" = @Id",
                    new { Sid = sid, Id = student.Id });
            }
            _logger.LogInformation("Backfilled student_id for {Count} students", studentsWithoutId.Count);
        }
    }
}
