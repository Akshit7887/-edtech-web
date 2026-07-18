using System.Data;
using System.Text;
using Dapper;
using EdTechApi.Data;
using EdTechApi.DTOs;
using EdTechApi.Models;

namespace EdTechApi.Services;

public interface IExamService
{
    Task<ExamListResponse> GetAllExamsAsync(int userId, string role, int page = 1, int limit = 20);
    Task<ExamDetailResponse> GetExamByIdAsync(int examId, int userId, string role, int questionOffset = 0, int questionLimit = 100);
    Task<Exam> CreateExamAsync(CreateExamRequest data, int teacherId);
    Task<Exam> UpdateExamAsync(int examId, UpdateExamRequest data, int teacherId);
    Task DeleteExamAsync(int examId, int teacherId);
    Task<Exam> ActivateExamAsync(int examId, int teacherId);
    Task<string> GenerateDeepLinkAsync(int examId);
    Task<object> ResolveDeepLinkAsync(string deepLinkCode);
    Task<BulkImportResponse> BulkImportStudentsAsync(int examId, string csvText);
    Task<byte[]> ExportResultsPdfAsync(int examId, int teacherId);
    Task<object> GetAttendanceReportAsync(int examId, int teacherId);
    Task<object> PublishQuestionsAsync(int examId, int teacherId);
}

public class ExamService : IExamService
{
    private readonly IDbConnectionFactory _db;
    private readonly IRedisCacheService _cache;
    private readonly ILogger<ExamService> _logger;
    private static readonly ThreadLocal<Random> _random = new(() => new Random());
    private readonly IConfiguration _config;
    private readonly IHubService _hub;

    private static readonly Dictionary<string, string> ValidTransitions = new()
    {
        { "draft", "active" },
        { "active", "closed" }
    };

    public ExamService(IDbConnectionFactory db, IRedisCacheService cache, ILogger<ExamService> logger, IConfiguration config, IHubService hub)
    {
        _db = db;
        _cache = cache;
        _logger = logger;
        _config = config;
        _hub = hub;
    }

    private static string ExamCacheKey(int examId) => $"exam:{examId}";
    private static string ExamListCacheKey(int userId, string role) => $"exams:{role}:{userId}";

    public async Task<ExamListResponse> GetAllExamsAsync(int userId, string role, int page = 1, int limit = 20)
    {
        using var conn = _db.CreateConnection();
        page = Math.Max(1, page);
        limit = Math.Min(100, Math.Max(1, limit));
        var offset = (page - 1) * limit;

        if (role == "teacher")
        {
            var sql = @"SELECT e.*,
                (SELECT COUNT(*) FROM ""Attendance"" a WHERE a.""exam_id"" = e.""id"" AND a.""status"" = 'present') as ""studentsAttended""
                FROM ""Exams"" e WHERE e.""teacher_id"" = @TeacherId
                ORDER BY e.""created_at"" DESC LIMIT @Limit OFFSET @Offset";

            var countSql = "SELECT COUNT(*) FROM \"Exams\" WHERE \"teacher_id\" = @TeacherId";

            var exams = (await conn.QueryAsync<Exam>(sql, new { TeacherId = userId, Limit = limit, Offset = offset })).ToList();
            var total = await conn.ExecuteScalarAsync<int>(countSql, new { TeacherId = userId });

            return new ExamListResponse
            {
                Data = exams.Select(e => new ExamItem
                {
                    Id = e.Id,
                    Title = e.Title,
                    Subject = e.Subject,
                    Status = e.Status,
                    DurationMinutes = e.DurationMinutes,
                    TotalQuestions = e.TotalQuestions,
                    TeacherName = null,
                    CreatedAt = e.CreatedAt
                }).ToList(),
                Pagination = new PaginationInfo { Page = page, Limit = limit, Total = total, TotalPages = (int)Math.Ceiling((double)total / limit) }
            };
        }
        else
        {
            var assignments = (await conn.QueryAsync<StudentExamAssignment>(
                "SELECT * FROM \"StudentExamAssignments\" WHERE \"student_id\" = @StudentId",
                new { StudentId = userId })).ToList();

            if (assignments.Count == 0)
                return new ExamListResponse { Data = new(), Pagination = new PaginationInfo { Page = page, Limit = limit, Total = 0, TotalPages = 0 } };

            var examIds = assignments.Select(a => a.ExamId).ToList();
            var sql = @"SELECT * FROM ""Exams"" WHERE ""id"" = ANY(@Ids) AND ""status"" IN ('active', 'closed')
                ORDER BY ""created_at"" DESC LIMIT @Limit OFFSET @Offset";
            var countSql = "SELECT COUNT(*) FROM \"Exams\" WHERE \"id\" = ANY(@Ids) AND \"status\" IN ('active', 'closed')";

            var exams = (await conn.QueryAsync<Exam>(sql, new { Ids = examIds, Limit = limit, Offset = offset })).ToList();
            var total = await conn.ExecuteScalarAsync<int>(countSql, new { Ids = examIds });

            return new ExamListResponse
            {
                Data = exams.Select(e => new ExamItem
                {
                    Id = e.Id,
                    Title = e.Title,
                    Subject = e.Subject,
                    Status = e.Status,
                    DurationMinutes = e.DurationMinutes,
                    TotalQuestions = e.TotalQuestions,
                    TeacherName = null,
                    CreatedAt = e.CreatedAt
                }).ToList(),
                Pagination = new PaginationInfo { Page = page, Limit = limit, Total = total, TotalPages = (int)Math.Ceiling((double)total / limit) }
            };
        }
    }

