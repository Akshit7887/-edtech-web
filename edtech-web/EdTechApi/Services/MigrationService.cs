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
            ["010_classes_add_subject"] = @"
ALTER TABLE ""Classes"" ADD COLUMN IF NOT EXISTS ""subject"" VARCHAR(100);",
            ["011_syllabus_class_link"] = @"
ALTER TABLE ""SyllabusFiles"" ADD COLUMN IF NOT EXISTS ""file_data"" BYTEA;
ALTER TABLE ""SyllabusFiles"" ALTER COLUMN ""file_path"" DROP NOT NULL;
ALTER TABLE ""SyllabusFiles"" ADD COLUMN IF NOT EXISTS ""class_id"" INTEGER REFERENCES ""Classes""(""id"") ON DELETE CASCADE;
CREATE INDEX IF NOT EXISTS idx_syllabus_files_class_id ON ""SyllabusFiles""(""class_id"");",
            ["012_teacher_approval"] = @"
ALTER TABLE ""Users"" ADD COLUMN IF NOT EXISTS ""approval_status"" VARCHAR(20) NOT NULL DEFAULT 'approved';
ALTER TABLE ""Users"" ADD COLUMN IF NOT EXISTS ""rejection_reason"" TEXT;
ALTER TABLE ""Users"" ADD COLUMN IF NOT EXISTS ""approved_at"" TIMESTAMP WITH TIME ZONE;",
            ["013_add_indexes_and_fks"] = @"
-- Users table indexes
CREATE INDEX IF NOT EXISTS idx_users_email ON ""Users""(""email"");
CREATE INDEX IF NOT EXISTS idx_users_student_id ON ""Users""(""student_id"");
CREATE INDEX IF NOT EXISTS idx_users_role ON ""Users""(""role"");
CREATE INDEX IF NOT EXISTS idx_users_approval_status ON ""Users""(""approval_status"");

-- Exams table indexes
CREATE INDEX IF NOT EXISTS idx_exams_teacher_id ON ""Exams""(""teacher_id"");
CREATE INDEX IF NOT EXISTS idx_exams_status ON ""Exams""(""status"");
CREATE INDEX IF NOT EXISTS idx_exams_scheduled_at ON ""Exams""(""scheduled_at"");
CREATE INDEX IF NOT EXISTS idx_exams_deep_link_code ON ""Exams""(""deep_link_code"");

-- QuestionPool table indexes
CREATE INDEX IF NOT EXISTS idx_question_pool_exam_id ON ""QuestionPool""(""exam_id"");
CREATE INDEX IF NOT EXISTS idx_question_pool_student_id ON ""QuestionPool""(""student_id"");
CREATE INDEX IF NOT EXISTS idx_question_pool_status ON ""QuestionPool""(""status"");

-- ExamSessions table indexes
CREATE INDEX IF NOT EXISTS idx_exam_sessions_student_id ON ""ExamSessions""(""student_id"");
CREATE INDEX IF NOT EXISTS idx_exam_sessions_exam_id ON ""ExamSessions""(""exam_id"");
CREATE INDEX IF NOT EXISTS idx_exam_sessions_status ON ""ExamSessions""(""status"");
CREATE INDEX IF NOT EXISTS idx_exam_sessions_created_at ON ""ExamSessions""(""created_at"" DESC);

-- StudentExamAssignments table indexes
CREATE INDEX IF NOT EXISTS idx_student_exam_assignments_student_id ON ""StudentExamAssignments""(""student_id"");
CREATE INDEX IF NOT EXISTS idx_student_exam_assignments_exam_id ON ""StudentExamAssignments""(""exam_id"");
-- Composite unique constraint for student-exam assignment
DO $$ BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'uq_student_exam_assignment') THEN
        ALTER TABLE ""StudentExamAssignments"" ADD CONSTRAINT uq_student_exam_assignment UNIQUE (""student_id"", ""exam_id"");
    END IF;
END $$;

-- OtpTokens table indexes
CREATE INDEX IF NOT EXISTS idx_otp_tokens_user_id ON ""OtpTokens""(""user_id"");
CREATE INDEX IF NOT EXISTS idx_otp_tokens_expires_at ON ""OtpTokens""(""expires_at"");

-- Notifications table indexes
CREATE INDEX IF NOT EXISTS idx_notifications_user_id ON ""Notifications""(""user_id"");
CREATE INDEX IF NOT EXISTS idx_notifications_is_read ON ""Notifications""(""is_read"");
CREATE INDEX IF NOT EXISTS idx_notifications_created_at ON ""Notifications""(""created_at"" DESC);

