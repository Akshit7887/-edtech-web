--
-- PostgreSQL database dump
--

\restrict tOhikSeLV6kkdunrgvPyige0FotwmtbhZX2hGr5cmAi0kZcszUyhVdJWhS26zje

-- Dumped from database version 18.4
-- Dumped by pg_dump version 18.4

SET statement_timeout = 0;
SET lock_timeout = 0;
SET idle_in_transaction_session_timeout = 0;
SET transaction_timeout = 0;
SET client_encoding = 'UTF8';
SET standard_conforming_strings = on;
SELECT pg_catalog.set_config('search_path', '', false);
SET check_function_bodies = false;
SET xmloption = content;
SET client_min_messages = warning;
SET row_security = off;

ALTER TABLE IF EXISTS ONLY public."Users" DROP CONSTRAINT IF EXISTS "Users_department_id_fkey";
ALTER TABLE IF EXISTS ONLY public."SyllabusFiles" DROP CONSTRAINT IF EXISTS "SyllabusFiles_uploaded_by_fkey";
ALTER TABLE IF EXISTS ONLY public."StudentExamAssignments" DROP CONSTRAINT IF EXISTS "StudentExamAssignments_student_id_fkey";
ALTER TABLE IF EXISTS ONLY public."StudentExamAssignments" DROP CONSTRAINT IF EXISTS "StudentExamAssignments_exam_id_fkey";
ALTER TABLE IF EXISTS ONLY public."QuestionPool" DROP CONSTRAINT IF EXISTS "QuestionPool_student_id_fkey";
ALTER TABLE IF EXISTS ONLY public."QuestionPool" DROP CONSTRAINT IF EXISTS "QuestionPool_exam_id_fkey";
ALTER TABLE IF EXISTS ONLY public."PendingRegistrations" DROP CONSTRAINT IF EXISTS "PendingRegistrations_auth_uid_fkey";
ALTER TABLE IF EXISTS ONLY public."ParentNotifications" DROP CONSTRAINT IF EXISTS "ParentNotifications_student_id_fkey";
ALTER TABLE IF EXISTS ONLY public."ParentNotifications" DROP CONSTRAINT IF EXISTS "ParentNotifications_parent_contact_id_fkey";
ALTER TABLE IF EXISTS ONLY public."ParentNotifications" DROP CONSTRAINT IF EXISTS "ParentNotifications_exam_id_fkey";
ALTER TABLE IF EXISTS ONLY public."ParentContacts" DROP CONSTRAINT IF EXISTS "ParentContacts_student_id_fkey";
ALTER TABLE IF EXISTS ONLY public."OtpTokens" DROP CONSTRAINT IF EXISTS "OtpTokens_user_id_fkey";
ALTER TABLE IF EXISTS ONLY public."Notifications" DROP CONSTRAINT IF EXISTS "Notifications_user_id_fkey";
ALTER TABLE IF EXISTS ONLY public."Exams" DROP CONSTRAINT IF EXISTS "Exams_teacher_id_fkey";
ALTER TABLE IF EXISTS ONLY public."ExamSessions" DROP CONSTRAINT IF EXISTS "ExamSessions_student_id_fkey";
ALTER TABLE IF EXISTS ONLY public."ExamSessions" DROP CONSTRAINT IF EXISTS "ExamSessions_exam_id_fkey";
ALTER TABLE IF EXISTS ONLY public."Departments" DROP CONSTRAINT IF EXISTS "Departments_head_id_fkey";
ALTER TABLE IF EXISTS ONLY public."Classes" DROP CONSTRAINT IF EXISTS "Classes_teacher_id_fkey";
ALTER TABLE IF EXISTS ONLY public."ClassStudents" DROP CONSTRAINT IF EXISTS "ClassStudents_student_id_fkey";
ALTER TABLE IF EXISTS ONLY public."ClassStudents" DROP CONSTRAINT IF EXISTS "ClassStudents_class_id_fkey";
ALTER TABLE IF EXISTS ONLY public."Attendance" DROP CONSTRAINT IF EXISTS "Attendance_student_id_fkey";
ALTER TABLE IF EXISTS ONLY public."Attendance" DROP CONSTRAINT IF EXISTS "Attendance_exam_id_fkey";
DROP TRIGGER IF EXISTS set_updated_at ON public."Users";
DROP TRIGGER IF EXISTS set_updated_at ON public."StudentExamAssignments";
DROP TRIGGER IF EXISTS set_updated_at ON public."QuestionPool";
DROP TRIGGER IF EXISTS set_updated_at ON public."PendingRegistrations";
DROP TRIGGER IF EXISTS set_updated_at ON public."ParentNotifications";
DROP TRIGGER IF EXISTS set_updated_at ON public."ParentContacts";
DROP TRIGGER IF EXISTS set_updated_at ON public."OtpTokens";
DROP TRIGGER IF EXISTS set_updated_at ON public."Notifications";
DROP TRIGGER IF EXISTS set_updated_at ON public."Exams";
DROP TRIGGER IF EXISTS set_updated_at ON public."ExamSessions";
DROP TRIGGER IF EXISTS set_updated_at ON public."Classes";
DROP TRIGGER IF EXISTS set_updated_at ON public."Attendance";
DROP INDEX IF EXISTS public.idx_users_department_id;
DROP INDEX IF EXISTS public.idx_syllabus_files_uploaded_by;
DROP INDEX IF EXISTS public.idx_syllabus_files_created_at;
DROP INDEX IF EXISTS public.idx_student_exam_assignments_student_id;
DROP INDEX IF EXISTS public.idx_student_exam_assignments_exam_id;
DROP INDEX IF EXISTS public.idx_question_pool_exam_id;
DROP INDEX IF EXISTS public.idx_parent_contacts_student_id;
DROP INDEX IF EXISTS public.idx_otp_tokens_user_id;
DROP INDEX IF EXISTS public.idx_notifications_user_id;
DROP INDEX IF EXISTS public.idx_exams_teacher_id;
DROP INDEX IF EXISTS public.idx_exams_status;
DROP INDEX IF EXISTS public.idx_exam_sessions_student_id;
DROP INDEX IF EXISTS public.idx_exam_sessions_status;
DROP INDEX IF EXISTS public.idx_exam_sessions_exam_id;
DROP INDEX IF EXISTS public.idx_attendance_student_id;
DROP INDEX IF EXISTS public.idx_attendance_exam_id;
ALTER TABLE IF EXISTS ONLY public."_Migrations" DROP CONSTRAINT IF EXISTS "_Migrations_pkey";
ALTER TABLE IF EXISTS ONLY public."Users" DROP CONSTRAINT IF EXISTS "Users_pkey";
ALTER TABLE IF EXISTS ONLY public."Users" DROP CONSTRAINT IF EXISTS "Users_phone_key";
ALTER TABLE IF EXISTS ONLY public."Users" DROP CONSTRAINT IF EXISTS "Users_email_key";
ALTER TABLE IF EXISTS ONLY public."SyllabusFiles" DROP CONSTRAINT IF EXISTS "SyllabusFiles_pkey";
ALTER TABLE IF EXISTS ONLY public."StudentExamAssignments" DROP CONSTRAINT IF EXISTS "StudentExamAssignments_student_id_exam_id_key";
ALTER TABLE IF EXISTS ONLY public."StudentExamAssignments" DROP CONSTRAINT IF EXISTS "StudentExamAssignments_pkey";
ALTER TABLE IF EXISTS ONLY public."QuestionPool" DROP CONSTRAINT IF EXISTS "QuestionPool_pkey";
ALTER TABLE IF EXISTS ONLY public."PendingRegistrations" DROP CONSTRAINT IF EXISTS "PendingRegistrations_pkey";
ALTER TABLE IF EXISTS ONLY public."ParentNotifications" DROP CONSTRAINT IF EXISTS "ParentNotifications_pkey";
ALTER TABLE IF EXISTS ONLY public."ParentContacts" DROP CONSTRAINT IF EXISTS "ParentContacts_pkey";
ALTER TABLE IF EXISTS ONLY public."OtpTokens" DROP CONSTRAINT IF EXISTS "OtpTokens_pkey";
ALTER TABLE IF EXISTS ONLY public."Notifications" DROP CONSTRAINT IF EXISTS "Notifications_pkey";
ALTER TABLE IF EXISTS ONLY public."Exams" DROP CONSTRAINT IF EXISTS "Exams_pkey";
ALTER TABLE IF EXISTS ONLY public."Exams" DROP CONSTRAINT IF EXISTS "Exams_deep_link_code_key";
ALTER TABLE IF EXISTS ONLY public."ExamSessions" DROP CONSTRAINT IF EXISTS "ExamSessions_pkey";
ALTER TABLE IF EXISTS ONLY public."Departments" DROP CONSTRAINT IF EXISTS "Departments_pkey";
ALTER TABLE IF EXISTS ONLY public."Departments" DROP CONSTRAINT IF EXISTS "Departments_name_key";
ALTER TABLE IF EXISTS ONLY public."Classes" DROP CONSTRAINT IF EXISTS "Classes_pkey";
ALTER TABLE IF EXISTS ONLY public."ClassStudents" DROP CONSTRAINT IF EXISTS "ClassStudents_pkey";
ALTER TABLE IF EXISTS ONLY public."Attendance" DROP CONSTRAINT IF EXISTS "Attendance_pkey";
ALTER TABLE IF EXISTS ONLY auth.users DROP CONSTRAINT IF EXISTS users_pkey;
ALTER TABLE IF EXISTS public."Users" ALTER COLUMN id DROP DEFAULT;
ALTER TABLE IF EXISTS public."SyllabusFiles" ALTER COLUMN id DROP DEFAULT;
ALTER TABLE IF EXISTS public."StudentExamAssignments" ALTER COLUMN id DROP DEFAULT;
ALTER TABLE IF EXISTS public."QuestionPool" ALTER COLUMN id DROP DEFAULT;
ALTER TABLE IF EXISTS public."PendingRegistrations" ALTER COLUMN id DROP DEFAULT;
ALTER TABLE IF EXISTS public."ParentNotifications" ALTER COLUMN id DROP DEFAULT;
ALTER TABLE IF EXISTS public."ParentContacts" ALTER COLUMN id DROP DEFAULT;
ALTER TABLE IF EXISTS public."OtpTokens" ALTER COLUMN id DROP DEFAULT;
ALTER TABLE IF EXISTS public."Notifications" ALTER COLUMN id DROP DEFAULT;
ALTER TABLE IF EXISTS public."Exams" ALTER COLUMN id DROP DEFAULT;
ALTER TABLE IF EXISTS public."ExamSessions" ALTER COLUMN id DROP DEFAULT;
ALTER TABLE IF EXISTS public."Departments" ALTER COLUMN id DROP DEFAULT;
ALTER TABLE IF EXISTS public."Classes" ALTER COLUMN id DROP DEFAULT;
ALTER TABLE IF EXISTS public."Attendance" ALTER COLUMN id DROP DEFAULT;
DROP TABLE IF EXISTS public."_Migrations";
DROP SEQUENCE IF EXISTS public."Users_id_seq";
DROP TABLE IF EXISTS public."Users";
DROP SEQUENCE IF EXISTS public."SyllabusFiles_id_seq";
DROP TABLE IF EXISTS public."SyllabusFiles";
DROP SEQUENCE IF EXISTS public."StudentExamAssignments_id_seq";
DROP TABLE IF EXISTS public."StudentExamAssignments";
DROP SEQUENCE IF EXISTS public."QuestionPool_id_seq";
DROP TABLE IF EXISTS public."QuestionPool";
DROP SEQUENCE IF EXISTS public."PendingRegistrations_id_seq";
DROP TABLE IF EXISTS public."PendingRegistrations";
DROP SEQUENCE IF EXISTS public."ParentNotifications_id_seq";
DROP TABLE IF EXISTS public."ParentNotifications";
DROP SEQUENCE IF EXISTS public."ParentContacts_id_seq";
DROP TABLE IF EXISTS public."ParentContacts";
DROP SEQUENCE IF EXISTS public."OtpTokens_id_seq";
DROP TABLE IF EXISTS public."OtpTokens";
DROP SEQUENCE IF EXISTS public."Notifications_id_seq";
DROP TABLE IF EXISTS public."Notifications";
DROP SEQUENCE IF EXISTS public."Exams_id_seq";
DROP TABLE IF EXISTS public."Exams";
DROP SEQUENCE IF EXISTS public."ExamSessions_id_seq";
DROP TABLE IF EXISTS public."ExamSessions";
DROP SEQUENCE IF EXISTS public."Departments_id_seq";
DROP TABLE IF EXISTS public."Departments";
DROP SEQUENCE IF EXISTS public."Classes_id_seq";
DROP TABLE IF EXISTS public."Classes";
DROP TABLE IF EXISTS public."ClassStudents";
DROP SEQUENCE IF EXISTS public."Attendance_id_seq";
DROP TABLE IF EXISTS public."Attendance";
DROP TABLE IF EXISTS auth.users;
DROP FUNCTION IF EXISTS public.update_updated_at_column();
DROP FUNCTION IF EXISTS public.generate_deep_link_code();
DROP EXTENSION IF EXISTS "uuid-ossp";
DROP EXTENSION IF EXISTS pgcrypto;
-- *not* dropping schema, since initdb creates it
DROP SCHEMA IF EXISTS auth;
--
-- Name: auth; Type: SCHEMA; Schema: -; Owner: postgres
--

