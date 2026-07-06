# Security

## Authentication & Authorization

- All API endpoints require authentication by default via `[RequireAuth]` attribute
- Public endpoints are explicitly annotated `[AllowAnonymous]`
- Role enforcement (`teacher`/`student`) uses `[RequireRole]` attribute
- JWT tokens carry `userId`, `role`, and `tokenVersion` claims
- Token version is checked against DB on every request; version mismatch invalidates all existing sessions

## OTP & Login

- OTP codes are never returned in API responses when `env.IsProduction() == true`
- Rate limiting (5 req/min) is applied to auth and OTP endpoints
- OTPs expire after 5 minutes and are single-use

## Exam Security

- Students only see questions assigned to them (via `StudentExamAssignments`)
- Answer keys (`CorrectAnswer`) are never included in student API responses
- Teachers must own an exam to modify or view it
- `userId` is never defaulted to 0 — missing auth results in HTTP 401

## Client-Side Proctoring

The frontend includes basic proctoring features (tab-switch detection, fullscreen enforcement) as a **deterrent**, not a guarantee. These client-side checks can be bypassed by a determined user. Server-side verification (answer patterns, submission timing) should be considered for high-stakes exams.

## Secrets Management

- Google OAuth client secret, JWT secret, Gemini API key, and database connection strings are read from environment variables only
- `appsettings.json` contains placeholder/empty values for all secrets
- See `appsettings.Example.json` for the required configuration shape

## Reporting a Vulnerability

Contact the repository maintainer directly. Do not file a public issue for security vulnerabilities.
