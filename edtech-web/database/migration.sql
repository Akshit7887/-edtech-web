-- EdTechApp Neon Migration
-- Run this in Neon SQL Editor to set up the database

-- Enable required extensions
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";
CREATE EXTENSION IF NOT EXISTS "pgcrypto";

-- ============================================================
-- ENUMS
-- ============================================================
CREATE TYPE user_role AS ENUM ('teacher', 'student');
CREATE TYPE exam_status AS ENUM ('draft', 'active', 'closed');
CREATE TYPE question_difficulty AS ENUM ('easy', 'medium', 'hard');
CREATE TYPE question_status AS ENUM ('pending', 'published');
CREATE TYPE session_status AS ENUM ('not_started', 'in_progress', 'completed', 'disqualified');
CREATE TYPE session_mode AS ENUM ('exam', 'practice');
CREATE TYPE attendance_status AS ENUM ('absent', 'present');
CREATE TYPE attendance_marked_by AS ENUM ('auto', 'teacher');
CREATE TYPE notification_type AS ENUM ('exam_reminder', 'result', 'announcement', 'general');
CREATE TYPE parent_delivery_status AS ENUM ('pending', 'sent', 'failed', 'delivered');
CREATE TYPE parent_sent_via AS ENUM ('sms', 'email');

-- ============================================================
-- TABLES
-- ============================================================

