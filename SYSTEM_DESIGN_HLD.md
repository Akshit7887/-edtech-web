# EdTech Platform — System Design High-Level Document (HLD)

## Executive Summary

EdTech is an AI-powered examination platform that enables teachers to create, manage, and grade exams while students take timed assessments with real-time auto-grading. The system supports role-based access, AI-driven question generation, OTP-based authentication, and comprehensive exam analytics.

---

## 1. System Architecture Overview

```
┌─────────────────────────────────────────────────────────────────┐
│                     CLIENT LAYER                                 │
│  (Vanilla JS Frontend - HTML/CSS/JavaScript)                    │
│  ├── Authentication (OTP, JWT, Password Reset)                  │
│  ├── Teacher Portal (Exam Management, Analytics, Reports)       │
│  └── Student Portal (Exams, Results, Practice Mode)             │
└─────────────────────┬───────────────────────────────────────────┘
                      │ HTTPS
                      ▼
┌─────────────────────────────────────────────────────────────────┐
│              API GATEWAY / REST LAYER                            │
│  (ASP.NET Core 10, C#)                                          │
│  ├── /api/auth/*          - Authentication & Authorization      │
│  ├── /api/exams/*         - Exam CRUD & Lifecycle               │
│  ├── /api/questions/*     - Question Generation & Sessions      │
│  ├── /api/students/*      - Student Analytics & Results         │
│  ├── /api/teacher/*       - Teacher Dashboard & Management      │
│  └── /api/reports/*       - Report Generation & Distribution    │
└─────────────────────┬───────────────────────────────────────────┘
                      │
        ┌─────────────┼──────────────┐
        ▼             ▼              ▼
┌──────────────┐ ┌──────────────┐ ┌──────────────┐
│   SERVICES   │ │  EXTERNAL    │ │  DATABASE    │
│              │ │   APIs       │ │   LAYER      │
└──────────────┘ └──────────────┘ └──────────────┘
```

---

## 2. Component Architecture

### 2.1 Frontend Layer
**Technology:** HTML5, CSS3, Vanilla JavaScript (No Framework)
**Deployment:** Vercel

**Components:**
- **Authentication Module**
  - OTP verification form
  - Login/Register pages
  - Password reset flow
  - Session management (JWT in localStorage)

- **Teacher Portal**
  - Exam creation/editing dashboard
  - Question bank management
  - AI question generation interface
  - Student roster and assignment management
  - Real-time exam statistics and monitoring
  - Report generation and distribution UI
  - Class and section management

- **Student Portal**
  - Exam list and enrollment
  - Timed exam interface with auto-save
  - Results and analytics dashboard
  - Practice mode
  - Session review and answer walkthrough

---

### 2.2 API Layer (ASP.NET Core 10)
**Technology:** ASP.NET Core 10, C#
**Deployment:** Railway

**Core Components:**

#### Authentication & Authorization
- **Endpoints:**
  - `POST /api/auth/generate-otp` — Send OTP to email
  - `POST /api/auth/verify-otp` — Verify OTP and issue JWT
  - `POST /api/auth/register` — New user registration
  - `POST /api/auth/reset-password` — Password reset
  - `POST /api/auth/refresh-token` — JWT refresh
  - `GET /api/auth/me` — Current user profile

- **Auth Mechanism:**
  - JWT (JSON Web Tokens) with RS256/HS256 signing
  - OTP via email (ZeptoMail/SMTP)
  - Role-based access control (RBAC): Teacher, Student, Admin
  - Custom `[RequireAuth]` and `[RequireRole]` middleware

#### Exam Management
- **Endpoints:**
  - `GET /api/exams` — List exams (paginated, filtered by role)
  - `GET /api/exams/:id` — Detailed exam view
  - `POST /api/exams` — Create exam (teacher-only)
  - `PUT /api/exams/:id` — Update exam metadata
  - `DELETE /api/exams/:id` — Delete exam
  - `POST /api/exams/:id/activate` — Make exam live
  - `POST /api/exams/:id/publish-questions` — Release questions to students

- **Core Logic:**
  - Exam scheduling and time windows
  - Question pool association
  - Student assignment batching
  - Attendance tracking
  - Deep-link resolution for quick exam access