CREATE SCHEMA auth;


ALTER SCHEMA auth OWNER TO postgres;

--
-- Name: public; Type: SCHEMA; Schema: -; Owner: postgres
--

-- *not* creating schema, since initdb creates it


ALTER SCHEMA public OWNER TO postgres;

--
-- Name: SCHEMA public; Type: COMMENT; Schema: -; Owner: postgres
--

COMMENT ON SCHEMA public IS '';


--
-- Name: pgcrypto; Type: EXTENSION; Schema: -; Owner: -
--

CREATE EXTENSION IF NOT EXISTS pgcrypto WITH SCHEMA public;


--
-- Name: EXTENSION pgcrypto; Type: COMMENT; Schema: -; Owner: 
--

COMMENT ON EXTENSION pgcrypto IS 'cryptographic functions';


--
-- Name: uuid-ossp; Type: EXTENSION; Schema: -; Owner: -
--

CREATE EXTENSION IF NOT EXISTS "uuid-ossp" WITH SCHEMA public;


--
-- Name: EXTENSION "uuid-ossp"; Type: COMMENT; Schema: -; Owner: 
--

COMMENT ON EXTENSION "uuid-ossp" IS 'generate universally unique identifiers (UUIDs)';


--
-- Name: generate_deep_link_code(); Type: FUNCTION; Schema: public; Owner: postgres
--

CREATE FUNCTION public.generate_deep_link_code() RETURNS character varying
    LANGUAGE plpgsql
    AS $$
DECLARE
    code VARCHAR(50);
