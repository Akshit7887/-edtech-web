using System.Data;
using Dapper;
using EdTechApi.Data;
using EdTechApi.DTOs;
using EdTechApi.Models;

namespace EdTechApi.Services;

public interface ITeacherService
{
    Task<object> GetAllStudentsAsync(int teacherId, int page = 1, int limit = 50);
    Task<object> CreateStudentAsync(int teacherId, string name, string email);
    Task<object> GetStudentDetailAsync(int studentId);
    Task<object?> SearchStudentBySidAsync(string studentId);
    Task DeleteStudentAsync(int studentId);
    Task<object> GetQuestionBankAsync(int examId, int teacherId);
    Task<QuestionPool> AddQuestionAsync(int examId, int teacherId, QuestionPool data);
    Task<QuestionPool> UpdateQuestionAsync(int questionId, int teacherId, QuestionPool data);
    Task DeleteQuestionAsync(int questionId, int teacherId);
    Task<ClassEntity> CreateClassAsync(int teacherId, CreateClassRequest data);
    Task<List<ClassListItem>> GetClassesAsync(int teacherId);
    Task<ClassDetailResponse> GetClassDetailAsync(int classId, int teacherId);
    Task<object> AddStudentsToClassAsync(int classId, int teacherId, List<int> studentIds);
    Task RemoveStudentFromClassAsync(int classId, int teacherId, int studentId);
    Task DeleteClassAsync(int classId, int teacherId);
    Task<object> SendAnnouncementAsync(int teacherId, AnnouncementRequest data);
    Task<Exam> ScheduleExamAsync(int examId, int teacherId, string scheduledAt);
    Task<List<ParentContactItem>> GetParentContactsAsync(int teacherId);
    Task<object> CreateOrUpdateParentContactAsync(int studentId, int teacherId, ParentContactRequest data);
    Task DeleteParentContactAsync(int studentId, int teacherId);
    Task<List<ParentNotification>> GetParentReportHistoryAsync(int examId, int teacherId);
}

public class TeacherService : ITeacherService
{
    private readonly IDbConnectionFactory _db;
    private readonly IHubService _hub;

    public TeacherService(IDbConnectionFactory db, IHubService hub)
    {
        _db = db;
        _hub = hub;
    }

    public async Task<object> GetAllStudentsAsync(int teacherId, int page = 1, int limit = 50)
    {
        using var conn = _db.CreateConnection();
        page = Math.Max(1, page);
        limit = Math.Min(10000, Math.Max(1, limit));
        var offset = (page - 1) * limit;

        var total = await conn.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM \"Users\" WHERE \"role\" = 'student'");

        var students = (await conn.QueryAsync<User>(
            "SELECT * FROM \"Users\" WHERE \"role\" = 'student' ORDER BY \"name\" ASC LIMIT @Limit OFFSET @Offset",
            new { Limit = limit, Offset = offset })).ToList();

        if (students.Count == 0)
            return new { data = new List<object>(), pagination = new { page, limit, total, totalPages = (int)Math.Ceiling((double)total / limit) } };

        var studentIds = students.Select(s => s.Id).ToList();
        var sessions = (await conn.QueryAsync<ExamSession>(
            "SELECT * FROM \"ExamSessions\" WHERE \"student_id\" = ANY(@Ids) ORDER BY \"student_id\" ASC",
            new { Ids = studentIds })).ToList();

        var examIds = sessions.Select(s => s.ExamId).Distinct().ToList();
        var exams = examIds.Any()
            ? (await conn.QueryAsync<Exam>("SELECT * FROM \"Exams\" WHERE \"id\" = ANY(@Ids)", new { Ids = examIds }))
                .ToDictionary(e => e.Id)
            : new Dictionary<int, Exam>();

        var sessionsByStudent = sessions.GroupBy(s => s.StudentId).ToDictionary(g => g.Key, g => g.ToList());