#### Question & Session Management
- **Endpoints:**
  - `POST /api/questions/generate` — AI-generate questions via Gemini
  - `POST /api/questions/assign` — Bulk assign questions to students
  - `POST /api/questions/create-session` — Initialize exam session
  - `POST /api/questions/submit` — Submit answers with auto-grading
  - `GET /api/questions/session/:studentId/:examId` — Retrieve session state
  - `POST /api/questions/disqualify/:sessionId` — Mark session invalid
  - `GET /api/questions/statistics/:examId` — Question-wise analytics

- **Core Logic:**
  - AI question generation (Gemini)
  - Session timeout handling
  - Answer validation and auto-grading (MCQ/Short Answer)
  - Plagiarism detection placeholder
  - Question randomization per student

#### Student Analytics
- **Endpoints:**
  - `GET /api/students/analytics/:studentId` — Performance metrics
  - `GET /api/students/review/:sessionId` — Answer review
  - `POST /api/students/practice/start` — Start practice session
  - `GET /api/students/notifications` — Notification feed

#### Teacher Dashboard
- **Endpoints:**
  - `GET /api/teacher/students` — Class roster
  - `GET /api/teacher/classes` — List managed classes
  - `GET /api/teacher/students/:id` — Individual student profile
  - `POST /api/teacher/parent-contacts` — Add parent email
  - `GET /api/teacher/report-history/:examId` — Historical reports

#### Reports & Notifications
- **Endpoints:**
  - `POST /api/reports/send/:examId` — Generate & send parent reports
  - `GET /api/reports/pending/:examId` — Pending report queue
  - `POST /api/reports/test-email` — Email configuration test

---

### 2.3 Service Layer

**Key Services:**

1. **GeminiService**
   - Calls Google Gemini API for intelligent question generation
   - Supports MCQ, short-answer, and essay question types
   - Caches generated questions
   - Handles rate limiting and retry logic

2. **JwtService**
   - Token generation with custom claims (userId, role, exp)
   - Token validation and refresh logic
   - Expiration handling (short-lived tokens with refresh tokens)

3. **OtpService**
   - OTP generation (6-digit alphanumeric)
   - Redis/In-memory caching with TTL (5-10 minutes)
   - Rate limiting per email
   - Verification logic

4. **EmailService**
   - SMTP/SendGrid integration
   - OTP delivery
   - Parent report generation and distribution
   - Exam notifications

5. **GoogleAuthService**
   - Google OAuth 2.0 integration
   - Token validation
   - User profile mapping

6. **AutoGradingService**
   - MCQ evaluation (case-insensitive)
   - Short-answer keyword matching
   - Essay scoring placeholder (manual or ML-based)
   - Score calculation and ranking

7. **AttendanceService**
   - Session start/end tracking
   - Attendance marking
   - Late submission detection

---

### 2.4 Database Layer
**Technology:** PostgreSQL 16 (Neon), Dapper ORM, Npgsql

**Connection Strategy:**
- Neon connection pool for serverless compatibility
- SSL mode required for Railway deployment
- Prepared statements via Dapper for performance

**Core Tables:**

#### Users & Authentication
```sql
users
├── id (UUID PK)
├── email (UNIQUE)
├── password_hash
├── otp_tokens (FK)
├── role (enum: teacher, student, admin)
├── first_name, last_name
├── created_at, updated_at
└── is_active

otp_tokens
├── id (UUID PK)
├── user_id (FK users)
├── token (6-digit)
├── expires_at
└── is_used

pending_registrations
├── id (UUID PK)
├── email
├── otp_token
├── temp_data (JSON)
└── expires_at
```

#### Exams & Questions
```sql
exams
├── id (UUID PK)
├── created_by (FK users - teacher)
├── title, description
├── subject, class_level
├── duration_minutes
├── total_marks
├── passing_percentage
├── status (draft, scheduled, active, ended)
├── start_time, end_time
├── is_published
├── deep_link_code (unique)
└── metadata (JSON: rules, negative_marking, etc)

question_pool
├── id (UUID PK)
├── exam_id (FK exams)
├── question_text
├── question_type (mcq, short_answer, essay)
├── options (JSONB for MCQ)
├── correct_answer
├── difficulty_level
├── marks
├── explanation
├── created_by (FK users)
└── is_active

student_exam_assignments
├── id (UUID PK)
├── student_id (FK users)
├── exam_id (FK exams)
├── assigned_at
├── question_ids (JSONB - randomized per student)
└── is_submitted
```