BEGIN
    code := upper(substr(md5(random()::text || clock_timestamp()::text), 1, 8));
    WHILE EXISTS (SELECT 1 FROM "Exams" WHERE "deep_link_code" = code) LOOP
        code := upper(substr(md5(random()::text || clock_timestamp()::text), 1, 8));
    END LOOP;
    RETURN code;
END;
$$;


ALTER FUNCTION public.generate_deep_link_code() OWNER TO postgres;

--
-- Name: update_updated_at_column(); Type: FUNCTION; Schema: public; Owner: postgres
--

CREATE FUNCTION public.update_updated_at_column() RETURNS trigger
    LANGUAGE plpgsql
    AS $$
BEGIN
    NEW."updated_at" = NOW();
    RETURN NEW;
END;
$$;


ALTER FUNCTION public.update_updated_at_column() OWNER TO postgres;

SET default_tablespace = '';

SET default_table_access_method = heap;

--
-- Name: users; Type: TABLE; Schema: auth; Owner: postgres
--

CREATE TABLE auth.users (
    id uuid DEFAULT gen_random_uuid() NOT NULL
);


ALTER TABLE auth.users OWNER TO postgres;

--
-- Name: Attendance; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public."Attendance" (
    id integer NOT NULL,
    student_id integer NOT NULL,
    exam_id integer NOT NULL,
    status character varying(20) DEFAULT 'absent'::character varying,
    marked_at timestamp with time zone,
    marked_by character varying(20) DEFAULT 'auto'::character varying,
    created_at timestamp with time zone DEFAULT now() NOT NULL,
    updated_at timestamp with time zone DEFAULT now() NOT NULL
);


ALTER TABLE public."Attendance" OWNER TO postgres;

--
-- Name: Attendance_id_seq; Type: SEQUENCE; Schema: public; Owner: postgres
--

CREATE SEQUENCE public."Attendance_id_seq"
    AS integer
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


ALTER SEQUENCE public."Attendance_id_seq" OWNER TO postgres;

--
-- Name: Attendance_id_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: postgres
--

ALTER SEQUENCE public."Attendance_id_seq" OWNED BY public."Attendance".id;


--
-- Name: ClassStudents; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public."ClassStudents" (
    class_id integer NOT NULL,
    student_id integer NOT NULL
);


ALTER TABLE public."ClassStudents" OWNER TO postgres;

--
-- Name: Classes; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public."Classes" (
    id integer NOT NULL,
    teacher_id integer NOT NULL,
    name character varying(255) NOT NULL,
    subject character varying(100),
    description text,
    created_at timestamp with time zone DEFAULT now() NOT NULL,
    updated_at timestamp with time zone DEFAULT now() NOT NULL
);


ALTER TABLE public."Classes" OWNER TO postgres;

--
-- Name: Classes_id_seq; Type: SEQUENCE; Schema: public; Owner: postgres
--

CREATE SEQUENCE public."Classes_id_seq"
    AS integer
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


ALTER SEQUENCE public."Classes_id_seq" OWNER TO postgres;

--
-- Name: Classes_id_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: postgres
--

ALTER SEQUENCE public."Classes_id_seq" OWNED BY public."Classes".id;


--
-- Name: Departments; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public."Departments" (
    id integer NOT NULL,
    name character varying(255) NOT NULL,
    description text,
    head_id integer,
    created_at timestamp with time zone DEFAULT now(),
    updated_at timestamp with time zone DEFAULT now()
);


ALTER TABLE public."Departments" OWNER TO postgres;

--
-- Name: Departments_id_seq; Type: SEQUENCE; Schema: public; Owner: postgres
--

CREATE SEQUENCE public."Departments_id_seq"
    AS integer
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


ALTER SEQUENCE public."Departments_id_seq" OWNER TO postgres;

--
-- Name: Departments_id_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: postgres
--

ALTER SEQUENCE public."Departments_id_seq" OWNED BY public."Departments".id;


--
-- Name: ExamSessions; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public."ExamSessions" (
    id integer NOT NULL,
    student_id integer NOT NULL,
    exam_id integer NOT NULL,
    score numeric(5,2) DEFAULT 0,
    total_questions integer NOT NULL,
    answered_count integer DEFAULT 0,
    status character varying(20) DEFAULT 'not_started'::character varying,
    disqualified_reason text,
    started_at timestamp with time zone,
    submitted_at timestamp with time zone,
    time_remaining_seconds integer,
    ip_address character varying(45),
    user_agent text,
    mode character varying(20) DEFAULT 'exam'::character varying,
    answers jsonb,
    created_at timestamp with time zone DEFAULT now() NOT NULL,
    updated_at timestamp with time zone DEFAULT now() NOT NULL
);


ALTER TABLE public."ExamSessions" OWNER TO postgres;

--
-- Name: ExamSessions_id_seq; Type: SEQUENCE; Schema: public; Owner: postgres
--

CREATE SEQUENCE public."ExamSessions_id_seq"
    AS integer
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


ALTER SEQUENCE public."ExamSessions_id_seq" OWNER TO postgres;

--
-- Name: ExamSessions_id_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: postgres
--

ALTER SEQUENCE public."ExamSessions_id_seq" OWNED BY public."ExamSessions".id;


--
-- Name: Exams; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public."Exams" (
    id integer NOT NULL,
    teacher_id integer NOT NULL,
    title character varying(255) NOT NULL,
    subject character varying(100) NOT NULL,
    syllabus_text text,
    syllabus_pdf_path character varying(500),
    duration_minutes integer DEFAULT 30,
    total_questions integer DEFAULT 0,
    deep_link_code character varying(50) NOT NULL,
    status character varying(20) DEFAULT 'draft'::character varying,
    scheduled_at timestamp with time zone,
    scheduled_end_at timestamp with time zone,
    allow_reattempt boolean DEFAULT false,
    created_at timestamp with time zone DEFAULT now() NOT NULL,
    updated_at timestamp with time zone DEFAULT now() NOT NULL
);


ALTER TABLE public."Exams" OWNER TO postgres;

--
-- Name: Exams_id_seq; Type: SEQUENCE; Schema: public; Owner: postgres
--

CREATE SEQUENCE public."Exams_id_seq"
    AS integer
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


ALTER SEQUENCE public."Exams_id_seq" OWNER TO postgres;

--
-- Name: Exams_id_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: postgres
--

ALTER SEQUENCE public."Exams_id_seq" OWNED BY public."Exams".id;


--
-- Name: Notifications; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public."Notifications" (
    id integer NOT NULL,
    user_id integer NOT NULL,
    title character varying(255) NOT NULL,
    message text NOT NULL,
    type character varying(30) DEFAULT 'general'::character varying,
    is_read boolean DEFAULT false,
    metadata jsonb,
    created_at timestamp with time zone DEFAULT now() NOT NULL,
    updated_at timestamp with time zone DEFAULT now() NOT NULL
);


ALTER TABLE public."Notifications" OWNER TO postgres;

--
-- Name: Notifications_id_seq; Type: SEQUENCE; Schema: public; Owner: postgres
--

CREATE SEQUENCE public."Notifications_id_seq"
    AS integer
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


ALTER SEQUENCE public."Notifications_id_seq" OWNER TO postgres;

--
-- Name: Notifications_id_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: postgres
--

ALTER SEQUENCE public."Notifications_id_seq" OWNED BY public."Notifications".id;