    public async Task<ExamDetailResponse> GetExamByIdAsync(int examId, int userId, string role, int questionOffset = 0, int questionLimit = 100)
    {
        var exam = await _cache.GetAsync<Exam>($"exam:{examId}");
        if (exam == null)
        {
            using var conn = _db.CreateReadOnlyConnection();
            exam = await conn.QueryFirstOrDefaultAsync<Exam>(
                "SELECT * FROM \"Exams\" WHERE \"id\" = @Id", new { Id = examId });
            if (exam != null) await _cache.SetAsync($"exam:{examId}", exam, TimeSpan.FromMinutes(5));
        }

        if (exam == null) throw new AppException(404, "Exam not found");

        using var readConn = _db.CreateReadOnlyConnection();

        var allQuestions = (await readConn.QueryAsync<QuestionPool>(
            "SELECT * FROM \"QuestionPool\" WHERE \"exam_id\" = @ExamId ORDER BY \"id\"",
            new { ExamId = examId })).ToList();

        if (role == "teacher")
        {
            if (exam.TeacherId != userId)
                throw new AppException(403, "Access denied");

            var paginatedForTeacher = allQuestions.Skip(questionOffset).Take(questionLimit).ToList();

            return new ExamDetailResponse
            {
                Id = exam.Id,
                Title = exam.Title,
                Subject = exam.Subject,
                SyllabusText = exam.SyllabusText,
                DurationMinutes = exam.DurationMinutes,
                TotalQuestions = exam.TotalQuestions,
                DeepLinkCode = exam.DeepLinkCode,
                Status = exam.Status,
                ScheduledAt = exam.ScheduledAt,
                ScheduledEndAt = exam.ScheduledEndAt,
                AllowReattempt = exam.AllowReattempt,
                Questions = paginatedForTeacher.Select(q => new QuestionItem
                {
                    Id = q.Id,
                    QuestionText = q.QuestionText,
                    OptionA = q.OptionA,
                    OptionB = q.OptionB,
                    OptionC = q.OptionC,
                    OptionD = q.OptionD,
                    CorrectAnswer = q.CorrectAnswer,
                    Difficulty = q.Difficulty,
                    Points = q.Points,
                    Status = q.Status
                }).ToList(),
                TotalQuestionsCount = allQuestions.Count,
                QuestionOffset = questionOffset,
                HasMore = (questionOffset + questionLimit) < allQuestions.Count,
                CreatedAt = exam.CreatedAt
            };
        }

        if (role == "student")
        {
            var assignment = await readConn.QueryFirstOrDefaultAsync<StudentExamAssignment>(
                "SELECT * FROM \"StudentExamAssignments\" WHERE \"student_id\" = @StudentId AND \"exam_id\" = @ExamId",
                new { StudentId = userId, ExamId = examId });
            if (assignment == null) throw new AppException(403, "You are not assigned to this exam");

            var assignedIds = assignment.QuestionIds ?? new();
            var assignedQuestions = allQuestions.Where(q => assignedIds.Contains(q.Id)).ToList();
            var paginated = assignedQuestions.Skip(questionOffset).Take(questionLimit).ToList();

            var attendance = await readConn.QueryFirstOrDefaultAsync<Attendance>(
                "SELECT * FROM \"Attendance\" WHERE \"student_id\" = @StudentId AND \"exam_id\" = @ExamId",
                new { StudentId = userId, ExamId = examId });

            return new ExamDetailResponse
            {
                Id = exam.Id,
                Title = exam.Title,
                Subject = exam.Subject,
                SyllabusText = exam.SyllabusText,
                DurationMinutes = exam.DurationMinutes,
                TotalQuestions = exam.TotalQuestions,
                DeepLinkCode = exam.DeepLinkCode,
                Status = exam.Status,
                ScheduledAt = exam.ScheduledAt,
                ScheduledEndAt = exam.ScheduledEndAt,
                AllowReattempt = exam.AllowReattempt,
                Questions = paginated.Select(q => new QuestionItem
                {
                    Id = q.Id,
                    QuestionText = q.QuestionText,
                    OptionA = q.OptionA,
                    OptionB = q.OptionB,
                    OptionC = q.OptionC,
                    OptionD = q.OptionD,
                    Difficulty = q.Difficulty,
                    Points = q.Points,
                    Status = q.Status
                }).ToList(),
                TotalQuestionsCount = assignedQuestions.Count,
                QuestionOffset = questionOffset,
                HasMore = (questionOffset + questionLimit) < assignedQuestions.Count,
                Attendance = attendance?.Status,
                CreatedAt = exam.CreatedAt
            };
        }

        throw new AppException(403, "Access denied");
    }

