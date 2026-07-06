-- Local Dev Migration for Neon (PascalCase tables matching C# code)

CREATE EXTENSION IF NOT EXISTS "uuid-ossp";
CREATE EXTENSION IF NOT EXISTS "pgcrypto";

-- TABLES (PascalCase to match C# SQL queries, VARCHAR instead of enums for simpler dev)
CREATE TABLE "Users" (
    "id" SERIAL PRIMARY KEY,
    "name" VARCHAR(255) NOT NULL,
    "role" VARCHAR(20) NOT NULL DEFAULT 'student',
    "phone" VARCHAR(20) UNIQUE,
    "email" VARCHAR(255) UNIQUE,
    "password_hash" VARCHAR(255) NOT NULL DEFAULT '',
    "token_version" INT DEFAULT 0,
    "created_at" TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    "updated_at" TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE TABLE "Exams" (
    "id" SERIAL PRIMARY KEY,
    "teacher_id" INT NOT NULL REFERENCES "Users"(id) ON DELETE CASCADE,
    "title" VARCHAR(255) NOT NULL,
    "subject" VARCHAR(100) NOT NULL,
    "syllabus_text" TEXT,
    "syllabus_pdf_path" VARCHAR(500),
    "duration_minutes" INT DEFAULT 30,
    "total_questions" INT DEFAULT 0,
    "deep_link_code" VARCHAR(50) UNIQUE NOT NULL,
    "status" VARCHAR(20) DEFAULT 'draft',
    "scheduled_at" TIMESTAMPTZ,
    "scheduled_end_at" TIMESTAMPTZ,
    "allow_reattempt" BOOLEAN DEFAULT FALSE,
    "created_at" TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    "updated_at" TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE TABLE "QuestionPool" (
    "id" SERIAL PRIMARY KEY,
    "exam_id" INT NOT NULL REFERENCES "Exams"(id) ON DELETE CASCADE,
    "student_id" INT REFERENCES "Users"(id) ON DELETE SET NULL,
    "question_text" TEXT NOT NULL,
    "option_a" TEXT NOT NULL,
    "option_b" TEXT NOT NULL,
    "option_c" TEXT,
    "option_d" TEXT,
    "correct_answer" CHAR(1) NOT NULL CHECK ("correct_answer" IN ('A','B','C','D')),
    "difficulty" VARCHAR(20) DEFAULT 'medium',
    "points" INT DEFAULT 1,
    "status" VARCHAR(20) DEFAULT 'published',
    "created_at" TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    "updated_at" TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE TABLE "StudentExamAssignments" (
    "id" SERIAL PRIMARY KEY,
    "student_id" INT NOT NULL REFERENCES "Users"(id) ON DELETE CASCADE,
    "exam_id" INT NOT NULL REFERENCES "Exams"(id) ON DELETE CASCADE,
    "question_ids" JSONB NOT NULL DEFAULT '[]',
    "created_at" TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    "updated_at" TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    UNIQUE("student_id", "exam_id")
);
-- Ensure no stale single-column unique constraints (Supabase artifact)
ALTER TABLE "StudentExamAssignments" DROP CONSTRAINT IF EXISTS "StudentExamAssignments_student_id_key";
ALTER TABLE "StudentExamAssignments" DROP CONSTRAINT IF EXISTS "StudentExamAssignments_exam_id_key";

CREATE TABLE "ExamSessions" (
    "id" SERIAL PRIMARY KEY,
    "student_id" INT NOT NULL REFERENCES "Users"(id) ON DELETE CASCADE,
    "exam_id" INT NOT NULL REFERENCES "Exams"(id) ON DELETE CASCADE,
    "score" NUMERIC(5,2) DEFAULT 0,
    "total_questions" INT NOT NULL,
    "answered_count" INT DEFAULT 0,
    "status" VARCHAR(20) DEFAULT 'not_started',
    "disqualified_reason" TEXT,
    "started_at" TIMESTAMPTZ,
    "submitted_at" TIMESTAMPTZ,
    "time_remaining_seconds" INT,
    "ip_address" VARCHAR(45),
    "user_agent" TEXT,
    "mode" VARCHAR(20) DEFAULT 'exam',
    "answers" JSONB,
    "created_at" TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    "updated_at" TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE TABLE "Attendance" (
    "id" SERIAL PRIMARY KEY,
    "student_id" INT NOT NULL REFERENCES "Users"(id) ON DELETE CASCADE,
    "exam_id" INT NOT NULL REFERENCES "Exams"(id) ON DELETE CASCADE,
    "status" VARCHAR(20) DEFAULT 'absent',
    "marked_at" TIMESTAMPTZ,
    "marked_by" VARCHAR(20) DEFAULT 'auto',
    "created_at" TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    "updated_at" TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE TABLE "ParentContacts" (
    "id" SERIAL PRIMARY KEY,
    "student_id" INT NOT NULL REFERENCES "Users"(id) ON DELETE CASCADE,
    "parent_name" VARCHAR(255) NOT NULL,
    "parent_phone" VARCHAR(20),
    "parent_email" VARCHAR(255),
    "relationship" VARCHAR(50),
    "created_at" TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    "updated_at" TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE TABLE "ParentNotifications" (
    "id" SERIAL PRIMARY KEY,
    "parent_contact_id" INT REFERENCES "ParentContacts"(id) ON DELETE CASCADE,
    "student_id" INT NOT NULL REFERENCES "Users"(id) ON DELETE CASCADE,
    "exam_id" INT NOT NULL REFERENCES "Exams"(id) ON DELETE CASCADE,
    "message_text" TEXT NOT NULL,
    "sent_via" VARCHAR(20),
    "sent_at" TIMESTAMPTZ,
    "delivery_status" VARCHAR(20) DEFAULT 'pending',
    "provider_response" TEXT,
    "created_at" TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    "updated_at" TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE TABLE "Notifications" (
    "id" SERIAL PRIMARY KEY,
    "user_id" INT NOT NULL REFERENCES "Users"(id) ON DELETE CASCADE,
    "title" VARCHAR(255) NOT NULL,
    "message" TEXT NOT NULL,
    "type" VARCHAR(30) DEFAULT 'general',
    "is_read" BOOLEAN DEFAULT FALSE,
    "metadata" JSONB,
    "created_at" TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    "updated_at" TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE TABLE "Classes" (
    "id" SERIAL PRIMARY KEY,
    "teacher_id" INT NOT NULL REFERENCES "Users"(id) ON DELETE CASCADE,
    "name" VARCHAR(255) NOT NULL,
    "subject" VARCHAR(100),
    "description" TEXT,
    "created_at" TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    "updated_at" TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE TABLE "ClassStudents" (
    "class_id" INT NOT NULL REFERENCES "Classes"(id) ON DELETE CASCADE,
    "student_id" INT NOT NULL REFERENCES "Users"(id) ON DELETE CASCADE,
    PRIMARY KEY ("class_id", "student_id")
);

CREATE TABLE "OtpTokens" (
    "id" SERIAL PRIMARY KEY,
    "user_id" INT NOT NULL REFERENCES "Users"(id) ON DELETE CASCADE,
    "otp_code" VARCHAR(6) NOT NULL,
    "expires_at" TIMESTAMPTZ NOT NULL,
    "is_used" BOOLEAN DEFAULT FALSE,
    "created_at" TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    "updated_at" TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE TABLE "PendingRegistrations" (
    "id" SERIAL PRIMARY KEY,
    "name" VARCHAR(255) NOT NULL,
    "identifier" VARCHAR(255) NOT NULL,
    "password_hash" VARCHAR(255) NOT NULL,
    "role" VARCHAR(20) NOT NULL,
    "phone" VARCHAR(20),
    "email" VARCHAR(255),
    "otp_code" VARCHAR(6) NOT NULL,
    "expires_at" TIMESTAMPTZ NOT NULL,
    "is_used" BOOLEAN DEFAULT FALSE,
    "created_at" TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    "updated_at" TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

-- INDEXES
CREATE INDEX idx_exams_teacher_id ON "Exams"("teacher_id");
CREATE INDEX idx_exams_status ON "Exams"("status");
CREATE INDEX idx_question_pool_exam_id ON "QuestionPool"("exam_id");
CREATE INDEX idx_student_exam_assignments_student_id ON "StudentExamAssignments"("student_id");
CREATE INDEX idx_student_exam_assignments_exam_id ON "StudentExamAssignments"("exam_id");
CREATE INDEX idx_exam_sessions_student_id ON "ExamSessions"("student_id");
CREATE INDEX idx_exam_sessions_exam_id ON "ExamSessions"("exam_id");
CREATE INDEX idx_exam_sessions_status ON "ExamSessions"("status");
CREATE INDEX idx_attendance_exam_id ON "Attendance"("exam_id");
CREATE INDEX idx_attendance_student_id ON "Attendance"("student_id");
CREATE INDEX idx_notifications_user_id ON "Notifications"("user_id");
CREATE INDEX idx_parent_contacts_student_id ON "ParentContacts"("student_id");
CREATE INDEX idx_otp_tokens_user_id ON "OtpTokens"("user_id");

-- AUTO-UPDATE updated_at TRIGGER
CREATE OR REPLACE FUNCTION update_updated_at_column()
RETURNS TRIGGER AS $$
BEGIN
    NEW."updated_at" = NOW();
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

DO $$
DECLARE
    t text;
BEGIN
    FOR t IN SELECT table_name FROM information_schema.tables
              WHERE table_schema = 'public' AND table_type = 'BASE TABLE'
              AND table_name NOT IN ('ClassStudents')
    LOOP
        EXECUTE format('CREATE TRIGGER set_updated_at BEFORE UPDATE ON %I FOR EACH ROW EXECUTE FUNCTION update_updated_at_column()', t);
    END LOOP;
END;
$$;

-- Generate unique deep link code
CREATE OR REPLACE FUNCTION generate_deep_link_code()
RETURNS VARCHAR(50) AS $$
DECLARE
    code VARCHAR(50);
BEGIN
    code := upper(substr(md5(random()::text || clock_timestamp()::text), 1, 8));
    WHILE EXISTS (SELECT 1 FROM "Exams" WHERE "deep_link_code" = code) LOOP
        code := upper(substr(md5(random()::text || clock_timestamp()::text), 1, 8));
    END LOOP;
    RETURN code;
END;
$$ LANGUAGE plpgsql;

-- Cleanup: Disable Row Level Security (leftover from Supabase, auth handled in C# layer)
ALTER TABLE "Users" DISABLE ROW LEVEL SECURITY;
ALTER TABLE "Exams" DISABLE ROW LEVEL SECURITY;
ALTER TABLE "QuestionPool" DISABLE ROW LEVEL SECURITY;
ALTER TABLE "StudentExamAssignments" DISABLE ROW LEVEL SECURITY;
ALTER TABLE "ExamSessions" DISABLE ROW LEVEL SECURITY;
ALTER TABLE "Attendance" DISABLE ROW LEVEL SECURITY;
ALTER TABLE "ParentContacts" DISABLE ROW LEVEL SECURITY;
ALTER TABLE "ParentNotifications" DISABLE ROW LEVEL SECURITY;
ALTER TABLE "Notifications" DISABLE ROW LEVEL SECURITY;
ALTER TABLE "Classes" DISABLE ROW LEVEL SECURITY;
ALTER TABLE "ClassStudents" DISABLE ROW LEVEL SECURITY;
ALTER TABLE "OtpTokens" DISABLE ROW LEVEL SECURITY;
ALTER TABLE "PendingRegistrations" DISABLE ROW LEVEL SECURITY;
ALTER TABLE "SyllabusFiles" DISABLE ROW LEVEL SECURITY;

-- Drop unused auth_uid column (Supabase artifact, not used since migration to Neon)
ALTER TABLE "Users" DROP COLUMN IF EXISTS "auth_uid";
