# EdTech Platform — System Design High-Level Document (HLD)

## Executive Summary

EdTech is an AI-powered examination platform that enables teachers to create, schedule, and manage exams while students take timed assessments with instant auto-grading. The system supports role-based access (Student / Teacher / Admin), AI-driven question generation (Google Gemini), OTP + JWT authentication with Google OAuth, live teacher dashboards via SignalR, parent report delivery, class & department management, syllabus file distribution, and comprehensive exam analytics. The frontend is a dependency-free vanilla JS static site; the backend is ASP.NET Core 10 with Dapper on Neon PostgreSQL.

---

## 1. System Architecture Overview

```
┌─────────────────────────────────────────────────────────────────┐
│                     CLIENT LAYER                                 │
│  (Static Vanilla JS — HTML/CSS/JS, no build step, Vercel)       │
│  ├── Auth Pages        (OTP login, register, admin login,       │
│  │                      forgot/reset password, Google OAuth)    │
│  ├── Teacher Portal    (13 pages: exams, questions, students,   │
│  │                      classes, attendance, statistics,        │
│  │                      reports, parent contacts, syllabus,     │
│  │                      scheduling, announcements)              │
│  ├── Student Portal    (9 pages: dashboard, exam screen,        │
│  │                      review, practice, classes, syllabus,    │
│  │                      notifications)                          │
│  └── Admin Portal      (stats, users, exams, classes,           │
│                         departments, teacher approvals,         │
│                         DB monitor)                             │
└─────────────────────┬───────────────────────────────────────────┘
                      │ HTTPS (REST + SignalR WebSocket)
                      ▼
┌─────────────────────────────────────────────────────────────────┐
│              API GATEWAY / REST LAYER                            │
│  (ASP.NET Core 10, C#, Railway — Dockerfile)                    │
│  ├── /api/auth/*          Auth, OTP, registration, Google       │
│  ├── /api/exams/*         Exam CRUD, lifecycle, deep links      │
│  ├── /api/questions/*     Question generation, sessions,        │
│  │                         auto-grading, disqualification       │
│  ├── /api/teacher/*       Dashboard, students, classes,         │
│  │                         question bank, scheduling,           │
│  │                         parent contacts, reports             │
│  ├── /api/students/*      Analytics, review, practice,          │
│  │                         notifications                        │
│  ├── /api/admin/*         Stats, users, teacher approvals,      │
│  │                         departments, DB monitoring           │
│  ├── /api/reports/*       Parent report generation & delivery   │
│  ├── /api/syllabus/*      Syllabus file upload/distribution     │
│  ├── /api/departments/*   Department CRUD & assignment          │
│  └── /hubs/*              SignalR: dashboard, exam,             │
│                            notification                         │
└─────────────────────┬───────────────────────────────────────────┘
                      │
        ┌─────────────┼──────────────┐
        ▼             ▼              ▼
┌──────────────┐ ┌──────────────┐ ┌──────────────┐
│  SERVICES    │ │  EXTERNAL    │ │  DATABASE    │
│  (see 2.3)   │ │  APIs        │ │  LAYER       │
│              │ │  Gemini ·    │ │  Neon PG16   │
│              │ │  SendGrid ·  │ │  + read      │
│              │ │  Google OAuth│ │  replica     │
└──────────────┘ └──────────────┘ └──────────────┘
```

---

## 2. Component Architecture

### 2.1 Frontend Layer
**Technology:** HTML5, CSS3, Vanilla JavaScript — no framework, no build step, no npm
**Deployment:** Vercel (static hosting + SPA rewrites via `vercel.json`), PWA via `sw.js` + `manifest.json`

**Components:**
- **Authentication Module** — OTP login (`generate-otp` / `verify-otp`), OTP registration (`send-register-otp` / `verify-register-otp`), admin password login, password reset, Google OAuth redirect/callback, JWT in localStorage with `refresh-token`
- **Teacher Portal** (13 pages) — dashboard, create-exam, edit-exam (incl. scheduling, deep links, publish, delete), questions (AI-generate + manual + bulk import), students, student-detail, classes, attendance, statistics (live per-exam, disqualify), reports (parent email delivery + history), parent-contacts, syllabus, profile
- **Student Portal** (9 pages) — dashboard, exam screen (timed, auto-save, auto-submit), results, review, practice mode, classes, notifications, syllabus, profile
- **Admin Portal** (8 pages) — dashboard, users, exams, classes, departments, teacher approvals, db-monitor
- **Shared JS modules** — `api.js` (ApiClient, snake_case, error envelope), `auth.js` (requireRole, setupNavbar, session handling), role-specific helpers (`teacher.js`, `admin.js`, `nav.js`)