--
-- Name: OtpTokens; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public."OtpTokens" (
    id integer NOT NULL,
    user_id integer NOT NULL,
    otp_code character varying(6) NOT NULL,
    expires_at timestamp with time zone NOT NULL,
    is_used boolean DEFAULT false,
    created_at timestamp with time zone DEFAULT now() NOT NULL,
    updated_at timestamp with time zone DEFAULT now() NOT NULL
);


ALTER TABLE public."OtpTokens" OWNER TO postgres;

--
-- Name: OtpTokens_id_seq; Type: SEQUENCE; Schema: public; Owner: postgres
--

CREATE SEQUENCE public."OtpTokens_id_seq"
    AS integer
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


ALTER SEQUENCE public."OtpTokens_id_seq" OWNER TO postgres;

--
-- Name: OtpTokens_id_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: postgres
--

ALTER SEQUENCE public."OtpTokens_id_seq" OWNED BY public."OtpTokens".id;


--
-- Name: ParentContacts; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public."ParentContacts" (
    id integer NOT NULL,
    student_id integer NOT NULL,
    parent_name character varying(255) NOT NULL,
    parent_phone character varying(20),
    parent_email character varying(255),
    relationship character varying(50),
    created_at timestamp with time zone DEFAULT now() NOT NULL,
    updated_at timestamp with time zone DEFAULT now() NOT NULL
);


ALTER TABLE public."ParentContacts" OWNER TO postgres;

--
-- Name: ParentContacts_id_seq; Type: SEQUENCE; Schema: public; Owner: postgres
--

CREATE SEQUENCE public."ParentContacts_id_seq"
    AS integer
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


ALTER SEQUENCE public."ParentContacts_id_seq" OWNER TO postgres;

--
-- Name: ParentContacts_id_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: postgres
--

ALTER SEQUENCE public."ParentContacts_id_seq" OWNED BY public."ParentContacts".id;


--
-- Name: ParentNotifications; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public."ParentNotifications" (
    id integer NOT NULL,
    parent_contact_id integer,
    student_id integer NOT NULL,
    exam_id integer NOT NULL,
    message_text text NOT NULL,
    sent_via character varying(20),
    sent_at timestamp with time zone,
    delivery_status character varying(20) DEFAULT 'pending'::character varying,
    provider_response text,
    created_at timestamp with time zone DEFAULT now() NOT NULL,
    updated_at timestamp with time zone DEFAULT now() NOT NULL
);


ALTER TABLE public."ParentNotifications" OWNER TO postgres;

--
-- Name: ParentNotifications_id_seq; Type: SEQUENCE; Schema: public; Owner: postgres
--

CREATE SEQUENCE public."ParentNotifications_id_seq"
    AS integer
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


ALTER SEQUENCE public."ParentNotifications_id_seq" OWNER TO postgres;

--
-- Name: ParentNotifications_id_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: postgres
--

ALTER SEQUENCE public."ParentNotifications_id_seq" OWNED BY public."ParentNotifications".id;


--
-- Name: PendingRegistrations; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public."PendingRegistrations" (
    id integer NOT NULL,
    auth_uid uuid,
    name character varying(255) NOT NULL,
    identifier character varying(255) NOT NULL,
    password_hash character varying(255) NOT NULL,
    role character varying(20) NOT NULL,
    otp_code character varying(6) NOT NULL,
    expires_at timestamp with time zone NOT NULL,
    is_used boolean DEFAULT false,
    created_at timestamp with time zone DEFAULT now() NOT NULL,
    updated_at timestamp with time zone DEFAULT now() NOT NULL,
    phone character varying(20),
    email character varying(255)
);


ALTER TABLE public."PendingRegistrations" OWNER TO postgres;

--
-- Name: PendingRegistrations_id_seq; Type: SEQUENCE; Schema: public; Owner: postgres
--

CREATE SEQUENCE public."PendingRegistrations_id_seq"
    AS integer
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


ALTER SEQUENCE public."PendingRegistrations_id_seq" OWNER TO postgres;

--
-- Name: PendingRegistrations_id_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: postgres
--

ALTER SEQUENCE public."PendingRegistrations_id_seq" OWNED BY public."PendingRegistrations".id;


--
-- Name: QuestionPool; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public."QuestionPool" (
    id integer NOT NULL,
    exam_id integer NOT NULL,
    student_id integer,
    question_text text NOT NULL,
    option_a text NOT NULL,
    option_b text NOT NULL,
    option_c text,
    option_d text,
    correct_answer character(1) NOT NULL,
    difficulty character varying(20) DEFAULT 'medium'::character varying,
    points integer DEFAULT 1,
    status character varying(20) DEFAULT 'published'::character varying,
    created_at timestamp with time zone DEFAULT now() NOT NULL,
    updated_at timestamp with time zone DEFAULT now() NOT NULL,
    CONSTRAINT "QuestionPool_correct_answer_check" CHECK ((correct_answer = ANY (ARRAY['A'::bpchar, 'B'::bpchar, 'C'::bpchar, 'D'::bpchar])))
);


ALTER TABLE public."QuestionPool" OWNER TO postgres;

--
-- Name: QuestionPool_id_seq; Type: SEQUENCE; Schema: public; Owner: postgres
--

CREATE SEQUENCE public."QuestionPool_id_seq"
    AS integer
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


ALTER SEQUENCE public."QuestionPool_id_seq" OWNER TO postgres;

--
-- Name: QuestionPool_id_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: postgres
--

ALTER SEQUENCE public."QuestionPool_id_seq" OWNED BY public."QuestionPool".id;


--
-- Name: StudentExamAssignments; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public."StudentExamAssignments" (
    id integer NOT NULL,
    student_id integer NOT NULL,
    exam_id integer NOT NULL,
    question_ids jsonb DEFAULT '[]'::jsonb NOT NULL,
    created_at timestamp with time zone DEFAULT now() NOT NULL,
    updated_at timestamp with time zone DEFAULT now() NOT NULL
);


ALTER TABLE public."StudentExamAssignments" OWNER TO postgres;

--
-- Name: StudentExamAssignments_id_seq; Type: SEQUENCE; Schema: public; Owner: postgres
--

CREATE SEQUENCE public."StudentExamAssignments_id_seq"
    AS integer
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


ALTER SEQUENCE public."StudentExamAssignments_id_seq" OWNER TO postgres;

--
-- Name: StudentExamAssignments_id_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: postgres
--

ALTER SEQUENCE public."StudentExamAssignments_id_seq" OWNED BY public."StudentExamAssignments".id;


--
-- Name: SyllabusFiles; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public."SyllabusFiles" (
    id integer NOT NULL,
    title character varying(255) NOT NULL,
    description text,
    file_name character varying(255) NOT NULL,
    file_path character varying(500) NOT NULL,
    content_type character varying(100) NOT NULL,
    file_size bigint NOT NULL,
    uploaded_by integer,
    created_at timestamp with time zone DEFAULT now(),
    updated_at timestamp with time zone DEFAULT now()
);


ALTER TABLE public."SyllabusFiles" OWNER TO postgres;

--
-- Name: SyllabusFiles_id_seq; Type: SEQUENCE; Schema: public; Owner: postgres
--

CREATE SEQUENCE public."SyllabusFiles_id_seq"
    AS integer
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


ALTER SEQUENCE public."SyllabusFiles_id_seq" OWNER TO postgres;

--
-- Name: SyllabusFiles_id_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: postgres
--

ALTER SEQUENCE public."SyllabusFiles_id_seq" OWNED BY public."SyllabusFiles".id;