    public async Task<Exam> CreateExamAsync(CreateExamRequest data, int teacherId)
    {
        using var conn = _db.CreateConnection();
        var deepLinkCode = Guid.NewGuid().ToString("N")[..8];
        var now = DateTime.UtcNow;

        var exam = await conn.QuerySingleAsync<Exam>(
            @"INSERT INTO ""Exams"" (""teacher_id"", ""title"", ""subject"", ""syllabus_text"", ""duration_minutes"", ""total_questions"", ""deep_link_code"", ""status"", ""created_at"", ""updated_at"")
              VALUES (@TeacherId, @Title, @Subject, @SyllabusText, @DurationMinutes, @TotalQuestions, @DeepLinkCode, @Status, @CreatedAt, @UpdatedAt) RETURNING *",
            new
            {
                TeacherId = teacherId,
                Title = data.Title,
                Subject = data.Subject,
                SyllabusText = data.SyllabusText,
                DurationMinutes = data.DurationMinutes > 0 ? data.DurationMinutes : 30,
                TotalQuestions = data.TotalQuestions > 0 ? data.TotalQuestions : 10,
                DeepLinkCode = deepLinkCode,
                Status = data.Status ?? "draft",
                CreatedAt = now,
                UpdatedAt = now
            });

        await _hub.NotifyTeacherDashboard(teacherId, "ExamCreated", new { exam.Id, exam.Title, exam.Subject, exam.Status });
        await _cache.RemoveAsync(ExamListCacheKey(teacherId, "teacher"));
        return exam;
    }