#### Exam Sessions & Results
```sql
exam_sessions
├── id (UUID PK)
├── student_id (FK users)
├── exam_id (FK exams)
├── status (in_progress, submitted, disqualified)
├── started_at
├── submitted_at
├── total_score
├── percentage
├── is_passed
├── answers (JSONB: {q_id: answer, ...})
├── metadata (JSON: device_info, location, etc)
└── attempt_number

attendance
├── id (UUID PK)
├── student_id (FK users)
├── exam_id (FK exams)
├── marked_at
├── is_present
└── session_id (FK exam_sessions)
```

#### Classes & Organization
```sql
classes
├── id (UUID PK)
├── teacher_id (FK users)
├── name, section, academic_year
└── created_at

class_students
├── id (UUID PK)
├── class_id (FK classes)
├── student_id (FK users)
└── joined_at

parent_contacts
├── id (UUID PK)
├── student_id (FK users)
├── parent_email (can be multiple)
├── parent_name
├── relationship
└── is_primary

parent_notifications
├── id (UUID PK)
├── parent_id (FK parent_contacts)
├── exam_id (FK exams)
├── report_type (pdf, email_summary)
├── sent_at
└── content (JSONB)

notifications
├── id (UUID PK)
├── user_id (FK users)
├── type (exam_assigned, results_ready, exam_reminder)
├── content
├── is_read
├── created_at
└── read_at
```

---

## 3. Data Flow & Use Cases

### 3.1 Exam Creation Flow
```
Teacher                 API                    Database
   │                     │                         │
   ├─ Create Exam ─────>│                         │
   │                     ├─ Validate Auth ──────> │
   │                     ├─ Create Exam ────────> │
   │                     │                    INSERT exam
   │                     │<─────── exam_id ───────│
   │<─ Return exam_id ───│                         │
   │                     │                         │
   ├─ Generate Q's  ───>│                         │
   │(via Gemini)        ├─ Call GeminiService    │
   │                     ├─ Parse Response       │
   │                     ├─ Save Questions ────> │
   │                     │                    INSERT questions
   │<─ Return Q's ───────│                         │
```

### 3.2 Student Exam Taking Flow
```
Student                 API                    Database       Gemini
   │                     │                         │              │
   ├─ Login (OTP) ──────>│                         │              │
   │                     ├─ Generate OTP ────────>│              │
   │                     ├─ Send Email ──────────────────────────>│
   │                     │                         │              │
   ├─ Verify OTP ──────>│                         │              │
   │                     ├─ Validate OTP ────────>│              │
   │                     ├─ Create JWT            │              │
   │<─ JWT Token ───────│                         │              │
   │                     │                         │              │
   ├─ Load Exam ───────>│                         │              │
   │                     ├─ Fetch Questions ─────>│              │
   │                     │<─ Questions ───────────│              │
   │<─ Display Questions │                         │              │
   │                     │                         │              │
   ├─ Submit Answers ──>│                         │              │
   │(every 30s)         ├─ Save Draft ──────────>│              │
   │                     │                    UPDATE exam_session
   │                     │                         │              │
   ├─ Final Submit ────>│                         │              │
   │                     ├─ AutoGrade ────────────┤              │
   │                     ├─ Calculate Score ─────>│              │
   │                     │                    UPDATE results
   │<─ Results ─────────│                         │              │
```

### 3.3 Report Generation Flow
```
Teacher                 API                    Database       Email
   │                     │                         │              │
   ├─ Request Report ──>│                         │              │
   │                     ├─ Fetch Results ───────>│              │
   │                     │<─ Student Data ────────│              │
   │                     ├─ Generate PDF          │              │
   │                     │                         │              │
   ├─ Trigger Send ────>│                         │              │
   │                     ├─ Get Parent Email ────>│              │
   │                     │<─ Parent Contacts ─────│              │
   │                     ├─ Send Email ──────────────────────────>│
   │                     │                         │          Email sent
   │<─ Confirmation ────│                         │              │
```

