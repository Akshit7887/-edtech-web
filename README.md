# EdTech Web

> AI-powered examination platform built with ASP.NET Core 10 and Neon/PostgreSQL.

## Overview

EdTech is a production-grade web application for creating, managing, and taking exams. Teachers can generate questions using Google Gemini AI, schedule exams, assign them to students, and track results in real time with live statistics and attendance reports. Students take timed exams with instant auto-grading and detailed performance reviews, practice mode, syllabus files, and class-based organization. Admins manage users, departments, classes, and approve teacher registrations.

## Tech Stack

| Layer | Technology |
|-------|-----------|
| Backend | ASP.NET Core 10, C# |
| Database | PostgreSQL 16 (Neon), read replica support |
| ORM | Dapper + Npgsql |
| Auth | JWT + OTP (SendGrid/SMTP) + Google OAuth |
| AI | Google Gemini API |
| Caching | Redis (optional, with circuit breaker fallback) |
| Realtime | SignalR hubs (dashboard, exam, notifications) |
| Frontend | Vanilla HTML, CSS, JavaScript (static, no build step) |
| Observability | Sentry |
| Deployment | Railway (backend), Vercel (frontend) |
| Tests | xUnit (AuthService, JwtService, GeminiService) |

## Project Structure

```
edtech-web/
├── EdTechApi/                # ASP.NET Core 10 backend
│   ├── Controllers/          # Auth, Exam, Question, Teacher, Student, Admin,
│   │                         # Reports, Syllabus, Department, GoogleAuth
│   ├── Services/             # Business logic + DB access (Dapper)
│   ├── Models/               # Entity models
│   ├── DTOs/                 # Request/response DTOs (snake_case JSON)
│   └── Program.cs            # DI, CORS, SignalR hubs, migrations, Sentry
└── frontend/                 # Static vanilla JS frontend (Vercel)
    ├── pages/
    │   ├── admin/            # Dashboard, users, exams, classes, departments, db-monitor
    │   ├── teacher/          # Dashboard, create/edit exam, questions, students,
    │   │                     # classes, attendance, statistics, reports,
    │   │                     # parent-contacts, syllabus, profile
    │   └── student/          # Dashboard, exam, results, review, practice,
    │                         # classes, notifications, syllabus, profile
    ├── js/                   # api.js, auth.js, teacher.js, admin.js, nav.js, etc.
    ├── css/                  # main.css, dashboard.css
    ├── vercel.json           # SPA rewrites for static hosting
    └── sw.js                 # PWA service worker

tests/EdTechApi.Tests/        # xUnit test project
```

## API Endpoints

Base URL: `/api`. All responses use the envelope `{ "success": true, "data": ... }` / `{ "success": false, "error": "..." }`. JSON is `snake_case`.

### Authentication
| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | `/api/auth/generate-otp` | Send OTP to email (login) |
| POST | `/api/auth/verify-otp` | Verify OTP and get JWT |
| POST | `/api/auth/send-register-otp` | Send registration OTP |
| POST | `/api/auth/verify-register-otp` | Verify registration OTP and create account |
| POST | `/api/auth/admin-login` | Admin password login |
| POST | `/api/auth/forgot-password` | Request password reset OTP |
| POST | `/api/auth/reset-password` | Reset password |
| POST | `/api/auth/refresh-token` | Refresh JWT |
| PUT | `/api/auth/profile` | Update profile |
| POST | `/api/auth/change-password` | Change password |
| DELETE | `/api/auth/profile` | Delete account |
| POST | `/api/auth/external-session` | Create session from Google OAuth |

### Google Auth
| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/auth/google/login` | Redirect to Google OAuth |
| GET | `/auth/google/callback` | OAuth callback |
| POST | `/auth/google/signin` | Exchange Google token |
| GET | `/auth/google/config` | OAuth client config |

### Exams
| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/exams` | List exams (paginated, role-filtered) |
| GET | `/api/exams/:id` | Get exam details + questions |
| POST | `/api/exams` | Create exam (teacher) |
| PUT | `/api/exams/:id` | Update exam (status transitions validated) |
| DELETE | `/api/exams/:id` | Delete exam |
| POST | `/api/exams/:id/activate` | Activate exam |
| POST | `/api/exams/ai-create` | AI-generated exam (Gemini) |
| POST | `/api/exams/:id/publish-questions` | Publish questions to students |
| POST | `/api/exams/:id/bulk-import` | Bulk import questions |
| GET | `/api/exams/:id/statistics` | Exam statistics |
| GET | `/api/exams/:id/attendance` | Attendance report |
| GET | `/api/exams/:id/export-pdf` | Export results (PDF) |
| GET | `/api/exams/:id/deep-link` | Get exam deep link |
| GET | `/api/exams/deep-link/:code` | Resolve deep link |