-- Users table
CREATE TABLE public.users (
    id SERIAL PRIMARY KEY,
    name VARCHAR(255) NOT NULL,
    role user_role NOT NULL,
    phone VARCHAR(20) UNIQUE,
    email VARCHAR(255) UNIQUE,
    password_hash VARCHAR(255) NOT NULL DEFAULT '',
    token_version INT DEFAULT 0,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

-- Exams table
CREATE TABLE public.exams (
    id SERIAL PRIMARY KEY,
    teacher_id INT NOT NULL REFERENCES public.users(id) ON DELETE CASCADE,
    title VARCHAR(255) NOT NULL,
    subject VARCHAR(100) NOT NULL,
    syllabus_text TEXT,
    syllabus_pdf_path VARCHAR(500),
    duration_minutes INT DEFAULT 30,
    total_questions INT DEFAULT 0,
    deep_link_code VARCHAR(50) UNIQUE NOT NULL,
    status exam_status DEFAULT 'draft',
    scheduled_at TIMESTAMPTZ,
    scheduled_end_at TIMESTAMPTZ,
    allow_reattempt BOOLEAN DEFAULT FALSE,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

-- Question pool
CREATE TABLE public.question_pool (
    id SERIAL PRIMARY KEY,
    exam_id INT NOT NULL REFERENCES public.exams(id) ON DELETE CASCADE,
    student_id INT REFERENCES public.users(id) ON DELETE SET NULL,
    question_text TEXT NOT NULL,
    option_a TEXT NOT NULL,
    option_b TEXT NOT NULL,
    option_c TEXT,
    option_d TEXT,
    correct_answer CHAR(1) NOT NULL CHECK (correct_answer IN ('A','B','C','D')),
    difficulty question_difficulty DEFAULT 'medium',
    points INT DEFAULT 1,
    status question_status DEFAULT 'published',
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

-- Student exam assignments
CREATE TABLE public.student_exam_assignments (
    id SERIAL PRIMARY KEY,
    student_id INT NOT NULL REFERENCES public.users(id) ON DELETE CASCADE,
    exam_id INT NOT NULL REFERENCES public.exams(id) ON DELETE CASCADE,
    question_ids JSONB NOT NULL DEFAULT '[]',
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    UNIQUE(student_id, exam_id)
);

-- Exam sessions
CREATE TABLE public.exam_sessions (
    id SERIAL PRIMARY KEY,
    student_id INT NOT NULL REFERENCES public.users(id) ON DELETE CASCADE,
    exam_id INT NOT NULL REFERENCES public.exams(id) ON DELETE CASCADE,
    score NUMERIC(5,2) DEFAULT 0,
    total_questions INT NOT NULL,
    answered_count INT DEFAULT 0,
    status session_status DEFAULT 'not_started',
    disqualified_reason TEXT,
    started_at TIMESTAMPTZ,
    submitted_at TIMESTAMPTZ,
    time_remaining_seconds INT,
    ip_address VARCHAR(45),
    user_agent TEXT,
    mode session_mode DEFAULT 'exam',
    answers JSONB,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

-- Attendance
CREATE TABLE public.attendance (
    id SERIAL PRIMARY KEY,
    student_id INT NOT NULL REFERENCES public.users(id) ON DELETE CASCADE,
    exam_id INT NOT NULL REFERENCES public.exams(id) ON DELETE CASCADE,
    status attendance_status DEFAULT 'absent',
    marked_at TIMESTAMPTZ,
    marked_by attendance_marked_by DEFAULT 'auto',
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

-- Parent contacts
CREATE TABLE public.parent_contacts (
    id SERIAL PRIMARY KEY,
    student_id INT NOT NULL REFERENCES public.users(id) ON DELETE CASCADE,
    parent_name VARCHAR(255) NOT NULL,
    parent_phone VARCHAR(20),
    parent_email VARCHAR(255),
    relationship VARCHAR(50),
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

-- Parent notifications
CREATE TABLE public.parent_notifications (
    id SERIAL PRIMARY KEY,
    parent_contact_id INT REFERENCES public.parent_contacts(id) ON DELETE CASCADE,
    student_id INT NOT NULL REFERENCES public.users(id) ON DELETE CASCADE,
    exam_id INT NOT NULL REFERENCES public.exams(id) ON DELETE CASCADE,
    message_text TEXT NOT NULL,
    sent_via parent_sent_via,
    sent_at TIMESTAMPTZ,
    delivery_status parent_delivery_status DEFAULT 'pending',
    provider_response TEXT,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

-- Notifications (in-app)
CREATE TABLE public.notifications (
    id SERIAL PRIMARY KEY,
    user_id INT NOT NULL REFERENCES public.users(id) ON DELETE CASCADE,
    title VARCHAR(255) NOT NULL,
    message TEXT NOT NULL,
    type notification_type DEFAULT 'general',
    is_read BOOLEAN DEFAULT FALSE,
    metadata JSONB,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

-- Classes
CREATE TABLE public.classes (
    id SERIAL PRIMARY KEY,
    teacher_id INT NOT NULL REFERENCES public.users(id) ON DELETE CASCADE,
    name VARCHAR(255) NOT NULL,
    subject VARCHAR(100),
    description TEXT,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

-- Class students (junction table)
CREATE TABLE public.class_students (
    class_id INT NOT NULL REFERENCES public.classes(id) ON DELETE CASCADE,
    student_id INT NOT NULL REFERENCES public.users(id) ON DELETE CASCADE,
    PRIMARY KEY (class_id, student_id)
);

-- OTP tokens
CREATE TABLE public.otp_tokens (
    id SERIAL PRIMARY KEY,
    user_id INT NOT NULL REFERENCES public.users(id) ON DELETE CASCADE,
    otp_code VARCHAR(6) NOT NULL,
    expires_at TIMESTAMPTZ NOT NULL,
    is_used BOOLEAN DEFAULT FALSE,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

-- Pending registrations
CREATE TABLE public.pending_registrations (
    id SERIAL PRIMARY KEY,
    name VARCHAR(255) NOT NULL,
    identifier VARCHAR(255) NOT NULL,
    password_hash VARCHAR(255) NOT NULL,
    role user_role NOT NULL,
    otp_code VARCHAR(6) NOT NULL,
    expires_at TIMESTAMPTZ NOT NULL,
    is_used BOOLEAN DEFAULT FALSE,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

-- ============================================================
-- INDEXES
-- ============================================================
CREATE INDEX idx_exams_teacher_id ON public.exams(teacher_id);
CREATE INDEX idx_exams_status ON public.exams(status);
CREATE INDEX idx_question_pool_exam_id ON public.question_pool(exam_id);
CREATE INDEX idx_student_exam_assignments_student_id ON public.student_exam_assignments(student_id);
CREATE INDEX idx_student_exam_assignments_exam_id ON public.student_exam_assignments(exam_id);
CREATE INDEX idx_exam_sessions_student_id ON public.exam_sessions(student_id);
CREATE INDEX idx_exam_sessions_exam_id ON public.exam_sessions(exam_id);
CREATE INDEX idx_exam_sessions_status ON public.exam_sessions(status);
CREATE INDEX idx_attendance_exam_id ON public.attendance(exam_id);
CREATE INDEX idx_attendance_student_id ON public.attendance(student_id);
CREATE INDEX idx_notifications_user_id ON public.notifications(user_id);
CREATE INDEX idx_parent_contacts_student_id ON public.parent_contacts(student_id);
CREATE INDEX idx_otp_tokens_user_id ON public.otp_tokens(user_id);

-- ============================================================
-- AUTO-UPDATE updated_at TRIGGER
-- ============================================================
CREATE OR REPLACE FUNCTION update_updated_at_column()
RETURNS TRIGGER AS $$
BEGIN
    NEW.updated_at = NOW();
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

DO $$
DECLARE
    t text;
BEGIN
    FOR t IN SELECT table_name FROM information_schema.tables 
              WHERE table_schema = 'public' AND table_type = 'BASE TABLE'
              AND table_name NOT IN ('class_students')
    LOOP
        EXECUTE format('CREATE TRIGGER set_updated_at BEFORE UPDATE ON %I FOR EACH ROW EXECUTE FUNCTION update_updated_at_column()', t);
    END LOOP;
END;
$$;

-- ============================================================
-- FUNCTIONS
-- ============================================================

-- Generate unique deep link code
CREATE OR REPLACE FUNCTION generate_deep_link_code()
RETURNS VARCHAR(50) AS $$
DECLARE
    code VARCHAR(50);
BEGIN
    code := upper(substr(md5(random()::text || clock_timestamp()::text), 1, 8));
    WHILE EXISTS (SELECT 1 FROM public.exams WHERE deep_link_code = code) LOOP
        code := upper(substr(md5(random()::text || clock_timestamp()::text), 1, 8));
    END LOOP;
    RETURN code;
END;
$$ LANGUAGE plpgsql;