    public async Task<Exam> UpdateExamAsync(int examId, UpdateExamRequest data, int teacherId)
    {
        using var conn = _db.CreateConnection();

        var exam = await conn.QueryFirstOrDefaultAsync<Exam>(
            "SELECT * FROM \"Exams\" WHERE \"id\" = @Id", new { Id = examId });
        if (exam == null) throw new AppException(404, "Exam not found");
        if (exam.TeacherId != teacherId) throw new AppException(403, "Access denied");

        if (!string.IsNullOrEmpty(data.Status) && data.Status != exam.Status)
        {
            if (!ValidTransitions.TryGetValue(exam.Status, out var expected) || data.Status != expected)
                throw new AppException(400, $"Invalid status transition: {exam.Status} -> {data.Status}. Only allowed: {exam.Status} -> {expected}");
        }

        var setClauses = new List<string>();
        var parameters = new DynamicParameters();
        parameters.Add("Id", examId);

        if (!string.IsNullOrEmpty(data.Title)) { setClauses.Add("\"title\" = @Title"); parameters.Add("Title", data.Title); }
        if (!string.IsNullOrEmpty(data.Subject)) { setClauses.Add("\"subject\" = @Subject"); parameters.Add("Subject", data.Subject); }
        if (data.SyllabusText != null) { setClauses.Add("\"syllabus_text\" = @SyllabusText"); parameters.Add("SyllabusText", data.SyllabusText); }
        if (data.DurationMinutes.HasValue) { setClauses.Add("\"duration_minutes\" = @DurationMinutes"); parameters.Add("DurationMinutes", data.DurationMinutes); }
        if (data.TotalQuestions.HasValue) { setClauses.Add("\"total_questions\" = @TotalQuestions"); parameters.Add("TotalQuestions", data.TotalQuestions); }
        if (!string.IsNullOrEmpty(data.Status)) { setClauses.Add("\"status\" = @Status"); parameters.Add("Status", data.Status); }

        if (setClauses.Count == 0) return exam;

        setClauses.Add("\"updated_at\" = @Now");
        parameters.Add("Now", DateTime.UtcNow);

        var sql = $"UPDATE \"Exams\" SET {string.Join(", ", setClauses)} WHERE \"id\" = @Id RETURNING *";
        exam = await conn.QuerySingleAsync<Exam>(sql, parameters);

        if (!string.IsNullOrEmpty(data.Status))
        {
            await _hub.NotifyTeacherDashboard(teacherId, "ExamStatusChanged", new { exam.Id, exam.Title, exam.Status });
            await _hub.NotifyExamGroup(examId, "ExamStatusChanged", new { exam.Id, exam.Status });
        }
        await _cache.RemoveAsync(ExamCacheKey(examId));
        await _cache.RemoveAsync(ExamListCacheKey(teacherId, "teacher"));
        return exam;
    }

    public async Task DeleteExamAsync(int examId, int teacherId)
    {
        using var conn = _db.CreateConnection();

        var exam = await conn.QueryFirstOrDefaultAsync<Exam>(
            "SELECT * FROM \"Exams\" WHERE \"id\" = @Id", new { Id = examId });
        if (exam == null) throw new AppException(404, "Exam not found");
        if (exam.TeacherId != teacherId) throw new AppException(403, "Access denied");
        if (exam.Status == "active") throw new AppException(400, "Cannot delete active exam");

        await conn.ExecuteAsync("DELETE FROM \"QuestionPool\" WHERE \"exam_id\" = @ExamId", new { ExamId = examId });
        await conn.ExecuteAsync("DELETE FROM \"StudentExamAssignments\" WHERE \"exam_id\" = @ExamId", new { ExamId = examId });
        await conn.ExecuteAsync("DELETE FROM \"ExamSessions\" WHERE \"exam_id\" = @ExamId", new { ExamId = examId });
        await conn.ExecuteAsync("DELETE FROM \"Attendance\" WHERE \"exam_id\" = @ExamId", new { ExamId = examId });
        await conn.ExecuteAsync("DELETE FROM \"Exams\" WHERE \"id\" = @Id", new { Id = examId });

        await _cache.RemoveAsync(ExamCacheKey(examId));
        await _cache.RemoveAsync(ExamListCacheKey(teacherId, "teacher"));
        await _hub.NotifyTeacherDashboard(teacherId, "ExamDeleted", new { examId = exam.Id, title = exam.Title });
    }