--
-- Name: Users; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public."Users" (
    id integer NOT NULL,
    name character varying(255) NOT NULL,
    role character varying(20) DEFAULT 'student'::character varying NOT NULL,
    phone character varying(20),
    email character varying(255),
    password_hash character varying(255) DEFAULT ''::character varying NOT NULL,
    token_version integer DEFAULT 0,
    created_at timestamp with time zone DEFAULT now() NOT NULL,
    updated_at timestamp with time zone DEFAULT now() NOT NULL,
    department_id integer
);


ALTER TABLE public."Users" OWNER TO postgres;

--
-- Name: Users_id_seq; Type: SEQUENCE; Schema: public; Owner: postgres
--

CREATE SEQUENCE public."Users_id_seq"
    AS integer
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


ALTER SEQUENCE public."Users_id_seq" OWNER TO postgres;

--
-- Name: Users_id_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: postgres
--

ALTER SEQUENCE public."Users_id_seq" OWNED BY public."Users".id;


--
-- Name: _Migrations; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public."_Migrations" (
    migration character varying(255) NOT NULL,
    applied_at timestamp with time zone DEFAULT now()
);


ALTER TABLE public."_Migrations" OWNER TO postgres;

--
-- Name: Attendance id; Type: DEFAULT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."Attendance" ALTER COLUMN id SET DEFAULT nextval('public."Attendance_id_seq"'::regclass);


--
-- Name: Classes id; Type: DEFAULT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."Classes" ALTER COLUMN id SET DEFAULT nextval('public."Classes_id_seq"'::regclass);


--
-- Name: Departments id; Type: DEFAULT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."Departments" ALTER COLUMN id SET DEFAULT nextval('public."Departments_id_seq"'::regclass);


--
-- Name: ExamSessions id; Type: DEFAULT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."ExamSessions" ALTER COLUMN id SET DEFAULT nextval('public."ExamSessions_id_seq"'::regclass);


--
-- Name: Exams id; Type: DEFAULT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."Exams" ALTER COLUMN id SET DEFAULT nextval('public."Exams_id_seq"'::regclass);


--
-- Name: Notifications id; Type: DEFAULT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."Notifications" ALTER COLUMN id SET DEFAULT nextval('public."Notifications_id_seq"'::regclass);


--
-- Name: OtpTokens id; Type: DEFAULT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."OtpTokens" ALTER COLUMN id SET DEFAULT nextval('public."OtpTokens_id_seq"'::regclass);


--
-- Name: ParentContacts id; Type: DEFAULT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."ParentContacts" ALTER COLUMN id SET DEFAULT nextval('public."ParentContacts_id_seq"'::regclass);


--
-- Name: ParentNotifications id; Type: DEFAULT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."ParentNotifications" ALTER COLUMN id SET DEFAULT nextval('public."ParentNotifications_id_seq"'::regclass);


--
-- Name: PendingRegistrations id; Type: DEFAULT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."PendingRegistrations" ALTER COLUMN id SET DEFAULT nextval('public."PendingRegistrations_id_seq"'::regclass);


--
-- Name: QuestionPool id; Type: DEFAULT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."QuestionPool" ALTER COLUMN id SET DEFAULT nextval('public."QuestionPool_id_seq"'::regclass);


--
-- Name: StudentExamAssignments id; Type: DEFAULT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."StudentExamAssignments" ALTER COLUMN id SET DEFAULT nextval('public."StudentExamAssignments_id_seq"'::regclass);


--
-- Name: SyllabusFiles id; Type: DEFAULT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."SyllabusFiles" ALTER COLUMN id SET DEFAULT nextval('public."SyllabusFiles_id_seq"'::regclass);


--
-- Name: Users id; Type: DEFAULT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."Users" ALTER COLUMN id SET DEFAULT nextval('public."Users_id_seq"'::regclass);


--
-- Data for Name: users; Type: TABLE DATA; Schema: auth; Owner: postgres
--

COPY auth.users (id) FROM stdin;
\.


--
-- Data for Name: Attendance; Type: TABLE DATA; Schema: public; Owner: postgres
--

COPY public."Attendance" (id, student_id, exam_id, status, marked_at, marked_by, created_at, updated_at) FROM stdin;
\.


--
-- Data for Name: ClassStudents; Type: TABLE DATA; Schema: public; Owner: postgres
--

COPY public."ClassStudents" (class_id, student_id) FROM stdin;
\.


--
-- Data for Name: Classes; Type: TABLE DATA; Schema: public; Owner: postgres
--

COPY public."Classes" (id, teacher_id, name, subject, description, created_at, updated_at) FROM stdin;
\.


--
-- Data for Name: Departments; Type: TABLE DATA; Schema: public; Owner: postgres
--

COPY public."Departments" (id, name, description, head_id, created_at, updated_at) FROM stdin;
\.


--
-- Data for Name: ExamSessions; Type: TABLE DATA; Schema: public; Owner: postgres
--

COPY public."ExamSessions" (id, student_id, exam_id, score, total_questions, answered_count, status, disqualified_reason, started_at, submitted_at, time_remaining_seconds, ip_address, user_agent, mode, answers, created_at, updated_at) FROM stdin;
\.


--
-- Data for Name: Exams; Type: TABLE DATA; Schema: public; Owner: postgres
--

COPY public."Exams" (id, teacher_id, title, subject, syllabus_text, syllabus_pdf_path, duration_minutes, total_questions, deep_link_code, status, scheduled_at, scheduled_end_at, allow_reattempt, created_at, updated_at) FROM stdin;
\.


--
-- Data for Name: Notifications; Type: TABLE DATA; Schema: public; Owner: postgres
--

COPY public."Notifications" (id, user_id, title, message, type, is_read, metadata, created_at, updated_at) FROM stdin;
2	5	Test All	Testing notification to everyone	announcement	f	\N	2026-07-06 21:01:04.688676+05:30	2026-07-06 21:01:04.70572+05:30
3	6	Test All	Testing notification to everyone	announcement	f	\N	2026-07-06 21:01:04.688676+05:30	2026-07-06 21:01:04.707841+05:30
5	5	tesst	cukvtitjuryvu	announcement	f	\N	2026-07-06 21:14:04.303817+05:30	2026-07-06 21:14:04.321803+05:30
6	6	tesst	cukvtitjuryvu	announcement	f	\N	2026-07-06 21:14:04.303817+05:30	2026-07-06 21:14:04.32655+05:30
1	4	Test All	Testing notification to everyone	announcement	t	\N	2026-07-06 21:01:04.688676+05:30	2026-07-06 21:15:06.677773+05:30
4	4	tesst	cukvtitjuryvu	announcement	t	\N	2026-07-06 21:14:04.303817+05:30	2026-07-06 21:15:07.102272+05:30
\.


--
-- Data for Name: OtpTokens; Type: TABLE DATA; Schema: public; Owner: postgres
--

COPY public."OtpTokens" (id, user_id, otp_code, expires_at, is_used, created_at, updated_at) FROM stdin;
4	6	759649	2026-07-06 21:05:53.005531+05:30	t	2026-07-06 21:00:53.005536+05:30	2026-07-06 21:01:04.323977+05:30
\.


--
-- Data for Name: ParentContacts; Type: TABLE DATA; Schema: public; Owner: postgres
--

COPY public."ParentContacts" (id, student_id, parent_name, parent_phone, parent_email, relationship, created_at, updated_at) FROM stdin;
\.


--
-- Data for Name: ParentNotifications; Type: TABLE DATA; Schema: public; Owner: postgres
--

COPY public."ParentNotifications" (id, parent_contact_id, student_id, exam_id, message_text, sent_via, sent_at, delivery_status, provider_response, created_at, updated_at) FROM stdin;
\.


