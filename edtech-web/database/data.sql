--
-- PostgreSQL database dump
--

\restrict 6aLgkd9qPPnavOTmRcnn41bQiNJaBV52emEegsngLN5KNCrWfVp02zAnQcrCC0W

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

--
-- Data for Name: users; Type: TABLE DATA; Schema: auth; Owner: postgres
--



--
-- Data for Name: Users; Type: TABLE DATA; Schema: public; Owner: postgres
--

INSERT INTO public."Users" (id, name, role, phone, email, password_hash, token_version, created_at, updated_at, department_id) VALUES (4, 'Akshit Kumar', 'student', NULL, 'anaak7887@gmail.com', '$2a$11$e9WBcOYrV8syWatFBLiJjeBReZ3EmY1O9bFWMJVsgaRI7J8l0IkKK', 0, '2026-07-06 13:12:00.144233+05:30', '2026-07-06 13:12:00.144233+05:30', NULL);
INSERT INTO public."Users" (id, name, role, phone, email, password_hash, token_version, created_at, updated_at, department_id) VALUES (5, 'Kash Hmm', 'teacher', NULL, 'kashhmm614@gmail.com', '$2a$11$WvPEcoZY39xXxmPX8UZ3xenI8uDyOCARQqzMozheuj1tKHZZEhdOK', 0, '2026-07-06 19:37:48.734807+05:30', '2026-07-06 19:37:48.734807+05:30', NULL);
INSERT INTO public."Users" (id, name, role, phone, email, password_hash, token_version, created_at, updated_at, department_id) VALUES (6, 'a', 'teacher', NULL, 'a@a.com', '$2a$11$TG.nyH17szEKAODTWZyNguN8/SVZT7BoycrdMW174iQ6R9LNM/hTS', 0, '2026-07-06 21:00:52.702361+05:30', '2026-07-06 21:00:52.094431+05:30', NULL);
INSERT INTO public."Users" (id, name, role, phone, email, password_hash, token_version, created_at, updated_at, department_id) VALUES (7, 'Lucky K', 'student', NULL, 'lk2614507@gmail.com', '$2a$11$YqmILUfsINSTovLamd4VIOCktBOcNRAO0HwfieCtKyhJ2ycMUV7d2', 0, '2026-07-06 21:14:22.947108+05:30', '2026-07-06 21:14:22.947108+05:30', NULL);


--
-- Data for Name: Exams; Type: TABLE DATA; Schema: public; Owner: postgres
--



--
-- Data for Name: Attendance; Type: TABLE DATA; Schema: public; Owner: postgres
--



--
-- Data for Name: Classes; Type: TABLE DATA; Schema: public; Owner: postgres
--



--
-- Data for Name: ClassStudents; Type: TABLE DATA; Schema: public; Owner: postgres
--



--
-- Data for Name: Departments; Type: TABLE DATA; Schema: public; Owner: postgres
--



--
-- Data for Name: ExamSessions; Type: TABLE DATA; Schema: public; Owner: postgres
--



--
-- Data for Name: Notifications; Type: TABLE DATA; Schema: public; Owner: postgres
--

INSERT INTO public."Notifications" (id, user_id, title, message, type, is_read, metadata, created_at, updated_at) VALUES (2, 5, 'Test All', 'Testing notification to everyone', 'announcement', false, NULL, '2026-07-06 21:01:04.688676+05:30', '2026-07-06 21:01:04.70572+05:30');
INSERT INTO public."Notifications" (id, user_id, title, message, type, is_read, metadata, created_at, updated_at) VALUES (3, 6, 'Test All', 'Testing notification to everyone', 'announcement', false, NULL, '2026-07-06 21:01:04.688676+05:30', '2026-07-06 21:01:04.707841+05:30');
INSERT INTO public."Notifications" (id, user_id, title, message, type, is_read, metadata, created_at, updated_at) VALUES (5, 5, 'tesst', 'cukvtitjuryvu', 'announcement', false, NULL, '2026-07-06 21:14:04.303817+05:30', '2026-07-06 21:14:04.321803+05:30');
INSERT INTO public."Notifications" (id, user_id, title, message, type, is_read, metadata, created_at, updated_at) VALUES (6, 6, 'tesst', 'cukvtitjuryvu', 'announcement', false, NULL, '2026-07-06 21:14:04.303817+05:30', '2026-07-06 21:14:04.32655+05:30');
INSERT INTO public."Notifications" (id, user_id, title, message, type, is_read, metadata, created_at, updated_at) VALUES (1, 4, 'Test All', 'Testing notification to everyone', 'announcement', true, NULL, '2026-07-06 21:01:04.688676+05:30', '2026-07-06 21:15:06.677773+05:30');
INSERT INTO public."Notifications" (id, user_id, title, message, type, is_read, metadata, created_at, updated_at) VALUES (4, 4, 'tesst', 'cukvtitjuryvu', 'announcement', true, NULL, '2026-07-06 21:14:04.303817+05:30', '2026-07-06 21:15:07.102272+05:30');


