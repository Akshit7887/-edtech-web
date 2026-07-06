# EdTech App — System Design

**Note:** This file previously described an old Node/Express/Sequelize/React Native stack that no longer reflects the codebase. The current tech stack is documented below.

## Current Architecture

```mermaid
flowchart TD
    subgraph CLIENT["Frontend — Vanilla JS (HTML/CSS/JS)"]
        A1["Auth Pages<br/>Login · Register · OTP<br/>Forgot/Reset Password"]
        A2["Teacher Pages<br/>Dashboard · Create Exam · Question Bank<br/>Student Mgmt · Class Mgmt"]
        A3["Student Pages<br/>Exam List · Exam Screen · Results<br/>Practice Mode · Analytics"]
    end

    subgraph API["REST API — ASP.NET Core 10"]
        B1["/api/auth/* — Auth (JWT + OTP)"]
        B2["/api/exams/* — Exam CRUD"]
        B3["/api/questions/* — Questions & Sessions"]
        B4["/api/students/* — Student Endpoints"]
        B5["/api/teacher/* — Teacher Endpoints"]
        B6["/api/reports/* — Parent Reports"]
    end

    subgraph BACKEND["Services"]
        C1["GeminiService — AI Question Generation"]
        C2["JwtService — JWT Token Management"]
        C3["OtpService — OTP Generation"]
        C4["EmailService — Email Delivery"]
        C5["GoogleAuthService — Google OAuth"]
    end

    subgraph DB["Database — Neon Postgres (PostgreSQL 16)"]
        D1["Users · Exams · QuestionPool"]
        D2["ExamSessions · StudentExamAssignments"]
        D3["Attendance · Notifications"]
        D4["Classes · ClassStudents"]
        D5["ParentContacts · ParentNotifications"]
        D6["OtpTokens · PendingRegistrations"]
    end

    CLIENT --> API
    API --> BACKEND
    API --> DB
```

## Tech Stack

| Layer | Technology |
|-------|-----------|
| Backend | ASP.NET Core 10 (C#) |
| Database | PostgreSQL 16 (Neon) |
| ORM | Dapper + Npgsql |
| Auth | JWT + OTP (Email) + Google OAuth |
| AI | Google Gemini API |
| Frontend | Vanilla HTML, CSS, JavaScript |
| Deployment | Railway (backend), Vercel (frontend) |

## Key Design Decisions

- **Custom JWT middleware** instead of ASP.NET Identity for lightweight auth
- **Dapper** over EF Core for performance on exam queries
- **Role-based auth** via `[RequireAuth]` and `[RequireRole]` attributes
- **Neon Postgres** with pooled connection for serverless compatibility
- **Gemini AI** for automated question/exam generation