--
-- Data for Name: PendingRegistrations; Type: TABLE DATA; Schema: public; Owner: postgres
--

COPY public."PendingRegistrations" (id, auth_uid, name, identifier, password_hash, role, otp_code, expires_at, is_used, created_at, updated_at, phone, email) FROM stdin;
1	\N	Test Student	newstudent@test.com	$2a$11$UxCNoX2OsR6PaY1XuGAezuARBSzNHjQU43B1Rky07SmjMMOoDsmOS	student	943531	2026-06-25 14:14:52.025511+05:30	f	2026-06-25 14:09:52.045516+05:30	2026-06-25 14:09:52.05736+05:30	\N	\N
2	\N	Aksht	kashhmm614@gmail.com	$2a$11$syzt0sMtkyp5QNpjpkzqe.1J4Orh/j8iOvsutIKyGpHmvcvY3EbsS	teacher	428774	2026-06-25 14:15:53.791105+05:30	f	2026-06-25 14:10:53.792719+05:30	2026-06-25 14:10:53.793147+05:30	\N	\N
\.


--
-- Data for Name: QuestionPool; Type: TABLE DATA; Schema: public; Owner: postgres
--

COPY public."QuestionPool" (id, exam_id, student_id, question_text, option_a, option_b, option_c, option_d, correct_answer, difficulty, points, status, created_at, updated_at) FROM stdin;
\.


--
-- Data for Name: StudentExamAssignments; Type: TABLE DATA; Schema: public; Owner: postgres
--

COPY public."StudentExamAssignments" (id, student_id, exam_id, question_ids, created_at, updated_at) FROM stdin;
\.


--
-- Data for Name: SyllabusFiles; Type: TABLE DATA; Schema: public; Owner: postgres
--

COPY public."SyllabusFiles" (id, title, description, file_name, file_path, content_type, file_size, uploaded_by, created_at, updated_at) FROM stdin;
1	ProjectMatrixx-v12.4-Complete-Root-Guide	\N	ProjectMatrixx-v12.4-Complete-Root-Guide.pdf	/uploads/syllabus/1aeae20c7fb4420ca1b051927c73ea2a_ProjectMatrixx-v12.4-Complete-Root-Guide.pdf	application/pdf	373483	5	2026-07-06 19:38:09.651652+05:30	2026-07-06 19:38:09.651652+05:30
\.


--
-- Data for Name: Users; Type: TABLE DATA; Schema: public; Owner: postgres
--

COPY public."Users" (id, name, role, phone, email, password_hash, token_version, created_at, updated_at, department_id) FROM stdin;
4	Akshit Kumar	student	\N	anaak7887@gmail.com	$2a$11$e9WBcOYrV8syWatFBLiJjeBReZ3EmY1O9bFWMJVsgaRI7J8l0IkKK	0	2026-07-06 13:12:00.144233+05:30	2026-07-06 13:12:00.144233+05:30	\N
5	Kash Hmm	teacher	\N	kashhmm614@gmail.com	$2a$11$WvPEcoZY39xXxmPX8UZ3xenI8uDyOCARQqzMozheuj1tKHZZEhdOK	0	2026-07-06 19:37:48.734807+05:30	2026-07-06 19:37:48.734807+05:30	\N
6	a	teacher	\N	a@a.com	$2a$11$TG.nyH17szEKAODTWZyNguN8/SVZT7BoycrdMW174iQ6R9LNM/hTS	0	2026-07-06 21:00:52.702361+05:30	2026-07-06 21:00:52.094431+05:30	\N
7	Lucky K	student	\N	lk2614507@gmail.com	$2a$11$YqmILUfsINSTovLamd4VIOCktBOcNRAO0HwfieCtKyhJ2ycMUV7d2	0	2026-07-06 21:14:22.947108+05:30	2026-07-06 21:14:22.947108+05:30	\N
\.


--
-- Data for Name: _Migrations; Type: TABLE DATA; Schema: public; Owner: postgres
--

COPY public."_Migrations" (migration, applied_at) FROM stdin;
004_create_syllabus_files	2026-07-06 13:05:23.555782+05:30
005_syllabus_files_nullable_uploaded_by	2026-07-06 13:05:23.59838+05:30
006_cleanup_supabase_artifacts	2026-07-06 19:20:22.881834+05:30
007_create_departments	2026-07-06 20:02:05.884719+05:30
\.


--
-- Name: Attendance_id_seq; Type: SEQUENCE SET; Schema: public; Owner: postgres
--

SELECT pg_catalog.setval('public."Attendance_id_seq"', 1, false);


--
-- Name: Classes_id_seq; Type: SEQUENCE SET; Schema: public; Owner: postgres
--

SELECT pg_catalog.setval('public."Classes_id_seq"', 1, false);


--
-- Name: Departments_id_seq; Type: SEQUENCE SET; Schema: public; Owner: postgres
--

SELECT pg_catalog.setval('public."Departments_id_seq"', 1, false);


--
-- Name: ExamSessions_id_seq; Type: SEQUENCE SET; Schema: public; Owner: postgres
--

SELECT pg_catalog.setval('public."ExamSessions_id_seq"', 1, false);


--
-- Name: Exams_id_seq; Type: SEQUENCE SET; Schema: public; Owner: postgres
--

SELECT pg_catalog.setval('public."Exams_id_seq"', 1, false);


--
-- Name: Notifications_id_seq; Type: SEQUENCE SET; Schema: public; Owner: postgres
--

SELECT pg_catalog.setval('public."Notifications_id_seq"', 6, true);


--
-- Name: OtpTokens_id_seq; Type: SEQUENCE SET; Schema: public; Owner: postgres
--

SELECT pg_catalog.setval('public."OtpTokens_id_seq"', 4, true);


--
-- Name: ParentContacts_id_seq; Type: SEQUENCE SET; Schema: public; Owner: postgres
--

SELECT pg_catalog.setval('public."ParentContacts_id_seq"', 1, false);


--
-- Name: ParentNotifications_id_seq; Type: SEQUENCE SET; Schema: public; Owner: postgres
--

SELECT pg_catalog.setval('public."ParentNotifications_id_seq"', 1, false);


--
-- Name: PendingRegistrations_id_seq; Type: SEQUENCE SET; Schema: public; Owner: postgres
--

SELECT pg_catalog.setval('public."PendingRegistrations_id_seq"', 2, true);


--
-- Name: QuestionPool_id_seq; Type: SEQUENCE SET; Schema: public; Owner: postgres
--

SELECT pg_catalog.setval('public."QuestionPool_id_seq"', 1, false);


--
-- Name: StudentExamAssignments_id_seq; Type: SEQUENCE SET; Schema: public; Owner: postgres
--

SELECT pg_catalog.setval('public."StudentExamAssignments_id_seq"', 1, false);


--
-- Name: SyllabusFiles_id_seq; Type: SEQUENCE SET; Schema: public; Owner: postgres
--

SELECT pg_catalog.setval('public."SyllabusFiles_id_seq"', 1, true);


--
-- Name: Users_id_seq; Type: SEQUENCE SET; Schema: public; Owner: postgres
--

SELECT pg_catalog.setval('public."Users_id_seq"', 7, true);


--
-- Name: users users_pkey; Type: CONSTRAINT; Schema: auth; Owner: postgres
--

ALTER TABLE ONLY auth.users
    ADD CONSTRAINT users_pkey PRIMARY KEY (id);


--
-- Name: Attendance Attendance_pkey; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."Attendance"
    ADD CONSTRAINT "Attendance_pkey" PRIMARY KEY (id);