        var data = students.Select(s =>
        {
            var sess = sessionsByStudent.ContainsKey(s.Id) ? sessionsByStudent[s.Id] : new List<ExamSession>();
            var completed = sess.Where(x => x.Status == "completed").ToList();
            var totalScore = completed.Sum(x => (double)x.Score);
            var maxScore = completed.Sum(x => (double)(exams.ContainsKey(x.ExamId) ? exams[x.ExamId].TotalQuestions : x.TotalQuestions));

            return new
            {
                id = s.Id,
                name = s.Name,
                email = s.Email,
                phone = s.Phone,
                student_id = s.StudentId,
                registeredAt = s.CreatedAt,
                totalExams = sess.Count,
                completedExams = completed.Count,
                totalScore,
                maxScore,
                averageScore = maxScore > 0 ? (int)Math.Round(totalScore / maxScore * 100) : 0
            };
        }).ToList();

        return new { data, pagination = new { page, limit, total, totalPages = (int)Math.Ceiling((double)total / limit) } };
    }

    public async Task<object> CreateStudentAsync(int teacherId, string name, string email)
    {
        using var conn = _db.CreateConnection();

        var existing = await conn.QueryFirstOrDefaultAsync<User>(
            "SELECT * FROM \"Users\" WHERE \"email\" = @Email", new { Email = email });
        if (existing != null)
            throw new AppException(409, "A user with this email already exists");

        var hash = BCrypt.Net.BCrypt.HashPassword(Guid.NewGuid().ToString());
        var now = DateTime.UtcNow;
        var user = await conn.QuerySingleAsync<User>(
            @"INSERT INTO ""Users"" (""name"", ""role"", ""email"", ""password_hash"", ""created_at"", ""updated_at"")
              VALUES (@Name, 'student', @Email, @PasswordHash, @Now, @Now) RETURNING *",
            new { Name = name, Email = email, PasswordHash = hash, Now = now });

        // Assign a 10-digit student_id
        var random = new Random();
        string sid;
        do {
            sid = random.Next(0, 1000000000).ToString("D10");
        } while (await conn.QueryFirstOrDefaultAsync<string>(
            "SELECT 1 FROM \"Users\" WHERE \"student_id\" = @Sid", new { Sid = sid }) != null);
        user = await conn.QuerySingleAsync<User>(
            "UPDATE \"Users\" SET \"student_id\" = @Sid WHERE \"id\" = @Id RETURNING *",
            new { Sid = sid, Id = user.Id });

        await _hub.NotifyTeacherDashboard(teacherId, "StudentCreated", new { id = user.Id, name = user.Name, email = user.Email });

        return new { id = user.Id, name = user.Name, email = user.Email, student_id = user.StudentId, role = user.Role, created_at = user.CreatedAt };
    }

    public async Task<object> GetStudentDetailAsync(int studentId)
    {
        using var conn = _db.CreateConnection();

        var student = await conn.QueryFirstOrDefaultAsync<User>(
            "SELECT * FROM \"Users\" WHERE \"id\" = @Id", new { Id = studentId });
        if (student == null || student.Role != "student")
            throw new AppException(404, "Student not found");

        var sessions = (await conn.QueryAsync<ExamSession>(
            "SELECT * FROM \"ExamSessions\" WHERE \"student_id\" = @StudentId ORDER BY \"created_at\" DESC",
            new { StudentId = studentId })).ToList();

        var examIds = sessions.Select(s => s.ExamId).Distinct().ToList();
        var exams = examIds.Any()
            ? (await conn.QueryAsync<Exam>("SELECT * FROM \"Exams\" WHERE \"id\" = ANY(@Ids)", new { Ids = examIds }))
                .ToDictionary(e => e.Id)
            : new Dictionary<int, Exam>();

        var completed = sessions.Where(s => s.Status == "completed").ToList();
        var totalScore = completed.Sum(s => (double)s.Score);
        var maxScore = completed.Sum(s => (double)(exams.ContainsKey(s.ExamId) ? exams[s.ExamId].TotalQuestions : s.TotalQuestions));

        return new
        {
            id = student.Id,
            name = student.Name,
            email = student.Email,
            phone = student.Phone,
            registeredAt = student.CreatedAt,
            totalExams = sessions.Count,
            completedExams = completed.Count,
            disqualifiedExams = sessions.Count(s => s.Status == "disqualified"),
            totalScore,
            maxScore,
            averageScore = maxScore > 0 ? (int)Math.Round(totalScore / maxScore * 100) : 0,
            examHistory = sessions.Select(s => new
            {
                examId = s.ExamId,
                examTitle = exams.ContainsKey(s.ExamId) ? exams[s.ExamId].Title : "Unknown",
                subject = exams.ContainsKey(s.ExamId) ? exams[s.ExamId].Subject : "",
                score = s.Score,
                totalQuestions = s.TotalQuestions,
                status = s.Status,
                submittedAt = s.SubmittedAt,
                timeUsed = s.StartedAt.HasValue && s.SubmittedAt.HasValue
                    ? (int?)Math.Round((s.SubmittedAt.Value - s.StartedAt.Value).TotalMinutes)
                    : null
            })
        };
    }

    public async Task<object> GetQuestionBankAsync(int examId, int teacherId)
    {
        using var conn = _db.CreateConnection();

        var exam = await conn.QueryFirstOrDefaultAsync<Exam>(
            "SELECT * FROM \"Exams\" WHERE \"id\" = @Id", new { Id = examId });
        if (exam == null || exam.TeacherId != teacherId)
            throw new AppException(404, "Exam not found or access denied");

        var questions = (await conn.QueryAsync<QuestionPool>(
            "SELECT * FROM \"QuestionPool\" WHERE \"exam_id\" = @ExamId ORDER BY \"created_at\" ASC",
            new { ExamId = examId })).ToList();

        return new { examId = exam.Id, examTitle = exam.Title, totalQuestions = questions.Count, questions };
    }

    public async Task<QuestionPool> AddQuestionAsync(int examId, int teacherId, QuestionPool data)
    {
        using var conn = _db.CreateConnection();

        var exam = await conn.QueryFirstOrDefaultAsync<Exam>(
            "SELECT * FROM \"Exams\" WHERE \"id\" = @Id", new { Id = examId });
        if (exam == null || exam.TeacherId != teacherId)
            throw new AppException(404, "Exam not found or access denied");

        if (string.IsNullOrEmpty(data.QuestionText) || string.IsNullOrEmpty(data.OptionA) || string.IsNullOrEmpty(data.OptionB) || string.IsNullOrEmpty(data.CorrectAnswer))
            throw new AppException(400, "Missing required fields: questionText, optionA, optionB, correctAnswer");

        if (!Letters.Contains(data.CorrectAnswer))
            throw new AppException(400, "correctAnswer must be A, B, C, or D");

        var now = DateTime.UtcNow;
        var question = await conn.QuerySingleAsync<QuestionPool>(
            @"INSERT INTO ""QuestionPool"" (""exam_id"", ""question_text"", ""option_a"", ""option_b"", ""option_c"", ""option_d"", ""correct_answer"", ""difficulty"", ""points"", ""status"", ""created_at"", ""updated_at"")
              VALUES (@ExamId, @QuestionText, @OptionA, @OptionB, @OptionC, @OptionD, @CorrectAnswer, @Difficulty, @Points, @Status, @CreatedAt, @UpdatedAt) RETURNING *",
            new
            {
                ExamId = examId,
                QuestionText = data.QuestionText,
                OptionA = data.OptionA,
                OptionB = data.OptionB,
                OptionC = data.OptionC,
                OptionD = data.OptionD,
                CorrectAnswer = data.CorrectAnswer,
                Difficulty = data.Difficulty ?? "medium",
                Points = data.Points > 0 ? data.Points : 1,
                Status = data.Status ?? "pending",
                CreatedAt = now,
                UpdatedAt = now
            });

        return question;
    }

    public async Task<QuestionPool> UpdateQuestionAsync(int questionId, int teacherId, QuestionPool data)
    {
        using var conn = _db.CreateConnection();

        var question = await conn.QueryFirstOrDefaultAsync<QuestionPool>(
            "SELECT * FROM \"QuestionPool\" WHERE \"id\" = @Id", new { Id = questionId });
        if (question == null) throw new AppException(404, "Question not found");

        var exam = await conn.QueryFirstOrDefaultAsync<Exam>(
            "SELECT * FROM \"Exams\" WHERE \"id\" = @Id", new { Id = question.ExamId });
        if (exam == null || exam.TeacherId != teacherId)
            throw new AppException(403, "Access denied");

        var setClauses = new List<string>();
        var parameters = new DynamicParameters();
        parameters.Add("Id", questionId);

        if (!string.IsNullOrEmpty(data.QuestionText)) { setClauses.Add("\"question_text\" = @QuestionText"); parameters.Add("QuestionText", data.QuestionText); }
        if (!string.IsNullOrEmpty(data.OptionA)) { setClauses.Add("\"option_a\" = @OptionA"); parameters.Add("OptionA", data.OptionA); }
        if (!string.IsNullOrEmpty(data.OptionB)) { setClauses.Add("\"option_b\" = @OptionB"); parameters.Add("OptionB", data.OptionB); }
        if (data.OptionC != null) { setClauses.Add("\"option_c\" = @OptionC"); parameters.Add("OptionC", data.OptionC); }
        if (data.OptionD != null) { setClauses.Add("\"option_d\" = @OptionD"); parameters.Add("OptionD", data.OptionD); }
        if (!string.IsNullOrEmpty(data.CorrectAnswer)) { setClauses.Add("\"correct_answer\" = @CorrectAnswer"); parameters.Add("CorrectAnswer", data.CorrectAnswer); }
        if (!string.IsNullOrEmpty(data.Difficulty)) { setClauses.Add("\"difficulty\" = @Difficulty"); parameters.Add("Difficulty", data.Difficulty); }
        if (data.Points > 0) { setClauses.Add("\"points\" = @Points"); parameters.Add("Points", data.Points); }
        if (!string.IsNullOrEmpty(data.Status)) { setClauses.Add("\"status\" = @Status"); parameters.Add("Status", data.Status); }

        if (setClauses.Count == 0) return question;

        setClauses.Add("\"updated_at\" = @Now");
        parameters.Add("Now", DateTime.UtcNow);

        var sql = $"UPDATE \"QuestionPool\" SET {string.Join(", ", setClauses)} WHERE \"id\" = @Id RETURNING *";
        return await conn.QuerySingleAsync<QuestionPool>(sql, parameters);
    }

    public async Task DeleteQuestionAsync(int questionId, int teacherId)
    {
        using var conn = _db.CreateConnection();

        var question = await conn.QueryFirstOrDefaultAsync<QuestionPool>(
            "SELECT * FROM \"QuestionPool\" WHERE \"id\" = @Id", new { Id = questionId });
        if (question == null) throw new AppException(404, "Question not found");

        var exam = await conn.QueryFirstOrDefaultAsync<Exam>(
            "SELECT * FROM \"Exams\" WHERE \"id\" = @Id", new { Id = question.ExamId });
        if (exam == null || exam.TeacherId != teacherId)
            throw new AppException(403, "Access denied");

        await conn.ExecuteAsync("DELETE FROM \"QuestionPool\" WHERE \"id\" = @Id", new { Id = questionId });
    }

    public async Task<ClassEntity> CreateClassAsync(int teacherId, CreateClassRequest data)
    {
        using var conn = _db.CreateConnection();

        if (string.IsNullOrEmpty(data.Name)) throw new AppException(400, "Class name is required");

        var now = DateTime.UtcNow;
        var cls = await conn.QuerySingleAsync<ClassEntity>(
            @"INSERT INTO ""Classes"" (""teacher_id"", ""name"", ""subject"", ""description"", ""created_at"", ""updated_at"")
              VALUES (@TeacherId, @Name, @Subject, @Description, @CreatedAt, @UpdatedAt) RETURNING *",
            new { TeacherId = teacherId, Name = data.Name, Subject = data.Subject ?? "", Description = data.Description ?? "", CreatedAt = now, UpdatedAt = now });

        await _hub.NotifyTeacherDashboard(teacherId, "ClassCreated", new { id = cls.Id, name = cls.Name });
        return cls;
    }

    public async Task<List<ClassListItem>> GetClassesAsync(int teacherId)
    {
        using var conn = _db.CreateConnection();

        var classes = (await conn.QueryAsync<ClassEntity>(
            "SELECT * FROM \"Classes\" WHERE \"teacher_id\" = @TeacherId ORDER BY \"name\" ASC",
            new { TeacherId = teacherId })).ToList();

        var result = new List<ClassListItem>();
        foreach (var cls in classes)
        {
            var count = await conn.ExecuteScalarAsync<int>(
                "SELECT COUNT(*) FROM \"ClassStudents\" WHERE \"class_id\" = @ClassId",
                new { ClassId = cls.Id });

            result.Add(new ClassListItem
            {
                Id = cls.Id,
                Name = cls.Name,
                Subject = cls.Subject,
                Description = cls.Description,
                StudentCount = count,
                CreatedAt = cls.CreatedAt
            });
        }

        return result;
    }

    public async Task<ClassDetailResponse> GetClassDetailAsync(int classId, int teacherId)
    {
        using var conn = _db.CreateConnection();

        var cls = await conn.QueryFirstOrDefaultAsync<ClassEntity>(
            "SELECT * FROM \"Classes\" WHERE \"id\" = @Id AND \"teacher_id\" = @TeacherId",
            new { Id = classId, TeacherId = teacherId });
        if (cls == null) throw new AppException(404, "Class not found");

        var teacher = await conn.QueryFirstOrDefaultAsync<User>(
            "SELECT * FROM \"Users\" WHERE \"id\" = @Id", new { Id = teacherId });

        var students = (await conn.QueryAsync<User>(
            @"SELECT u.* FROM ""Users"" u
              JOIN ""ClassStudents"" cs ON cs.""student_id"" = u.""id""
              WHERE cs.""class_id"" = @ClassId
              ORDER BY u.""name"" ASC",
            new { ClassId = classId })).ToList();

        return new ClassDetailResponse
        {
            Id = cls.Id,
            Name = cls.Name,
            Subject = cls.Subject,
            Description = cls.Description,
            TeacherName = teacher?.Name ?? "Unknown",
            StudentCount = students.Count,
            CreatedAt = cls.CreatedAt,
            Students = students.Select(s => new StudentInClass
            {
                Id = s.Id,
                Name = s.Name,
                Email = s.Email,
                StudentId = s.StudentId,
                Phone = s.Phone
            }).ToList()
        };
    }

    public async Task<object> AddStudentsToClassAsync(int classId, int teacherId, List<int> studentIds)
    {
        using var conn = _db.CreateConnection();

        var cls = await conn.QueryFirstOrDefaultAsync<ClassEntity>(
            "SELECT * FROM \"Classes\" WHERE \"id\" = @Id AND \"teacher_id\" = @TeacherId",
            new { Id = classId, TeacherId = teacherId });
        if (cls == null) throw new AppException(404, "Class not found");

        var students = (await conn.QueryAsync<User>(
            "SELECT * FROM \"Users\" WHERE \"id\" = ANY(@Ids) AND \"role\" = 'student'",
            new { Ids = studentIds })).ToList();

        var now = DateTime.UtcNow;
        foreach (var student in students)
        {
            await conn.ExecuteAsync(
                @"INSERT INTO ""ClassStudents"" (""class_id"", ""student_id"") VALUES (@ClassId, @StudentId) ON CONFLICT DO NOTHING",
                new { ClassId = classId, StudentId = student.Id });
        }

        return new { added = students.Count };
    }

    public async Task RemoveStudentFromClassAsync(int classId, int teacherId, int studentId)
    {
        using var conn = _db.CreateConnection();

        var cls = await conn.QueryFirstOrDefaultAsync<ClassEntity>(
            "SELECT * FROM \"Classes\" WHERE \"id\" = @Id AND \"teacher_id\" = @TeacherId",
            new { Id = classId, TeacherId = teacherId });
        if (cls == null) throw new AppException(404, "Class not found");

        await conn.ExecuteAsync(
            "DELETE FROM \"ClassStudents\" WHERE \"class_id\" = @ClassId AND \"student_id\" = @StudentId",
            new { ClassId = classId, StudentId = studentId });
    }

    public async Task DeleteClassAsync(int classId, int teacherId)
    {
        using var conn = _db.CreateConnection();

        var cls = await conn.QueryFirstOrDefaultAsync<ClassEntity>(
            "SELECT * FROM \"Classes\" WHERE \"id\" = @Id AND \"teacher_id\" = @TeacherId",
            new { Id = classId, TeacherId = teacherId });
        if (cls == null) throw new AppException(404, "Class not found");

        await conn.ExecuteAsync("DELETE FROM \"ClassStudents\" WHERE \"class_id\" = @ClassId", new { ClassId = classId });
        await conn.ExecuteAsync("DELETE FROM \"Classes\" WHERE \"id\" = @Id", new { Id = classId });
    }

    public async Task<object> SendAnnouncementAsync(int teacherId, AnnouncementRequest data)
    {
        using var conn = _db.CreateConnection();
        var now = DateTime.UtcNow;
        List<int> targetIds;

        if (data.UserIds != null && data.UserIds.Count > 0)
        {
            targetIds = data.UserIds;
        }
        else if (!string.IsNullOrEmpty(data.Role))
        {
            if (data.Role == "all")
            {
                var ids = await conn.QueryAsync<int>("SELECT \"id\" FROM \"Users\" WHERE \"role\" IN ('student', 'teacher')");
                targetIds = ids.ToList();
            }
            else
            {
                var ids = await conn.QueryAsync<int>("SELECT \"id\" FROM \"Users\" WHERE \"role\" = @Role",
                    new { Role = data.Role });
                targetIds = ids.ToList();
            }
        }
        else
        {
            var classes = (await conn.QueryAsync<ClassEntity>(
                "SELECT * FROM \"Classes\" WHERE \"teacher_id\" = @TeacherId",
                new { TeacherId = teacherId })).ToList();

            var classIds = classes.Select(c => c.Id).ToList();
            if (classIds.Count == 0) return new { sentCount = 0 };

            var ids = await conn.QueryAsync<int>(
                "SELECT DISTINCT \"student_id\" FROM \"ClassStudents\" WHERE \"class_id\" = ANY(@Ids)",
                new { Ids = classIds });
            targetIds = ids.ToList();
        }

        foreach (var uid in targetIds)
        {
            await conn.ExecuteAsync(
                @"INSERT INTO ""Notifications"" (""user_id"", ""title"", ""message"", ""type"", ""created_at"")
                  VALUES (@UserId, @Title, @Message, 'announcement', @CreatedAt)",
                new { UserId = uid, Title = data.Title, Message = data.Message, CreatedAt = now });

            await _hub.NotifyUser(uid, "NewNotification", new { title = data.Title, message = data.Message, type = "announcement" });
        }

        return new { sentCount = targetIds.Count };
    }

    public async Task<Exam> ScheduleExamAsync(int examId, int teacherId, string scheduledAt)
    {
        using var conn = _db.CreateConnection();

        var exam = await conn.QueryFirstOrDefaultAsync<Exam>(
            "SELECT * FROM \"Exams\" WHERE \"id\" = @Id", new { Id = examId });
        if (exam == null || exam.TeacherId != teacherId)
            throw new AppException(404, "Exam not found");

        if (!DateTime.TryParse(scheduledAt, out var scheduleDate))
            throw new AppException(400, "Invalid scheduled date");

        exam = await conn.QuerySingleAsync<Exam>(
            "UPDATE \"Exams\" SET \"scheduled_at\" = @ScheduledAt, \"status\" = 'draft', \"updated_at\" = @Now WHERE \"id\" = @Id RETURNING *",
            new { ScheduledAt = scheduleDate, Now = DateTime.UtcNow, Id = examId });

        return exam;
    }

    public async Task<List<ParentContactItem>> GetParentContactsAsync(int teacherId)
    {
        using var conn = _db.CreateConnection();

        var contacts = (await conn.QueryAsync<ParentContact>(
            @"SELECT pc.*, u.""name"" as ""StudentName""
              FROM ""ParentContacts"" pc
              JOIN ""Users"" u ON pc.""student_id"" = u.""id""
              WHERE u.""role"" = 'student'
              ORDER BY u.""name"" ASC",
            null)).ToList();

        return contacts.Select(c => new ParentContactItem
        {
            Id = c.Id,
            StudentId = c.StudentId,
            StudentName = c.StudentName ?? "Unknown",
            ParentName = c.ParentName,
            ParentPhone = c.ParentPhone,
            ParentEmail = c.ParentEmail
        }).ToList();
    }

    public async Task<object> CreateOrUpdateParentContactAsync(int studentId, int teacherId, ParentContactRequest data)
    {
        using var conn = _db.CreateConnection();

        var student = await conn.QueryFirstOrDefaultAsync<User>(
            "SELECT * FROM \"Users\" WHERE \"id\" = @Id", new { Id = studentId });
        if (student == null || student.Role != "student")
            throw new AppException(404, "Student not found");

        var now = DateTime.UtcNow;
        var existing = await conn.QueryFirstOrDefaultAsync<ParentContact>(
            "SELECT * FROM \"ParentContacts\" WHERE \"student_id\" = @StudentId", new { StudentId = studentId });

        if (existing != null)
        {
            await conn.ExecuteAsync(
                @"UPDATE ""ParentContacts"" SET ""parent_name"" = @ParentName, ""parent_phone"" = @ParentPhone, ""parent_email"" = @ParentEmail, ""updated_at"" = @Now WHERE ""id"" = @Id",
                new { ParentName = data.ParentName, ParentPhone = data.ParentPhone ?? "", ParentEmail = data.ParentEmail ?? "", Now = now, Id = existing.Id });
            return new { contact = existing, created = false };
        }

        var contact = await conn.QuerySingleAsync<ParentContact>(
            @"INSERT INTO ""ParentContacts"" (""student_id"", ""parent_name"", ""parent_phone"", ""parent_email"", ""teacher_id"", ""created_at"", ""updated_at"")
              VALUES (@StudentId, @ParentName, @ParentPhone, @ParentEmail, @TeacherId, @CreatedAt, @UpdatedAt) RETURNING *",
            new { StudentId = studentId, ParentName = data.ParentName, ParentPhone = data.ParentPhone ?? "", ParentEmail = data.ParentEmail ?? "", TeacherId = teacherId, CreatedAt = now, UpdatedAt = now });

        return new { contact, created = true };
    }

    public async Task DeleteParentContactAsync(int studentId, int teacherId)
    {
        using var conn = _db.CreateConnection();
        await conn.ExecuteAsync(
            "DELETE FROM \"ParentContacts\" WHERE \"student_id\" = @StudentId",
            new { StudentId = studentId });
    }

    public async Task<object?> SearchStudentBySidAsync(string studentId)
    {
        using var conn = _db.CreateConnection();

        var student = await conn.QueryFirstOrDefaultAsync<User>(
            "SELECT * FROM \"Users\" WHERE \"student_id\" = @Sid AND \"role\" = 'student'",
            new { Sid = studentId });

        if (student == null) return null;

        return new
        {
            id = student.Id,
            name = student.Name,
            email = student.Email,
            student_id = student.StudentId,
            phone = student.Phone
        };
    }

    public async Task DeleteStudentAsync(int studentId)
    {
        using var conn = _db.CreateConnection();

        var student = await conn.QueryFirstOrDefaultAsync<User>(
            "SELECT * FROM \"Users\" WHERE \"id\" = @Id AND \"role\" = 'student'", new { Id = studentId });
        if (student == null)
            throw new AppException(404, "Student not found");

        await conn.ExecuteAsync("DELETE FROM \"Users\" WHERE \"id\" = @Id", new { Id = studentId });
    }

    public async Task<List<ParentNotification>> GetParentReportHistoryAsync(int examId, int teacherId)
    {
        using var conn = _db.CreateConnection();

        var exam = await conn.QueryFirstOrDefaultAsync<Exam>(
            "SELECT * FROM \"Exams\" WHERE \"id\" = @Id", new { Id = examId });
        if (exam == null || exam.TeacherId != teacherId)
            throw new AppException(404, "Exam not found or access denied");

        var reports = (await conn.QueryAsync<ParentNotification>(
            "SELECT * FROM \"ParentNotifications\" WHERE \"exam_id\" = @ExamId ORDER BY \"sent_at\" DESC",
            new { ExamId = examId })).ToList();

        return reports;
    }

    private static readonly HashSet<string> Letters = new() { "A", "B", "C", "D" };
}