### 2.2 API Layer (ASP.NET Core 10)

**Core Components:**

#### Authentication & Authorization (`AuthController`, `GoogleAuthController`)
- `POST /api/auth/generate-otp`, `POST /api/auth/verify-otp` — OTP login flow
- `POST /api/auth/send-register-otp`, `POST /api/auth/verify-register-otp` — OTP registration (teacher accounts created with `approval_status = 'pending'`)
- `POST /api/auth/admin-login` — password login for admins
- `POST /api/auth/forgot-password`, `POST /api/auth/reset-password`
- `POST /api/auth/refresh-token`, `PUT /api/auth/profile`, `POST /api/auth/change-password`, `DELETE /api/auth/profile`, `POST /api/auth/external-session`
- `GET /auth/google/login`, `GET /auth/google/callback`, `POST /auth/google/signin`, `GET /auth/google/config`

**Auth mechanism:** custom JWT middleware (`[RequireAuth]`) + role enforcement (`[RequireRole]`). JWT carries `userId`, `role`, `tokenVersion`; token version is checked against the DB on every request so password changes invalidate all existing sessions. Passwords hashed with BCrypt. OTPs expire after 5 minutes, are single-use, rate-limited (5 req/min), and never returned in API responses in production.

#### Exam Management (`ExamController`)
- `GET /api/exams` — paginated, role-filtered list (teachers own, students assigned)
- `GET /api/exams/:id`, `POST /api/exams`, `PUT /api/exams/:id` (validated status transitions: draft → scheduled → active → ended), `DELETE /api/exams/:id`
- `POST /api/exams/ai-create` — full exam generated by Gemini
- `POST /api/exams/:id/activate`, `POST /api/exams/:id/publish-questions`, `POST /api/exams/:id/bulk-import`
- `GET /api/exams/:id/statistics`, `GET /api/exams/:id/attendance`, `GET /api/exams/:id/export-pdf`
- `GET /api/exams/:id/deep-link`, `GET /api/exams/deep-link/:code` — deep-link resolution (`edtech-exam://exam/{code}`)

#### Question & Session Management (`QuestionController`)
- `POST /api/questions/generate` — Gemini AI generation; `POST /api/questions/generate-personalized`
- `POST /api/questions/assign` — per-student question assignment
- `POST /api/questions/create-session`, `POST /api/questions/submit` — session lifecycle with auto-grading (MCQ exact/partial matching), draft persistence
- `GET /api/questions/session/:studentId/:examId` — resume state
- `POST /api/questions/disqualify/:sessionId` — teacher disqualification with mandatory reason (min 3 chars)
- `GET /api/questions/statistics/:examId`, `GET /api/questions/my-results/:studentId`

#### Teacher Dashboard (`TeacherController`)
- Students: `GET/POST /api/teacher/students`, `GET /api/teacher/students/search-by-sid`, `GET /api/teacher/students/:id` (profile + classes + exam history), `DELETE /api/teacher/students/:id`
- Question bank CRUD: `GET/POST /api/teacher/questions/:examId`, `PUT/DELETE /api/teacher/questions/:questionId`
- Classes: `POST/GET /api/teacher/classes`, `GET /api/teacher/classes/:id`, `POST /api/teacher/classes/:id/students`, `DELETE /api/teacher/classes/:id/students/:studentId`, `DELETE /api/teacher/classes/:id`
- `POST /api/teacher/announcement`, `PUT /api/teacher/schedule/:examId` (set or clear `scheduled_at`)
- Parent contacts: `GET /api/teacher/parent-contacts`, `POST /api/teacher/parent-contacts/:studentId` (persists `relationship`), `DELETE /api/teacher/parent-contacts/:studentId`
- `GET /api/teacher/report-history/:examId`

#### Student Portal (`StudentController`)
- `GET /api/students/analytics/:studentId`, `GET /api/students/review/:sessionId`
- `POST /api/students/practice/start`, `POST /api/students/practice/submit`
- `GET /api/students/notifications`, `PUT /api/students/notifications/:id/read`, `PUT /api/students/notifications/read-all`, `GET /api/students/classes`