--
-- Name: ClassStudents ClassStudents_pkey; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."ClassStudents"
    ADD CONSTRAINT "ClassStudents_pkey" PRIMARY KEY (class_id, student_id);


--
-- Name: Classes Classes_pkey; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."Classes"
    ADD CONSTRAINT "Classes_pkey" PRIMARY KEY (id);


--
-- Name: Departments Departments_name_key; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."Departments"
    ADD CONSTRAINT "Departments_name_key" UNIQUE (name);


--
-- Name: Departments Departments_pkey; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."Departments"
    ADD CONSTRAINT "Departments_pkey" PRIMARY KEY (id);


--
-- Name: ExamSessions ExamSessions_pkey; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."ExamSessions"
    ADD CONSTRAINT "ExamSessions_pkey" PRIMARY KEY (id);


--
-- Name: Exams Exams_deep_link_code_key; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."Exams"
    ADD CONSTRAINT "Exams_deep_link_code_key" UNIQUE (deep_link_code);


--
-- Name: Exams Exams_pkey; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."Exams"
    ADD CONSTRAINT "Exams_pkey" PRIMARY KEY (id);


--
-- Name: Notifications Notifications_pkey; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."Notifications"
    ADD CONSTRAINT "Notifications_pkey" PRIMARY KEY (id);


--
-- Name: OtpTokens OtpTokens_pkey; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."OtpTokens"
    ADD CONSTRAINT "OtpTokens_pkey" PRIMARY KEY (id);


--
-- Name: ParentContacts ParentContacts_pkey; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."ParentContacts"
    ADD CONSTRAINT "ParentContacts_pkey" PRIMARY KEY (id);


--
-- Name: ParentNotifications ParentNotifications_pkey; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."ParentNotifications"
    ADD CONSTRAINT "ParentNotifications_pkey" PRIMARY KEY (id);


--
-- Name: PendingRegistrations PendingRegistrations_pkey; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."PendingRegistrations"
    ADD CONSTRAINT "PendingRegistrations_pkey" PRIMARY KEY (id);


--
-- Name: QuestionPool QuestionPool_pkey; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."QuestionPool"
    ADD CONSTRAINT "QuestionPool_pkey" PRIMARY KEY (id);


--
-- Name: StudentExamAssignments StudentExamAssignments_pkey; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."StudentExamAssignments"
    ADD CONSTRAINT "StudentExamAssignments_pkey" PRIMARY KEY (id);


--
-- Name: StudentExamAssignments StudentExamAssignments_student_id_exam_id_key; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."StudentExamAssignments"
    ADD CONSTRAINT "StudentExamAssignments_student_id_exam_id_key" UNIQUE (student_id, exam_id);


--
-- Name: SyllabusFiles SyllabusFiles_pkey; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."SyllabusFiles"
    ADD CONSTRAINT "SyllabusFiles_pkey" PRIMARY KEY (id);


--
-- Name: Users Users_email_key; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."Users"
    ADD CONSTRAINT "Users_email_key" UNIQUE (email);


--
-- Name: Users Users_phone_key; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."Users"
    ADD CONSTRAINT "Users_phone_key" UNIQUE (phone);


--
-- Name: Users Users_pkey; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."Users"
    ADD CONSTRAINT "Users_pkey" PRIMARY KEY (id);


--
-- Name: _Migrations _Migrations_pkey; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."_Migrations"
    ADD CONSTRAINT "_Migrations_pkey" PRIMARY KEY (migration);


--
-- Name: idx_attendance_exam_id; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX idx_attendance_exam_id ON public."Attendance" USING btree (exam_id);


--
-- Name: idx_attendance_student_id; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX idx_attendance_student_id ON public."Attendance" USING btree (student_id);


--
-- Name: idx_exam_sessions_exam_id; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX idx_exam_sessions_exam_id ON public."ExamSessions" USING btree (exam_id);


--
-- Name: idx_exam_sessions_status; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX idx_exam_sessions_status ON public."ExamSessions" USING btree (status);


--
-- Name: idx_exam_sessions_student_id; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX idx_exam_sessions_student_id ON public."ExamSessions" USING btree (student_id);


--
-- Name: idx_exams_status; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX idx_exams_status ON public."Exams" USING btree (status);


--
-- Name: idx_exams_teacher_id; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX idx_exams_teacher_id ON public."Exams" USING btree (teacher_id);


--
-- Name: idx_notifications_user_id; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX idx_notifications_user_id ON public."Notifications" USING btree (user_id);


--
-- Name: idx_otp_tokens_user_id; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX idx_otp_tokens_user_id ON public."OtpTokens" USING btree (user_id);


--
-- Name: idx_parent_contacts_student_id; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX idx_parent_contacts_student_id ON public."ParentContacts" USING btree (student_id);


--
-- Name: idx_question_pool_exam_id; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX idx_question_pool_exam_id ON public."QuestionPool" USING btree (exam_id);


--
-- Name: idx_student_exam_assignments_exam_id; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX idx_student_exam_assignments_exam_id ON public."StudentExamAssignments" USING btree (exam_id);


--
-- Name: idx_student_exam_assignments_student_id; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX idx_student_exam_assignments_student_id ON public."StudentExamAssignments" USING btree (student_id);


--
-- Name: idx_syllabus_files_created_at; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX idx_syllabus_files_created_at ON public."SyllabusFiles" USING btree (created_at DESC);


--
-- Name: idx_syllabus_files_uploaded_by; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX idx_syllabus_files_uploaded_by ON public."SyllabusFiles" USING btree (uploaded_by);


--
-- Name: idx_users_department_id; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX idx_users_department_id ON public."Users" USING btree (department_id);


--
-- Name: Attendance set_updated_at; Type: TRIGGER; Schema: public; Owner: postgres
--

CREATE TRIGGER set_updated_at BEFORE UPDATE ON public."Attendance" FOR EACH ROW EXECUTE FUNCTION public.update_updated_at_column();


--
-- Name: Classes set_updated_at; Type: TRIGGER; Schema: public; Owner: postgres
--

CREATE TRIGGER set_updated_at BEFORE UPDATE ON public."Classes" FOR EACH ROW EXECUTE FUNCTION public.update_updated_at_column();


--
-- Name: ExamSessions set_updated_at; Type: TRIGGER; Schema: public; Owner: postgres
--

CREATE TRIGGER set_updated_at BEFORE UPDATE ON public."ExamSessions" FOR EACH ROW EXECUTE FUNCTION public.update_updated_at_column();


--
-- Name: Exams set_updated_at; Type: TRIGGER; Schema: public; Owner: postgres
--

CREATE TRIGGER set_updated_at BEFORE UPDATE ON public."Exams" FOR EACH ROW EXECUTE FUNCTION public.update_updated_at_column();


--
-- Name: Notifications set_updated_at; Type: TRIGGER; Schema: public; Owner: postgres
--

CREATE TRIGGER set_updated_at BEFORE UPDATE ON public."Notifications" FOR EACH ROW EXECUTE FUNCTION public.update_updated_at_column();


--
-- Name: OtpTokens set_updated_at; Type: TRIGGER; Schema: public; Owner: postgres
--

CREATE TRIGGER set_updated_at BEFORE UPDATE ON public."OtpTokens" FOR EACH ROW EXECUTE FUNCTION public.update_updated_at_column();


--
-- Name: ParentContacts set_updated_at; Type: TRIGGER; Schema: public; Owner: postgres
--

CREATE TRIGGER set_updated_at BEFORE UPDATE ON public."ParentContacts" FOR EACH ROW EXECUTE FUNCTION public.update_updated_at_column();