### Questions & Sessions
| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | `/api/questions/generate` | AI-generate questions |
| POST | `/api/questions/assign` | Assign questions to students |
| POST | `/api/questions/create-session` | Create exam session |
| POST | `/api/questions/submit` | Submit answers with auto-grading |
| GET | `/api/questions/session/:studentId/:examId` | Get session |
| POST | `/api/questions/disqualify/:sessionId` | Disqualify student |
| GET | `/api/questions/statistics/:examId` | Question-wise stats |
| POST | `/api/questions/generate-personalized` | Personalized questions |
| GET | `/api/questions/my-results/:studentId` | Student results |

### Teacher
| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/teacher/students` | List students (with class info) |
| POST | `/api/teacher/students` | Create student account |
| GET | `/api/teacher/students/search-by-sid` | Search by student ID |
| GET | `/api/teacher/students/:id` | Student detail (profile, classes, history) |
| DELETE | `/api/teacher/students/:id` | Delete student |
| GET/POST/PUT/DELETE | `/api/teacher/questions/...` | Question bank CRUD per exam |
| POST | `/api/teacher/classes` | Create class |
| GET | `/api/teacher/classes` | List classes |
| GET | `/api/teacher/classes/:id` | Class detail |
| POST | `/api/teacher/classes/:id/students` | Add student to class |
| DELETE | `/api/teacher/classes/:id/students/:studentId` | Remove student from class |
| DELETE | `/api/teacher/classes/:id` | Delete class |
| POST | `/api/teacher/announcement` | Send announcement |
| PUT | `/api/teacher/schedule/:examId` | Schedule exam (empty body clears) |
| GET | `/api/teacher/parent-contacts` | List parent contacts |
| POST | `/api/teacher/parent-contacts/:studentId` | Create/update parent contact |
| DELETE | `/api/teacher/parent-contacts/:studentId` | Delete parent contact |
| GET | `/api/teacher/report-history/:examId` | Report history |

### Student
| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/students/analytics/:studentId` | Performance analytics |
| GET | `/api/students/review/:sessionId` | Exam review |
| POST | `/api/students/practice/start` | Start practice session |
| POST | `/api/students/practice/submit` | Submit practice |
| GET | `/api/students/notifications` | Notifications |
| PUT | `/api/students/notifications/:id/read` | Mark notification read |
| PUT | `/api/students/notifications/read-all` | Mark all read |
| GET | `/api/students/classes` | Student's classes |

### Reports
| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | `/api/reports/send/:examId` | Send parent reports |
| GET | `/api/reports/pending/:examId` | Pending report queue |
| POST | `/api/reports/test-email` | Test email config |
| POST | `/api/reports/test-sms` | Test SMS config |

### Admin
| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/admin/stats` | Platform stats |
| GET | `/api/admin/pending-teachers` | Teacher approval queue |
| POST | `/api/admin/teachers/:id/approve` | Approve teacher |
| POST | `/api/admin/teachers/:id/reject` | Reject teacher |
| GET | `/api/admin/users` | List users (role/status filters, paginated) |
| POST | `/api/admin/users` | Create user (any role, sets approval) |
| PUT | `/api/admin/users/:id` | Update user (profile, role, department, status, password) |
| DELETE | `/api/admin/users/:id` | Delete user + all related data |
| GET | `/api/admin/exams` | All exams (paginated) |
| GET | `/api/admin/exams/:id` | Exam detail (questions, sessions, stats) |
| DELETE | `/api/admin/exams/:id` | Delete exam + related data |
| GET | `/api/admin/db-snapshot` | DB monitor snapshot |
| GET | `/api/admin/classes` | All classes (paginated) |
| GET | `/api/admin/classes/:id` | Class detail + students |
| POST | `/api/admin/classes/:id/students` | Add student to class |
| DELETE | `/api/admin/classes/:id/students/:studentId` | Remove student from class |
| DELETE | `/api/admin/classes/:id` | Delete class |
| POST | `/api/admin/backfill-student-ids` | Backfill missing student IDs |

### Syllabus
| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/syllabus` | List syllabus files |
| GET | `/api/syllabus/:id` | Syllabus detail |
| GET | `/api/syllabus/my` | My accessible syllabi |
| POST | `/api/syllabus/upload` | Upload syllabus file |
| GET | `/api/syllabus/:id/download` | Download file |
| PATCH | `/api/syllabus/:id` | Update syllabus |
| DELETE | `/api/syllabus/:id` | Delete syllabus |