-- Classes table indexes
CREATE INDEX IF NOT EXISTS idx_classes_teacher_id ON ""Classes""(""teacher_id"");

-- ClassStudents table indexes
CREATE INDEX IF NOT EXISTS idx_class_students_class_id ON ""ClassStudents""(""class_id"");
CREATE INDEX IF NOT EXISTS idx_class_students_student_id ON ""ClassStudents""(""student_id"");
-- Composite unique constraint
DO $$ BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'uq_class_student') THEN
        ALTER TABLE ""ClassStudents"" ADD CONSTRAINT uq_class_student UNIQUE (""class_id"", ""student_id"");
    END IF;
END $$;

-- ParentContacts table indexes
CREATE INDEX IF NOT EXISTS idx_parent_contacts_student_id ON ""ParentContacts""(""student_id"");

-- SyllabusFiles table indexes
CREATE INDEX IF NOT EXISTS idx_syllabus_files_class_id ON ""SyllabusFiles""(""class_id"");

-- Foreign key constraints (add if not exist)
DO $$ BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'fk_exams_teacher_id') THEN
        ALTER TABLE ""Exams"" ADD CONSTRAINT fk_exams_teacher_id FOREIGN KEY (""teacher_id"") REFERENCES ""Users""(""id"") ON DELETE CASCADE;
    END IF;
END $$;

DO $$ BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'fk_question_pool_exam_id') THEN
        ALTER TABLE ""QuestionPool"" ADD CONSTRAINT fk_question_pool_exam_id FOREIGN KEY (""exam_id"") REFERENCES ""Exams""(""id"") ON DELETE CASCADE;
    END IF;
END $$;

DO $$ BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'fk_question_pool_student_id') THEN
        ALTER TABLE ""QuestionPool"" ADD CONSTRAINT fk_question_pool_student_id FOREIGN KEY (""student_id"") REFERENCES ""Users""(""id"") ON DELETE SET NULL;
    END IF;
END $$;

DO $$ BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'fk_exam_sessions_student_id') THEN
        ALTER TABLE ""ExamSessions"" ADD CONSTRAINT fk_exam_sessions_student_id FOREIGN KEY (""student_id"") REFERENCES ""Users""(""id"") ON DELETE CASCADE;
    END IF;
END $$;

DO $$ BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'fk_exam_sessions_exam_id') THEN
        ALTER TABLE ""ExamSessions"" ADD CONSTRAINT fk_exam_sessions_exam_id FOREIGN KEY (""exam_id"") REFERENCES ""Exams""(""id"") ON DELETE CASCADE;
    END IF;
END $$;

DO $$ BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'fk_attendance_student_id') THEN
        ALTER TABLE ""Attendance"" ADD CONSTRAINT fk_attendance_student_id FOREIGN KEY (""student_id"") REFERENCES ""Users""(""id"") ON DELETE CASCADE;
    END IF;
END $$;

DO $$ BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'fk_attendance_exam_id') THEN
        ALTER TABLE ""Attendance"" ADD CONSTRAINT fk_attendance_exam_id FOREIGN KEY (""exam_id"") REFERENCES ""Exams""(""id"") ON DELETE CASCADE;
    END IF;
END $$;

DO $$ BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'fk_parent_contacts_student_id') THEN
        ALTER TABLE ""ParentContacts"" ADD CONSTRAINT fk_parent_contacts_student_id FOREIGN KEY (""student_id"") REFERENCES ""Users""(""id"") ON DELETE CASCADE;
    END IF;
END $$;

DO $$ BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'fk_parent_notifications_student_id') THEN
        ALTER TABLE ""ParentNotifications"" ADD CONSTRAINT fk_parent_notifications_student_id FOREIGN KEY (""student_id"") REFERENCES ""Users""(""id"") ON DELETE CASCADE;
    END IF;
END $$;

DO $$ BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'fk_parent_notifications_exam_id') THEN
        ALTER TABLE ""ParentNotifications"" ADD CONSTRAINT fk_parent_notifications_exam_id FOREIGN KEY (""exam_id"") REFERENCES ""Exams""(""id"") ON DELETE CASCADE;
    END IF;
END $$;

DO $$ BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'fk_parent_notifications_contact_id') THEN
        ALTER TABLE ""ParentNotifications"" ADD CONSTRAINT fk_parent_notifications_contact_id FOREIGN KEY (""parent_contact_id"") REFERENCES ""ParentContacts""(""id"") ON DELETE CASCADE;
    END IF;
END $$;
",
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