--
-- Data for Name: OtpTokens; Type: TABLE DATA; Schema: public; Owner: postgres
--

INSERT INTO public."OtpTokens" (id, user_id, otp_code, expires_at, is_used, created_at, updated_at) VALUES (4, 6, '759649', '2026-07-06 21:05:53.005531+05:30', true, '2026-07-06 21:00:53.005536+05:30', '2026-07-06 21:01:04.323977+05:30');


--
-- Data for Name: ParentContacts; Type: TABLE DATA; Schema: public; Owner: postgres
--



--
-- Data for Name: ParentNotifications; Type: TABLE DATA; Schema: public; Owner: postgres
--



--
-- Data for Name: PendingRegistrations; Type: TABLE DATA; Schema: public; Owner: postgres
--

INSERT INTO public."PendingRegistrations" (id, auth_uid, name, identifier, password_hash, role, otp_code, expires_at, is_used, created_at, updated_at, phone, email) VALUES (1, NULL, 'Test Student', 'newstudent@test.com', '$2a$11$UxCNoX2OsR6PaY1XuGAezuARBSzNHjQU43B1Rky07SmjMMOoDsmOS', 'student', '943531', '2026-06-25 14:14:52.025511+05:30', false, '2026-06-25 14:09:52.045516+05:30', '2026-06-25 14:09:52.05736+05:30', NULL, NULL);
INSERT INTO public."PendingRegistrations" (id, auth_uid, name, identifier, password_hash, role, otp_code, expires_at, is_used, created_at, updated_at, phone, email) VALUES (2, NULL, 'Aksht', 'kashhmm614@gmail.com', '$2a$11$syzt0sMtkyp5QNpjpkzqe.1J4Orh/j8iOvsutIKyGpHmvcvY3EbsS', 'teacher', '428774', '2026-06-25 14:15:53.791105+05:30', false, '2026-06-25 14:10:53.792719+05:30', '2026-06-25 14:10:53.793147+05:30', NULL, NULL);


--
-- Data for Name: QuestionPool; Type: TABLE DATA; Schema: public; Owner: postgres
--



--
-- Data for Name: StudentExamAssignments; Type: TABLE DATA; Schema: public; Owner: postgres
--



--
-- Data for Name: SyllabusFiles; Type: TABLE DATA; Schema: public; Owner: postgres
--

INSERT INTO public."SyllabusFiles" (id, title, description, file_name, file_path, content_type, file_size, uploaded_by, created_at, updated_at) VALUES (1, 'ProjectMatrixx-v12.4-Complete-Root-Guide', NULL, 'ProjectMatrixx-v12.4-Complete-Root-Guide.pdf', '/uploads/syllabus/1aeae20c7fb4420ca1b051927c73ea2a_ProjectMatrixx-v12.4-Complete-Root-Guide.pdf', 'application/pdf', 373483, 5, '2026-07-06 19:38:09.651652+05:30', '2026-07-06 19:38:09.651652+05:30');


--
-- Data for Name: _Migrations; Type: TABLE DATA; Schema: public; Owner: postgres
--

INSERT INTO public."_Migrations" (migration, applied_at) VALUES ('004_create_syllabus_files', '2026-07-06 13:05:23.555782+05:30');
INSERT INTO public."_Migrations" (migration, applied_at) VALUES ('005_syllabus_files_nullable_uploaded_by', '2026-07-06 13:05:23.59838+05:30');
INSERT INTO public."_Migrations" (migration, applied_at) VALUES ('006_cleanup_supabase_artifacts', '2026-07-06 19:20:22.881834+05:30');
INSERT INTO public."_Migrations" (migration, applied_at) VALUES ('007_create_departments', '2026-07-06 20:02:05.884719+05:30');


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
-- PostgreSQL database dump complete
--

\unrestrict 6aLgkd9qPPnavOTmRcnn41bQiNJaBV52emEegsngLN5KNCrWfVp02zAnQcrCC0W