### Departments
| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/departments` | List departments |
| GET | `/api/departments/:id` | Department detail |
| POST | `/api/departments` | Create department |
| PUT | `/api/departments/:id` | Update department |
| DELETE | `/api/departments/:id` | Delete department |
| POST | `/api/departments/assign` | Assign user to department |
| POST | `/api/departments/remove-user/:userId` | Remove user from department |
| GET | `/api/departments/:id/users` | Department users |

### Realtime Hubs (SignalR)
| Hub | Purpose |
|-----|---------|
| `/hubs/dashboard` | Teacher dashboard updates |
| `/hubs/exam` | Exam group notifications |
| `/hubs/notification` | Per-user notifications |

## Database Schema

PostgreSQL 16 (Neon), tables created/updated automatically at startup by `MigrationService`:

- `Users` — name, email, phone, `password_hash` (BCrypt), `role` (student/teacher/admin), `student_id`, `approval_status` (teacher approval flow), `token_version` (session invalidation), `department_id`
- `Exams` — title, subject, `syllabus_text`, `duration_minutes`, `total_questions`, `status` (draft/scheduled/active/ended), `scheduled_at`, `scheduled_end_at`, `allow_reattempt`, `deep_link_code`, `teacher_id`
- `QuestionPool` — questions with options (A–D), `correct_answer`, explanation, status per exam
- `ExamSessions` — per-student attempt: `score`, `answered_count`, `status` (in_progress/submitted/disqualified), `disqualified_reason`, `answers` (JSONB), IP/user-agent, mode (exam/practice)
- `StudentExamAssignments` — question assignments per student
- `Attendance` — attendance records per exam
- `Notifications` — in-app notifications
- `Classes` / `ClassStudents` — class management (teacher-owned, subject)
- `ParentContacts` / `ParentNotifications` — parent reporting (incl. `relationship`)
- `OtpTokens` — OTP verification (5-min TTL, single-use)
- `PendingRegistrations` — pre-registration OTP records
- `SyllabusFiles` — syllabus uploads (`file_data` BYTEA, class-scoped)
- `Departments` — org structure; `Users.department_id` FK
- `_Migrations` — migration ledger

## Getting Started

### Prerequisites
- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- PostgreSQL 16 (or Neon account) — optional: Redis for caching
- Any static file server for the frontend (e.g. `npx serve`, Vercel)

### Backend

```bash
git clone https://github.com/Akshit7887/-edtech-web.git
cd -edtech-web/edtech-web/EdTechApi

# Configure appsettings.json (or environment variables):
# NEON_CONNECTION_STRING, JWT_SECRET, GEMINI_API_KEY, SENDGRID_API_KEY

dotnet restore
dotnet run
```

Migrations run automatically on startup (no manual SQL needed).

### Frontend

```bash
cd edtech-web/frontend
npx serve .          # serves on http://localhost:3000
```

The frontend auto-detects the API base URL (same origin, or `http://localhost:5000` in dev). No build step.

### Tests

```bash
cd tests/EdTechApi.Tests
dotnet test
```

### Configuration

Set via environment variables (recommended for production) or `appsettings.json`:

| Variable | Config Key | Description |
|----------|-----------|-------------|
| `NEON_CONNECTION_STRING` | `ConnectionStrings:Neon` | PostgreSQL connection string (Neon). Use the `-pooler` hostname for pooled connections. Set `SSL Mode=Require;Trust Server Certificate=true` for production. |
| `NEON_REPLICA_CONNECTION_STRING` | `ConnectionStrings:NeonReplica` | Optional read-replica connection string |
| `JWT_SECRET` | `Jwt:Secret` | JWT signing key (min 32 chars) |
| `GEMINI_API_KEY` | `Gemini:ApiKey` | Google Gemini API key |
| `SENDGRID_API_KEY` | `SendGrid:ApiKey` | SendGrid API key for email |
| `REDIS_CONNECTION_STRING` | `Redis:ConnectionString` | Optional Redis cache (falls back gracefully) |
| `GOOGLE_CLIENT_ID` | `Google:ClientId` | Google OAuth client ID |
| `GOOGLE_CLIENT_SECRET` | `Google:ClientSecret` | Google OAuth client secret |
| `SENTRY_DSN` | `Sentry:Dsn` | Sentry error tracking DSN |

Secrets are never committed to `appsettings.json` — placeholder values in the repo are for local dev only.

## Error Format

```json
{
  "success": false,
  "error": "Error message",
  "requestId": "trace-id"
}
```

Success responses:

```json
{
  "success": true,
  "data": { ... },
  "message": "Success message"
}
```

## License

MIT