#### Reports (`ReportsController`)
- `POST /api/reports/send/:examId`, `GET /api/reports/pending/:examId`, `POST /api/reports/test-email`, `POST /api/reports/test-sms`

#### Admin (`AdminController`)
- `GET /api/admin/stats`, `GET /api/admin/users`, `GET /api/admin/exams`, `GET /api/admin/classes` (+ detail/delete)
- `GET /api/admin/pending-teachers`, `POST /api/admin/teachers/:id/approve`, `POST /api/admin/teachers/:id/reject` — teacher approval workflow
- `GET /api/admin/db-snapshot`, `POST /api/admin/backfill-student-ids`, `DELETE /api/admin/users/:id`

#### Syllabus & Departments
- `GET /api/syllabus` (+ `/:id`, `/my`, `POST /upload`, `GET /:id/download`, `PATCH /:id`, `DELETE /:id`) — syllabus file distribution scoped to classes
- `GET/POST/PUT/DELETE /api/departments` + `POST /api/departments/assign`, `POST /api/departments/remove-user/:userId`, `GET /api/departments/:id/users`

#### Realtime (SignalR)
- `/hubs/dashboard` — teacher dashboard live updates (exam status changes)
- `/hubs/exam` — exam-group events (status, grading)
- `/hubs/notification` — per-user notification delivery

### 2.3 Service Layer

| Service | Responsibility |
|---------|----------------|
| **AuthService** | Registration, OTP login, password reset, Google external session, token-version invalidation |
| **JwtService** | JWT issuance/validation with `userId`, `role`, `tokenVersion` claims |
| **OtpService** | OTP generation, 5-min TTL, single-use, rate limiting |
| **EmailService** | SendGrid/SMTP delivery (OTPs, parent reports, announcements) |
| **GeminiService** | AI question/exam generation with prompt templates + circuit breaker |
| **GoogleAuthService** | Google OAuth 2.0 sign-in and token exchange |
| **ExamService** | Exam CRUD, status transitions, scheduling, attendance, statistics, PDF export, deep links |
| **QuestionService** | Question bank, AI generation, sessions, auto-grading, disqualification |
| **TeacherService** | Students, classes, question bank, parent contacts, announcements, scheduling |
| **StudentService** | Analytics, review, practice mode, notifications |
| **ReportsService** | Parent report generation and delivery |
| **SyllabusService** | Syllabus file upload/download scoped by class |
| **DepartmentService** | Department CRUD and user assignment |
| **HubService** | SignalR push fan-out to dashboard/exam/notification hubs |
| **RedisCacheService** | Optional Redis cache (falls back to no-op when Redis is unavailable) |
| **CircuitBreakerService** | Guards external dependencies (Gemini, Redis, SendGrid) |
| **MigrationService** | Idempotent auto-migrations at startup (`_Migrations` ledger) |
| **DbConnectionFactory** | Primary + optional read-replica `IDbConnection` factory (Npgsql) |

### 2.4 Database Layer
**Technology:** PostgreSQL 16 (Neon), Dapper + Npgsql, automatic migrations on startup