    public async Task<Exam> ActivateExamAsync(int examId, int teacherId)
    {
        using var conn = _db.CreateConnection();

        var exam = await conn.QueryFirstOrDefaultAsync<Exam>(
            "SELECT * FROM \"Exams\" WHERE \"id\" = @Id", new { Id = examId });
        if (exam == null) throw new AppException(404, "Exam not found");
        if (exam.TeacherId != teacherId) throw new AppException(403, "Access denied");
        if (exam.Status != "draft") throw new AppException(400, $"Cannot activate exam with status \"{exam.Status}\". Only draft exams can be activated.");

        await conn.ExecuteAsync(
            "UPDATE \"Exams\" SET \"status\" = 'closed' WHERE \"teacher_id\" = @TeacherId AND \"status\" = 'active'",
            new { TeacherId = teacherId });

        exam = await conn.QuerySingleAsync<Exam>(
            "UPDATE \"Exams\" SET \"status\" = 'active', \"updated_at\" = @Now WHERE \"id\" = @Id RETURNING *",
            new { Now = DateTime.UtcNow, Id = examId });

        await _hub.NotifyTeacherDashboard(teacherId, "ExamStatusChanged", new { exam.Id, exam.Title, exam.Status });
        await _hub.NotifyExamGroup(examId, "ExamActivated", new { exam.Id, exam.Title });
        await _cache.RemoveAsync(ExamCacheKey(examId));
        await _cache.RemoveAsync(ExamListCacheKey(teacherId, "teacher"));
        return exam;
    }

    public async Task<string> GenerateDeepLinkAsync(int examId)
    {
        using var conn = _db.CreateConnection();
        var exam = await conn.QueryFirstOrDefaultAsync<Exam>(
            "SELECT * FROM \"Exams\" WHERE \"id\" = @Id", new { Id = examId });
        if (exam == null) throw new AppException(404, "Exam not found");

        var prefix = _config?.GetValue<string>("App:DeepLinkPrefix") ?? "edtech-exam://exam/";
        return $"{prefix}{exam.DeepLinkCode}";
    }

    public async Task<object> ResolveDeepLinkAsync(string deepLinkCode)
    {
        using var conn = _db.CreateConnection();
        var exam = await conn.QueryFirstOrDefaultAsync<Exam>(
            "SELECT * FROM \"Exams\" WHERE \"deep_link_code\" = @Code", new { Code = deepLinkCode });
        if (exam == null) throw new AppException(404, "Invalid deep link code. Exam not found.");
        if (exam.Status != "active") throw new AppException(400, "This exam is not currently active.");

        return new { id = exam.Id, title = exam.Title, subject = exam.Subject, durationMinutes = exam.DurationMinutes, totalQuestions = exam.TotalQuestions, status = exam.Status };
    }

