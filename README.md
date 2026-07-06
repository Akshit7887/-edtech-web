# EdTech Web

> AI-powered examination platform built with ASP.NET Core and Neon/PostgreSQL.

## Overview

EdTech is a production-grade web application for creating, managing, and taking exams. Teachers can generate questions using Google Gemini AI, assign exams to students, and track results in real time. Students take timed exams with instant auto-grading and detailed performance reviews.

## Tech Stack

| Layer | Technology |
|-------|-----------|
| Backend | ASP.NET Core 10, C# |
| Database | PostgreSQL 16 (Neon) |
| ORM | Dapper + Npgsql |
| Auth | JWT + OTP (ZeptoMail/SMTP) |
| AI | Google Gemini API |
| Frontend | HTML, CSS, JavaScript (Vanilla) |
| Deployment | Railway (backend), Vercel (frontend) |

## API Endpoints

### Authentication
| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | `/api/auth/generate-otp` | Send OTP to email |
| POST | `/api/auth/verify-otp` | Verify OTP and get JWT |
| POST | `/api/auth/register` | Register new user |
| POST | `/api/auth/forgot-password` | Request password reset OTP |
| POST | `/api/auth/reset-password` | Reset password |
| POST | `/api/auth/refresh-token` | Refresh JWT |
| PUT | `/api/auth/profile` | Update profile |
| POST | `/api/auth/change-password` | Change password |
| GET | `/api/auth/me` | Get current user |

### Exams
| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/exams` | List exams (paginated) |
| GET | `/api/exams/:id` | Get exam details |
| POST | `/api/exams` | Create exam (teacher) |
| PUT | `/api/exams/:id` | Update exam |
| DELETE | `/api/exams/:id` | Delete exam |
| POST | `/api/exams/:id/activate` | Activate exam |
| POST | `/api/exams/ai-create` | AI-generated exam |
| POST | `/api/exams/:id/publish-questions` | Publish questions |
| GET | `/api/exams/:id/statistics` | Exam statistics |
| GET | `/api/exams/:id/attendance` | Attendance report |
| GET | `/api/exams/:id/export-pdf` | Export results |
| GET | `/api/exams/deep-link/:code` | Resolve deep link |

### Questions
| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | `/api/questions/generate` | AI-generate questions |
| POST | `/api/questions/assign` | Assign questions to students |
| POST | `/api/questions/create-session` | Create exam session |
| POST | `/api/questions/submit` | Submit exam answers |
| GET | `/api/questions/session/:studentId/:examId` | Get session |
| POST | `/api/questions/disqualify/:sessionId` | Disqualify student |
| GET | `/api/questions/statistics/:examId` | Question stats |
| POST | `/api/questions/generate-personalized` | Personalized questions |
| GET | `/api/questions/my-results/:studentId` | Student results |

### Teacher
| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/teacher/students` | List students |
| GET | `/api/teacher/students/:id` | Student detail |
| GET/POST/PUT/DELETE | `/api/teacher/questions/:examId` | Question bank CRUD |
| POST | `/api/teacher/classes` | Create class |
| GET | `/api/teacher/classes` | List classes |
| POST | `/api/teacher/parent-contacts` | Manage parent contacts |
| GET | `/api/teacher/report-history/:examId` | Report history |

### Student
| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/students/analytics/:studentId` | Student analytics |
| GET | `/api/students/review/:sessionId` | Exam review |
| POST | `/api/students/practice/start` | Start practice |
| POST | `/api/students/practice/submit` | Submit practice |
| GET | `/api/students/notifications` | Notifications |
| PUT | `/api/students/notifications/:id/read` | Mark read |

### Reports
| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | `/api/reports/send/:examId` | Send parent reports |
| GET | `/api/reports/pending/:examId` | Pending reports |
| POST | `/api/reports/test-email` | Test email config |

## Database Schema

The database uses PostgreSQL with the following tables:

- `users` — Teachers and students
- `exams` — Exam definitions
- `question_pool` — Questions bank
- `exam_sessions` — Student exam attempts
- `student_exam_assignments` — Question assignments
- `attendance` — Attendance tracking
- `notifications` — In-app notifications
- `classes` / `class_students` — Class management
- `parent_contacts` / `parent_notifications` — Parent reporting
- `otp_tokens` — OTP verification
- `pending_registrations` — Pre-registration

## Getting Started

### Prerequisites
- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- PostgreSQL 16 (or Neon account)

### Setup

```bash
# Clone the repo
git clone https://github.com/Akshit7887/-edtech-web.git
cd -edtech-web/edtech-web/EdTechApi

# Configure appsettings.json with your credentials
# (Neon connection string, JWT secret, Gemini API key, SMTP settings)

# Restore and run
dotnet restore
dotnet run
```

### Configuration

Set via environment variables (recommended for production) or `appsettings.json`:

| Variable | Config Key | Description |
|----------|-----------|-------------|
| `NEON_CONNECTION_STRING` | `ConnectionStrings:Neon` | PostgreSQL connection string (Neon). Use the `-pooler` hostname for Railway/Neon pooled connections. Set `SSL Mode=Require;Trust Server Certificate=true` for production. |
| `JWT_SECRET` | `Jwt:Secret` | JWT signing key (min 32 chars) |
| `GEMINI_API_KEY` | `Gemini:ApiKey` | Google Gemini API key |
| `SENDGRID_API_KEY` | `SendGrid:ApiKey` | SendGrid API key for email |
| `GOOGLE_CLIENT_ID` | `Google:ClientId` | Google OAuth client ID |
| `GOOGLE_CLIENT_SECRET` | `Google:ClientSecret` | Google OAuth client secret |

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