**Connection strategy:** primary connection from `NEON_CONNECTION_STRING` (use `-pooler` hostname for Neon's pooled endpoint, `SSL Mode=Require` in production); optional `NeonReplica` connection string routes heavy reads to a read replica.

**Core tables (all snake_case, migration-managed):**

```sql
users
├── id (INTEGER PK)                    -- all FKs are integer, not UUID
├── name, email, phone
├── password_hash (BCrypt)
├── role (student | teacher | admin)
├── student_id (VARCHAR(10) UNIQUE)    -- institution ID, backfillable by admin
├── token_version (INT)                -- session invalidation
├── department_id (FK departments, ON DELETE SET NULL)
├── approval_status (approved | pending | rejected)   -- teacher approval flow
├── rejection_reason, approved_at
└── created_at, updated_at

exams
├── id (INTEGER PK)
├── teacher_id (FK users)
├── title, subject
├── syllabus_text, syllabus_pdf_path
├── duration_minutes, total_questions
├── status (draft | scheduled | active | ended)
├── scheduled_at, scheduled_end_at     -- set/cleared via PUT /api/teacher/schedule/:id
├── allow_reattempt (BOOL)
├── deep_link_code (UNIQUE)
└── created_at, updated_at

question_pool
├── id (INTEGER PK)
├── exam_id (FK exams)
├── question_text, question_type
├── option_a .. option_d, correct_answer (never sent to students)
├── explanation, marks/difficulty
└── status (draft | published)

exam_sessions
├── id (INTEGER PK)
├── student_id (FK users), exam_id (FK exams)
├── score (DECIMAL), total_questions, answered_count
├── status (in_progress | submitted | disqualified)
├── disqualified_reason
├── answers (JSONB: [{question_id, answer}])
├── started_at, submitted_at, time_remaining_seconds
├── ip_address, user_agent
├── mode (exam | practice)
└── created_at, updated_at

student_exam_assignments   -- per-student question assignment
attendance                 -- exam attendance records
notifications              -- in-app notifications (type, content, is_read)
classes / class_students   -- teacher-owned classes (name, section, subject, academic_year)
parent_contacts            -- parent_name, parent_email, parent_phone, relationship
parent_notifications       -- report delivery log (report_type, sent_at, content JSONB)
otp_tokens                 -- 5-min TTL, single-use OTPs
pending_registrations      -- registration OTP staging
syllabus_files             -- uploaded_by, class_id, file_data BYTEA, file_path
departments                -- name, description; users.department_id FK
_migrations                -- migration ledger
```

---

## 3. Data Flow & Use Cases

### 3.1 Teacher Registration & Approval Flow
```
Teacher             API (AuthService)          Admin Portal          Database
   │                      │                        │                    │
   ├─ send-register-otp ─>│                        │                    │
   ├─ verify-register-otp>│  role=teacher          │                    │
   │                      │  approval_status='pending' ───────────────>│
   │<─ registered ────────│                        │                    │
   │                      │                        ├─ pending-teachers ─>│
   │                      │                        │<── pending list ────│
   │                      │                        ├─ approve/:id ─────>│
   │                      │                        │               approval_status='approved'
   │<─ can log in now ────│                        │                    │
```

### 3.2 Exam Creation & Scheduling Flow
```
Teacher                 API                      Database
   │                        │                        │
   ├─ POST /api/exams ────>│  INSERT exam (draft) ──>│
   ├─ POST /api/questions/generate (Gemini)          │
   │                        ├─ call Gemini ──────────┼─ (external)
   │                        ├─ validate & save ────>│  INSERT question_pool
   ├─ PUT /api/teacher/schedule/:id                 │
   │                        ├─ set scheduled_at ───>│  UPDATE exams
   ├─ POST /api/exams/:id/activate                  │
   │                        ├─ status: draft→active>│
   ├─ POST /api/questions/assign                    │
   │                        ├─ per-student assignment ─> INSERT student_exam_assignments
   │                        └─ SignalR hub → student dashboard
```

### 3.3 Student Exam Taking Flow
```
Student                 API                    Database        SignalR
   │                        │                        │            │
   ├─ Login (OTP) ────────>│  generate/verify OTP    │            │
   │<─ JWT token ──────────│  JwtService             │            │
   │                        │                        │            │
   ├─ Load Exam ──────────>│                        │            │
   │                        ├─ Fetch assigned questions ─>        │
   │<─ Questions ──────────│                        │            │
   │                        │                        │            │
   ├─ create-session ─────>│  INSERT exam_sessions  │            │
   ├─ Submit (auto-save) ─>│  UPDATE answers JSONB  │            │
   │                        │                        │            │
   ├─ Final Submit ───────>│  AutoGrade (score,     │            │
   │                        │   answered_count) ────>│            │
   │                        │  status=submitted     │            │
   │<─ Results ────────────│                        │            │
   │                        │  notify teacher stats ─────────────>│  dashboard hub
```

### 3.4 Teacher Statistics & Disqualification Flow
```
Teacher                 API                    Database
   │                        │                        │
   ├─ GET /api/exams/:id/statistics ────────────────>│  aggregate sessions
   │<─ aggregates + student_results (incl. session_id)
   ├─ Disqualify button ──>│  POST /api/questions/disqualify/:sessionId
   │                        ├─ validate teacher owns exam
   │                        ├─ reason (min 3 chars) ─> UPDATE status='disqualified'
   │<─ success + refresh ──│                        │
```

### 3.5 Parent Report Flow
```
Teacher                 API                    Database        Email (SendGrid)
   │                        │                        │            │
   ├─ POST /api/reports/send/:examId                 │            │
   │                        ├─ fetch results ───────>│            │
   │                        ├─ fetch parent_contacts>│            │
   │                        ├─ generate report      │            │
   │                        ├─ send ────────────────────────────────────────>│
   │                        ├─ log parent_notifications ─>                  │
   │<─ confirmation ────────│                        │            │
```

---

## 4. Security Architecture

### 4.1 Authentication & Authorization
- **JWT structure:** claims `userId`, `role` (student/teacher/admin), `tokenVersion`, `exp`, `iat`; 24h expiry + refresh endpoint
- **Token versioning:** `token_version` checked against DB on every authenticated request; password change/rotation invalidates all existing sessions immediately
- **RBAC:** `[RequireAuth]` (default for all non-public routes) + `[RequireRole("teacher")]` style enforcement; explicit `[AllowAnonymous]` only for public endpoints
- **Teacher approval:** teacher accounts start `pending`; login blocked until an admin approves

### 4.2 OTP Security
- Cryptographically secure random code, 5-minute TTL, single-use
- Rate limiting (5 req/min) on auth/OTP endpoints
- OTP codes never returned in API responses when `Environment.Name == production`

### 4.3 Password Security
- **BCrypt** (`BCrypt.Net`) with per-password salt
- Reset via email OTP verification; `change-password` requires current password

### 4.4 API Security
- HTTPS in production; CORS restricted to configured origins (localhost dev + Vercel app)
- SQL injection prevented via Dapper parameterized queries
- Server-side validation on all inputs; ownership checks (teacher owns exam/class/student) before mutations
- Secrets only via environment variables / secret managers — never in responses or logs

### 4.5 Exam Integrity
- Students only ever see their own assigned questions; `correct_answer` and answer keys are never serialized to student endpoints
- Sessions track `ip_address`, `user_agent`, and per-question answers; teachers can disqualify with a persisted reason
- Draft answers persist in `answers` JSONB for resume/auto-submit

### 4.6 Database & External Services
- PostgreSQL with `SSL Mode=Require` in production; Neon managed encryption + automated backups
- Gemini/SendGrid/Redis guarded by `CircuitBreakerService`; API keys in env vars only

---

## 5. Scalability & Performance

### 5.1 Database
- Neon pooled connection (`-pooler` hostname) for serverless-friendly connection reuse
- Optional read replica (`NeonReplica`) offloads reporting/analytics reads
- Indexed foreign keys on high-traffic joins (exams.teacher_id, exam_sessions.student_id/exam_id, question_pool.exam_id)
- List endpoints paginated (limit/offset); JSONB for answers to avoid normalized explosion

### 5.2 API & Caching
- **Redis** (optional) for query caching with `RedisCacheService`; `CircuitBreakerService` degrades gracefully to direct DB when Redis/Gemini/SendGrid fail
- Async I/O throughout (Dapper async, Npgsql async)
- Gemini generation timeouts + retry via circuit breaker
- Stateless API — horizontally scalable on Railway

### 5.3 Frontend
- Static assets served from Vercel's global CDN; no build step
- Lazy loading of exam questions client-side; auto-save drafts every 30s
- PWA (`sw.js`) for offline-first shell + `manifest.json`

### 5.4 Realtime
- SignalR hubs push exam-status changes, notification events, and dashboard updates without polling
- Hub fan-out is lightweight (in-memory groups; Redis backplane possible at scale)

---

## 6. Deployment & Infrastructure

### 6.1 Backend — Railway
- **Container:** `edtech-web/EdTechApi/Dockerfile`
- **Start command:** `dotnet EdTechApi.dll`
- **Environment variables:**
  ```
  NEON_CONNECTION_STRING=postgresql://...pooler...;SSL Mode=Require;Trust Server Certificate=true
  NEON_REPLICA_CONNECTION_STRING=<optional read replica>
  JWT_SECRET=<32-char min>
  GEMINI_API_KEY=<Gemini key>
  SENDGRID_API_KEY=<SendGrid key>
  REDIS_CONNECTION_STRING=<optional Redis>
  GOOGLE_CLIENT_ID / GOOGLE_CLIENT_SECRET
  SENTRY_DSN=<optional>
  ```
- **Migrations:** run automatically at startup — no manual step

### 6.2 Frontend — Vercel
- **Deploy:** point Vercel at the repo; output directory `edtech-web/frontend`
- **Rewrite:** `vercel.json` SPA rewrites map all routes to static files
- **API base:** auto-detected (`/api` same-origin in production, `http://localhost:5000` in dev)
- **PWA:** `sw.js` + `manifest.json` included

### 6.3 Database — Neon
- Managed PostgreSQL 16; pooled connections; automated daily backups; monitoring dashboard

### 6.4 CI/CD & Testing
- Push to `master` → Railway/Vercel auto-deploy
- **Tests:** xUnit project `tests/EdTechApi.Tests` covering AuthService (OTP flows, bcrypt, token version), JwtService (issuance/validation), GeminiService (prompt parsing/validation); uses Moq + Microsoft.AspNetCore.Mvc.Testing
- **Errors:** Sentry DSN wired in `Program.cs` for exception tracking

---

## 7. Disaster Recovery & Backup

### 7.1 Backup Strategy
- **Database:** Neon automated backups (daily, 30-day retention)
- **Code:** GitHub (`https://github.com/Akshit7887/-edtech-web`)
- **Secrets:** Railway/Vercel secret vaults — never in the repo

### 7.2 Recovery Plan
- **RTO:** ~1 hour · **RPO:** 24 hours
- Restore Neon backup → redeploy from GitHub; schema migrations are idempotent so restores and fresh deploys are safe

### 7.3 Monitoring
- Sentry for exceptions · Neon dashboard for DB · Railway metrics (CPU/memory/network)

---

## 8. Future Enhancements

### 8.1 Short Term
- [ ] Redis-backed SignalR backplane for multi-instance realtime
- [ ] Question bank sharing across exams
- [ ] Advanced analytics (per-topic breakdown, cohort trends)

### 8.2 Medium Term
- [ ] Mobile app (React Native/Flutter) reusing the REST API + deep links
- [ ] Video/audio proctoring
- [ ] Essay auto-grading (LLM-assisted scoring)
- [ ] SMS delivery for OTPs/reports (test-sms endpoint already reserved)

### 8.3 Long Term
- [ ] AI-adaptive testing (difficulty adjustment per student)
- [ ] Multi-tenant SaaS platform
- [ ] Offline exam mode with sync

---

## 9. Non-Functional Requirements

| Requirement | Target | Implementation |
|-------------|--------|-----------------|
| **Availability** | 99.5% | Railway + Neon managed services, graceful circuit-breaker fallbacks |
| **Response Time** | <2s (p99) | Dapper + tuned SQL, Redis cache, CDN static assets |
| **Throughput** | 1000 req/s | Stateless API, connection pooling, read replica |
| **Data Consistency** | Strong | ACID transactions, FKs, single primary DB |
| **Security** | High | BCrypt, JWT token-versioning, RBAC, teacher approval, ownership checks |
| **Scalability** | Horizontal | Stateless API, SignalR hubs, Neon pooler |
| **Maintainability** | High | Auto-migrations, xUnit tests, snake_case DTO contracts, structured services |

---

## 10. Technology Stack Summary

| Layer | Technology | Rationale |
|-------|-----------|-----------|
| Frontend | Vanilla JS (static) | Zero deps, zero build, CDN-fast, PWA |
| API | ASP.NET Core 10 | Performance, security primitives, SignalR built-in |
| Database | PostgreSQL 16 (Neon) | ACID, JSONB, serverless-friendly pooling |
| ORM | Dapper + Npgsql | Minimal overhead, explicit SQL |
| Auth | JWT + OTP + Google OAuth | Stateless, multi-factor-friendly |
| AI | Google Gemini | State-of-the-art generation for questions/exams |
| Email | SendGrid/SMTP | Reliable delivery for OTPs and parent reports |
| Realtime | SignalR | Live dashboards and notifications without polling |
| Caching | Redis (optional) | Cache + circuit-breaker degradation |
| Observability | Sentry | Exception tracking |
| Deployment | Railway + Vercel + Neon | Low-maintenance, auto-deploy on push |
| Tests | xUnit + Moq | Unit + integration coverage for core services |

---

## 11. Contact & Support

- **Repository:** https://github.com/Akshit7887/-edtech-web
- **Issues:** GitHub Issues for bug reports and feature requests
- **Documentation:** README.md (endpoints, setup, env vars) and this HLD