    public async Task<BulkImportResponse> BulkImportStudentsAsync(int examId, string csvText)
    {
        using var conn = _db.CreateConnection();

        var exam = await conn.QueryFirstOrDefaultAsync<Exam>(
            "SELECT * FROM \"Exams\" WHERE \"id\" = @Id", new { Id = examId });
        if (exam == null) throw new AppException(404, "Exam not found");

        var allPool = (await conn.QueryAsync<QuestionPool>(
            "SELECT * FROM \"QuestionPool\" WHERE \"exam_id\" = @ExamId AND \"status\" = 'published'",
            new { ExamId = examId })).ToList();

        if (allPool.Count < exam.TotalQuestions)
            throw new AppException(400, "Not enough published questions (" + allPool.Count + ") for exam (" + exam.TotalQuestions + ").");

        var lines = csvText.Split('\n').Where(l => !string.IsNullOrWhiteSpace(l)).ToList();
        if (lines.Count == 0) return new BulkImportResponse();

        var header = lines[0].ToLower().Split(',').Select(h => h.Trim()).ToList();
        var hasName = header.Any(h => h.Contains("name"));
        var hasPhone = header.Any(h => h.Contains("phone"));
        var hasEmail = header.Any(h => h.Contains("email"));
        var dataStart = hasName || hasPhone || hasEmail ? 1 : 0;

        var imported = 0;
        var errors = new List<string>();

        for (int i = dataStart; i < lines.Count; i++)
        {
            var cols = lines[i].Split(',').Select(c => c.Trim()).ToList();
            var name = hasName ? cols[header.FindIndex(h => h.Contains("name"))] : (cols.Count > 0 ? cols[0] : "");
            var phone = hasPhone && header.FindIndex(h => h.Contains("phone")) >= 0 ? cols[header.FindIndex(h => h.Contains("phone"))] : (cols.Count > 1 ? cols[1] : "");
            var email = hasEmail && header.FindIndex(h => h.Contains("email")) >= 0 ? cols[header.FindIndex(h => h.Contains("email"))] : (cols.Count > 2 ? cols[2] : "");

            if (string.IsNullOrEmpty(name)) { errors.Add("Row " + (i + 1) + ": missing name"); continue; }

            try
            {
                var identifier = email ?? phone;
                if (string.IsNullOrEmpty(identifier)) { errors.Add("Row " + (i + 1) + ": missing phone or email"); continue; }

                var student = identifier.Contains('@')
                    ? await conn.QueryFirstOrDefaultAsync<User>("SELECT * FROM \"Users\" WHERE \"email\" = @Email", new { Email = identifier })
                    : await conn.QueryFirstOrDefaultAsync<User>("SELECT * FROM \"Users\" WHERE \"phone\" = @Phone", new { Phone = identifier });

                if (student == null)
                {
                    var hash = BCrypt.Net.BCrypt.HashPassword("changeme123");
                    var now = DateTime.UtcNow;
                    student = await conn.QuerySingleAsync<User>(
                        "INSERT INTO \"Users\" (\"name\", \"role\", \"password_hash\", \"email\", \"phone\", \"created_at\", \"updated_at\") VALUES (@Name, 'student', @Hash, @Email, @Phone, @CreatedAt, @UpdatedAt) RETURNING *",
                        new
                        {
                            Name = name,
                            Hash = hash,
                            Email = identifier.Contains('@') ? identifier : null,
                            Phone = !identifier.Contains('@') ? identifier : null,
                            CreatedAt = now,
                            UpdatedAt = now
                        });
                }

                var shuffled = FisherYatesShuffle(allPool);
                var qIds = shuffled.Take(exam.TotalQuestions).Select(q => q.Id).ToList();
                var now2 = DateTime.UtcNow;

                await conn.ExecuteAsync(
                    "INSERT INTO \"StudentExamAssignments\" (\"student_id\", \"exam_id\", \"question_ids\", \"created_at\", \"updated_at\") VALUES (@StudentId, @ExamId, @QuestionIds, @Now, @Now) ON CONFLICT (\"student_id\", \"exam_id\") DO UPDATE SET \"question_ids\" = @QuestionIds, \"updated_at\" = @Now",
                    new { StudentId = student.Id, ExamId = examId, QuestionIds = qIds, Now = now2 });
                imported++;
            }
            catch (Exception ex)
            {
                errors.Add("Row " + (i + 1) + ": " + ex.Message);
            }
        }

        return new BulkImportResponse { Imported = imported, Errors = errors };
    }

    public async Task<byte[]> ExportResultsPdfAsync(int examId, int teacherId)
    {
        using var conn = _db.CreateConnection();

        var exam = await conn.QueryFirstOrDefaultAsync<Exam>(
            "SELECT * FROM \"Exams\" WHERE \"id\" = @Id", new { Id = examId });
        if (exam == null || exam.TeacherId != teacherId)
            throw new AppException(404, "Exam not found or access denied");

        var sessions = (await conn.QueryAsync<ExamSession>(
            "SELECT * FROM \"ExamSessions\" WHERE \"exam_id\" = @ExamId ORDER BY \"created_at\" DESC",
            new { ExamId = examId })).ToList();

        var studentIds = sessions.Select(s => s.StudentId).Distinct().ToList();
        var students = (await conn.QueryAsync<User>(
            "SELECT \"id\", \"name\" FROM \"Users\" WHERE \"id\" = ANY(@Ids)",
            new { Ids = studentIds })).ToDictionary(s => s.Id, s => s.Name ?? "Unknown");

        var completed = sessions.Where(s => s.Status == "completed").ToList();
        var passThreshold = exam.TotalQuestions * 0.4;
        var passCount = completed.Count(s => s.Score >= (decimal)passThreshold);

        var html = new System.Text.StringBuilder();
        html.Append("<html><head><meta charset='utf-8'><title>Exam Results</title>");
        html.Append("<style>body{font-family:Arial,sans-serif;margin:40px}h1{color:#333}table{width:100%;border-collapse:collapse;margin-top:20px}th,td{padding:10px;text-align:left;border-bottom:1px solid #ddd}th{background:#f5f5f5}.pass{color:green}.fail{color:red}</style></head><body>");
        html.AppendFormat("<h1>{0}</h1><p>Subject: {1} | Total Students: {2} | Passed: {3} | Failed: {4}</p>",
            System.Net.WebUtility.HtmlEncode(exam.Title ?? "Exam"),
            System.Net.WebUtility.HtmlEncode(exam.Subject ?? ""),
            completed.Count, passCount, completed.Count - passCount);
        html.Append("<table><thead><tr><th>Student</th><th>Score</th><th>Total</th><th>Status</th><th>Submitted</th></tr></thead><tbody>");
        foreach (var s in sessions)
        {
            var name = students.ContainsKey(s.StudentId) ? students[s.StudentId] : "Unknown";
            var cls = s.Status == "completed" ? (s.Score >= (decimal)passThreshold ? "pass" : "fail") : "";
            html.AppendFormat("<tr class='{0}'><td>{1}</td><td>{2}</td><td>{3}</td><td>{4}</td><td>{5}</td></tr>",
                cls,
                System.Net.WebUtility.HtmlEncode(name),
                s.Score, s.TotalQuestions,
                System.Net.WebUtility.HtmlEncode(s.Status ?? ""),
                s.SubmittedAt?.ToString("g") ?? "-");
        }
        html.Append("</tbody></table></body></html>");

        return Encoding.UTF8.GetBytes(html.ToString());
    }