--
-- Name: ParentNotifications set_updated_at; Type: TRIGGER; Schema: public; Owner: postgres
--

CREATE TRIGGER set_updated_at BEFORE UPDATE ON public."ParentNotifications" FOR EACH ROW EXECUTE FUNCTION public.update_updated_at_column();


--
-- Name: PendingRegistrations set_updated_at; Type: TRIGGER; Schema: public; Owner: postgres
--

CREATE TRIGGER set_updated_at BEFORE UPDATE ON public."PendingRegistrations" FOR EACH ROW EXECUTE FUNCTION public.update_updated_at_column();


--
-- Name: QuestionPool set_updated_at; Type: TRIGGER; Schema: public; Owner: postgres
--

CREATE TRIGGER set_updated_at BEFORE UPDATE ON public."QuestionPool" FOR EACH ROW EXECUTE FUNCTION public.update_updated_at_column();


--
-- Name: StudentExamAssignments set_updated_at; Type: TRIGGER; Schema: public; Owner: postgres
--

CREATE TRIGGER set_updated_at BEFORE UPDATE ON public."StudentExamAssignments" FOR EACH ROW EXECUTE FUNCTION public.update_updated_at_column();


--
-- Name: Users set_updated_at; Type: TRIGGER; Schema: public; Owner: postgres
--

CREATE TRIGGER set_updated_at BEFORE UPDATE ON public."Users" FOR EACH ROW EXECUTE FUNCTION public.update_updated_at_column();


--
-- Name: Attendance Attendance_exam_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."Attendance"
    ADD CONSTRAINT "Attendance_exam_id_fkey" FOREIGN KEY (exam_id) REFERENCES public."Exams"(id) ON DELETE CASCADE;


--
-- Name: Attendance Attendance_student_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."Attendance"
    ADD CONSTRAINT "Attendance_student_id_fkey" FOREIGN KEY (student_id) REFERENCES public."Users"(id) ON DELETE CASCADE;


--
-- Name: ClassStudents ClassStudents_class_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."ClassStudents"
    ADD CONSTRAINT "ClassStudents_class_id_fkey" FOREIGN KEY (class_id) REFERENCES public."Classes"(id) ON DELETE CASCADE;


--
-- Name: ClassStudents ClassStudents_student_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."ClassStudents"
    ADD CONSTRAINT "ClassStudents_student_id_fkey" FOREIGN KEY (student_id) REFERENCES public."Users"(id) ON DELETE CASCADE;


--
-- Name: Classes Classes_teacher_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."Classes"
    ADD CONSTRAINT "Classes_teacher_id_fkey" FOREIGN KEY (teacher_id) REFERENCES public."Users"(id) ON DELETE CASCADE;


--
-- Name: Departments Departments_head_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."Departments"
    ADD CONSTRAINT "Departments_head_id_fkey" FOREIGN KEY (head_id) REFERENCES public."Users"(id) ON DELETE SET NULL;


--
-- Name: ExamSessions ExamSessions_exam_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."ExamSessions"
    ADD CONSTRAINT "ExamSessions_exam_id_fkey" FOREIGN KEY (exam_id) REFERENCES public."Exams"(id) ON DELETE CASCADE;


--
-- Name: ExamSessions ExamSessions_student_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."ExamSessions"
    ADD CONSTRAINT "ExamSessions_student_id_fkey" FOREIGN KEY (student_id) REFERENCES public."Users"(id) ON DELETE CASCADE;


--
-- Name: Exams Exams_teacher_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."Exams"
    ADD CONSTRAINT "Exams_teacher_id_fkey" FOREIGN KEY (teacher_id) REFERENCES public."Users"(id) ON DELETE CASCADE;


--
-- Name: Notifications Notifications_user_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."Notifications"
    ADD CONSTRAINT "Notifications_user_id_fkey" FOREIGN KEY (user_id) REFERENCES public."Users"(id) ON DELETE CASCADE;


--
-- Name: OtpTokens OtpTokens_user_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."OtpTokens"
    ADD CONSTRAINT "OtpTokens_user_id_fkey" FOREIGN KEY (user_id) REFERENCES public."Users"(id) ON DELETE CASCADE;


--
-- Name: ParentContacts ParentContacts_student_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."ParentContacts"
    ADD CONSTRAINT "ParentContacts_student_id_fkey" FOREIGN KEY (student_id) REFERENCES public."Users"(id) ON DELETE CASCADE;


--
-- Name: ParentNotifications ParentNotifications_exam_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."ParentNotifications"
    ADD CONSTRAINT "ParentNotifications_exam_id_fkey" FOREIGN KEY (exam_id) REFERENCES public."Exams"(id) ON DELETE CASCADE;


--
-- Name: ParentNotifications ParentNotifications_parent_contact_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."ParentNotifications"
    ADD CONSTRAINT "ParentNotifications_parent_contact_id_fkey" FOREIGN KEY (parent_contact_id) REFERENCES public."ParentContacts"(id) ON DELETE CASCADE;


--
-- Name: ParentNotifications ParentNotifications_student_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."ParentNotifications"
    ADD CONSTRAINT "ParentNotifications_student_id_fkey" FOREIGN KEY (student_id) REFERENCES public."Users"(id) ON DELETE CASCADE;


--
-- Name: PendingRegistrations PendingRegistrations_auth_uid_fkey; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."PendingRegistrations"
    ADD CONSTRAINT "PendingRegistrations_auth_uid_fkey" FOREIGN KEY (auth_uid) REFERENCES auth.users(id) ON DELETE CASCADE;


--
-- Name: QuestionPool QuestionPool_exam_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."QuestionPool"
    ADD CONSTRAINT "QuestionPool_exam_id_fkey" FOREIGN KEY (exam_id) REFERENCES public."Exams"(id) ON DELETE CASCADE;


--
-- Name: QuestionPool QuestionPool_student_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."QuestionPool"
    ADD CONSTRAINT "QuestionPool_student_id_fkey" FOREIGN KEY (student_id) REFERENCES public."Users"(id) ON DELETE SET NULL;


--
-- Name: StudentExamAssignments StudentExamAssignments_exam_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."StudentExamAssignments"
    ADD CONSTRAINT "StudentExamAssignments_exam_id_fkey" FOREIGN KEY (exam_id) REFERENCES public."Exams"(id) ON DELETE CASCADE;


--
-- Name: StudentExamAssignments StudentExamAssignments_student_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."StudentExamAssignments"
    ADD CONSTRAINT "StudentExamAssignments_student_id_fkey" FOREIGN KEY (student_id) REFERENCES public."Users"(id) ON DELETE CASCADE;


--
-- Name: SyllabusFiles SyllabusFiles_uploaded_by_fkey; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."SyllabusFiles"
    ADD CONSTRAINT "SyllabusFiles_uploaded_by_fkey" FOREIGN KEY (uploaded_by) REFERENCES public."Users"(id) ON DELETE SET NULL;


--
-- Name: Users Users_department_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."Users"
    ADD CONSTRAINT "Users_department_id_fkey" FOREIGN KEY (department_id) REFERENCES public."Departments"(id) ON DELETE SET NULL;


--
-- Name: SCHEMA public; Type: ACL; Schema: -; Owner: postgres
--

REVOKE USAGE ON SCHEMA public FROM PUBLIC;


--
-- PostgreSQL database dump complete
--

\unrestrict tOhikSeLV6kkdunrgvPyige0FotwmtbhZX2hGr5cmAi0kZcszUyhVdJWhS26zje