---

## 4. Security Architecture

### 4.1 Authentication & Authorization
- **JWT Token Structure:**
  ```json
  {
    "sub": "user-id",
    "role": "student|teacher|admin",
    "email": "user@example.com",
    "exp": 1700000000,
    "iat": 1699996400
  }
  ```
- **Token Lifetime:** 1 hour (JWT), 7 days (Refresh Token)
- **Refresh Token Storage:** Secure HttpOnly cookie or localStorage

### 4.2 OTP Security
- **Generation:** Cryptographically secure random 6-digit code
- **Storage:** Redis with TTL (5-10 minutes)
- **Rate Limiting:** Max 3 attempts per email per 5 minutes
- **Delivery:** SMTP/SendGrid over TLS

### 4.3 Password Security
- **Hashing:** Bcrypt (ASP.NET Core Identity's default)
- **Salt:** Auto-generated per password
- **Reset Flow:** Email-based OTP verification

### 4.4 API Security
- **HTTPS:** Enforced in production (Railway/Vercel)
- **CORS:** Configured for frontend domain(s)
- **Rate Limiting:** API throttling per user/IP
- **SQL Injection:** Prevented via Dapper prepared statements
- **CSRF:** Token validation for state-changing requests
- **Input Validation:** Server-side validation for all inputs
- **Sensitive Data:** No passwords/secrets in logs or error responses

### 4.5 Database Security
- **SSL/TLS:** PostgreSQL connection with SSL Mode=Require
- **Least Privilege:** Separate DB user with minimal permissions
- **Backups:** Neon automated backup policy
- **Data Encryption:** At-rest via Neon's managed encryption

### 4.6 External Service Security
- **API Keys:** Stored in environment variables (never in code)
- **OAuth:** Google OAuth 2.0 with secure redirect URIs
- **Gemini API:** Rate limiting and usage monitoring

---

## 5. Scalability & Performance

### 5.1 Database Optimization
- **Connection Pooling:** Neon connection pool for serverless
- **Indexing Strategy:**
  ```sql
  -- High-traffic queries
  CREATE INDEX idx_exams_created_by ON exams(created_by);
  CREATE INDEX idx_exam_sessions_student ON exam_sessions(student_id, exam_id);
  CREATE INDEX idx_questions_pool_exam ON question_pool(exam_id);
  CREATE INDEX idx_student_assignments_exam ON student_exam_assignments(exam_id);
  ```
- **Query Optimization:** Dapper for lightweight ORM with hand-tuned SQL
- **Pagination:** All list endpoints support offset/limit

### 5.2 API Performance
- **Caching Strategy:**
  - Questions: Redis cache (30 min TTL)
  - User profiles: Redis cache (10 min TTL)
  - Exam metadata: In-memory cache with cache-busting
- **Async Operations:**
  - Email dispatch via background jobs (Hangfire/Azure Service Bus)
  - Report generation as async tasks
  - Gemini API calls with timeout and retry
- **Load Balancing:** Railway auto-scaling (horizontal pod autoscaling)

### 5.3 Frontend Optimization
- **Lazy Loading:** Questions loaded on-demand
- **Compression:** Gzip for HTML/CSS/JS assets
- **CDN:** Vercel global CDN for static assets
- **Pagination:** Infinite scroll or page-based for exam lists

### 5.4 Concurrent Exam Sessions
- **Expected Capacity:** 1000+ concurrent students per exam
- **Session Management:** In-memory session store with Redis fallback
- **WebSocket Consideration:** For real-time notifications (future enhancement)

---

## 6. Deployment & Infrastructure

### 6.1 Backend Deployment (Railway)
- **Platform:** Railway.app (Railway)
- **Containerization:** Docker
- **Process:** `dotnet run` or `dotnet EdTechApi.dll`
- **Environment Variables:**
  ```
  NEON_CONNECTION_STRING=postgresql://...
  JWT_SECRET=<32-char min secret>
  GEMINI_API_KEY=<API key>
  SENDGRID_API_KEY=<API key>
  GOOGLE_CLIENT_ID=<OAuth ID>
  GOOGLE_CLIENT_SECRET=<OAuth secret>
  ```
- **Scaling:** CPU/Memory limits configured; auto-scales on traffic spikes

### 6.2 Frontend Deployment (Vercel)
- **Platform:** Vercel
- **Build:** `npm build` or `npm run build`
- **Deployment:** Automatic on master branch push
- **Environment Variables:**
  ```
  REACT_APP_API_BASE_URL=https://api.edtech.com
  REACT_APP_GEMINI_API_KEY=<API key (if client-side)>
  ```

### 6.3 Database Deployment (Neon)
- **Platform:** Neon (PostgreSQL 16 as a Service)
- **Connection Pool:** Neon-managed connection pooler
- **Backups:** Automated daily backups with 30-day retention
- **Monitoring:** Neon dashboard for metrics and alerts

### 6.4 CI/CD Pipeline
- **Trigger:** Push to master branch
- **Tests:** Unit tests (xUnit), integration tests
- **Build:** Docker image creation
- **Deploy:** Automatic Railway and Vercel deployment
- **Monitoring:** GitHub Actions logs and Sentry for error tracking

---

## 7. Disaster Recovery & Backup

### 7.1 Backup Strategy
- **Database:** Neon automated backups (daily, 30-day retention)
- **Application Code:** GitHub repository (version control)
- **Secrets:** Managed via Railway/Vercel secrets vault

### 7.2 Recovery Plan
- **RTO (Recovery Time Objective):** 1 hour
- **RPO (Recovery Point Objective):** 24 hours
- **Procedures:**
  - Database restore from Neon backup
  - Redeployment from GitHub via Railway
  - DNS failover (if multi-region setup)

### 7.3 Monitoring & Alerts
- **Application:** Sentry for exception tracking
- **Database:** Neon monitoring dashboard
- **Infrastructure:** Railway metrics (CPU, memory, network)
- **Uptime:** UptimeRobot or similar for endpoint monitoring

---

## 8. Future Enhancements

### 8.1 Short Term
- [ ] Advanced analytics dashboard (Grafana)
- [ ] Plagiarism detection (Turnitin integration)
- [ ] Mobile app (React Native or Flutter)
- [ ] Video proctoring (basic integration)

### 8.2 Medium Term
- [ ] Real-time notifications (WebSocket/SignalR)
- [ ] Advanced ML-based auto-grading
- [ ] Multi-language support
- [ ] Payment gateway for premium features

### 8.3 Long Term
- [ ] AI-powered adaptive testing (difficulty adjustment)
- [ ] Multi-tenant SaaS platform
- [ ] Offline exam mode with sync
- [ ] Advanced reporting with custom dashboards

---

## 9. Non-Functional Requirements

| Requirement | Target | Implementation |
|-------------|--------|-----------------|
| **Availability** | 99.5% | Railway auto-scaling, Neon managed DB |
| **Response Time** | <2s (p99) | Caching, query optimization, CDN |
| **Throughput** | 1000 req/s | Load balancing, connection pooling |
| **Data Consistency** | Strong | ACID transactions, referential integrity |
| **Security** | SOC 2 compliant | Encryption, HTTPS, audit logs |
| **Scalability** | Horizontal | Stateless API, managed database |
| **Maintainability** | High | Code reviews, automated testing, documentation |

---

## 10. Technology Stack Summary

| Layer | Technology | Rationale |
|-------|-----------|-----------|
| Frontend | Vanilla JS (HTML/CSS) | Lightweight, no dependencies, fast load |
| API | ASP.NET Core 10 | Performance, built-in security, enterprise-ready |
| Database | PostgreSQL 16 (Neon) | ACID compliance, JSON support, serverless-friendly |
| ORM | Dapper | Performance, flexibility, minimal overhead |
| Auth | JWT + OTP | Stateless, scalable, widely supported |
| AI | Google Gemini | State-of-the-art LLM, reliable API |
| Email | SendGrid/SMTP | Reliable delivery, good deliverability |
| Deployment | Railway + Vercel + Neon | Affordable, scalable, low maintenance |

---

## 11. Contact & Support

- **Repository:** https://github.com/Akshit7887/-edtech-web
- **Issues:** GitHub Issues for bug reports and feature requests
- **Documentation:** See README.md and API endpoints in README.md