    public async Task<object> GetAttendanceReportAsync(int examId, int teacherId)
    {
        using var conn = _db.CreateConnection();

        var exam = await conn.QueryFirstOrDefaultAsync<Exam>(
            "SELECT * FROM \"Exams\" WHERE \"id\" = @Id", new { Id = examId });
        if (exam == null || exam.TeacherId != teacherId)
            throw new AppException(404, "Exam not found or access denied");

        var attendance = (await conn.QueryAsync<Attendance>(
            "SELECT a.*, u.\"name\" as \"StudentName\" FROM \"Attendance\" a JOIN \"Users\" u ON a.\"student_id\" = u.\"id\" WHERE a.\"exam_id\" = @ExamId ORDER BY a.\"created_at\" ASC",
            new { ExamId = examId })).ToList();

        var present = attendance.Count(a => a.Status == "present");
        var absent = attendance.Count(a => a.Status == "absent");

        return new
        {
            examId,
            examTitle = exam.Title,
            present,
            absent,
            total = present + absent,
            students = attendance.Select(a => new
            {
                studentId = a.StudentId,
                studentName = a.StudentName,
                status = a.Status,
                markedAt = a.MarkedAt
            })
        };
    }

    public async Task<object> PublishQuestionsAsync(int examId, int teacherId)
    {
        using var conn = _db.CreateConnection();

        var exam = await conn.QueryFirstOrDefaultAsync<Exam>(
            "SELECT * FROM \"Exams\" WHERE \"id\" = @Id", new { Id = examId });
        if (exam == null || exam.TeacherId != teacherId)
            throw new AppException(404, "Exam not found or access denied");

        await conn.ExecuteAsync(
            "UPDATE \"QuestionPool\" SET \"status\" = 'published' WHERE \"exam_id\" = @ExamId AND \"status\" = 'pending'",
            new { ExamId = examId });

        var publishedCount = await conn.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM \"QuestionPool\" WHERE \"exam_id\" = @ExamId AND \"status\" = 'published'",
            new { ExamId = examId });

        await conn.ExecuteAsync(
            "UPDATE \"Exams\" SET \"total_questions\" = @Count, \"updated_at\" = @Now WHERE \"id\" = @Id",
            new { Count = publishedCount, Now = DateTime.UtcNow, Id = examId });

        await _cache.RemoveAsync(ExamCacheKey(examId));
        await _cache.RemoveAsync(ExamListCacheKey(teacherId, "teacher"));

        return new { publishedCount };
    }

    private static List<T> FisherYatesShuffle<T>(List<T> list)
    {
        var shuffled = new List<T>(list);
        for (int i = shuffled.Count - 1; i > 0; i--)
        {
            var j = _random.Value!.Next(i + 1);
            (shuffled[i], shuffled[j]) = (shuffled[j], shuffled[i]);
        }
        return shuffled;
    }
}
