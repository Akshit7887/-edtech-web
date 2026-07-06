using System.Data;
using Dapper;
using EdTechApi.Data;
using EdTechApi.DTOs;
using EdTechApi.Models;

namespace EdTechApi.Services;

public interface IQuestionService
{
    Task<object> StartExamAsync(int studentId, int examId);
    Task<object> SubmitExamAsync(int sessionId, List<AnswerDto> answers);
    Task<List<object>> GetStudentExamQuestionsAsync(int studentId, int examId);
    Task<ExamStatisticsResponse> GetExamStatisticsAsync(int examId, int teacherId);
    Task<object> DisqualifyStudentAsync(int sessionId, string reason);
    Task<object> GenerateAndAssignPersonalizedQuestionsAsync(int examId, int teacherId, int questionCount, string difficulty);
    Task<object> AssignQuestionsToStudentsAsync(int examId, List<int> studentIds);
    Task<object> CreateExamSessionAsync(int studentId, int examId, string ipAddress, string userAgent);
    Task<object> SubmitExamAnswersAsync(int sessionId, List<AnswerDto> answers);
    Task<object> GetExamSessionAsync(int studentId, int examId);
    Task<object> DisqualifySessionAsync(int sessionId, string reason);
}

public class QuestionService : IQuestionService
{
    private readonly IDbConnectionFactory _db;
    private readonly IGeminiService _gemini;
    private readonly ILogger<QuestionService> _logger;
    private static readonly ThreadLocal<Random> _random = new(() => new Random());
    private static readonly string[] Letters = { "A", "B", "C", "D" };

    public QuestionService(IDbConnectionFactory db, IGeminiService gemini, ILogger<QuestionService> logger)
    {
        _db = db;
        _gemini = gemini;
        _logger = logger;
    }

    public async Task<object> StartExamAsync(int studentId, int examId)
    {
        using var conn = _db.CreateConnection();

        var exam = await conn.QueryFirstOrDefaultAsync<Exam>(
            "SELECT * FROM \"Exams\" WHERE \"id\" = @Id", new { Id = examId });
        if (exam == null) throw new AppException(404, "Exam not found");
        if (exam.Status != "active") throw new AppException(400, "Exam is not active");

        var assignment = await conn.QueryFirstOrDefaultAsync<StudentExamAssignment>(
            "SELECT * FROM \"StudentExamAssignments\" WHERE \"student_id\" = @StudentId AND \"exam_id\" = @ExamId",
            new { StudentId = studentId, ExamId = examId });
        if (assignment == null) throw new AppException(403, "You are not assigned to this exam");

        var existing = await conn.QueryFirstOrDefaultAsync<ExamSession>(
            "SELECT * FROM \"ExamSessions\" WHERE \"student_id\" = @StudentId AND \"exam_id\" = @ExamId AND \"status\" != 'disqualified'",
            new { StudentId = studentId, ExamId = examId });

        if (existing != null)
        {
            if (existing.Status == "in_progress") return existing;
            if (existing.Status == "completed") throw new AppException(400, "Exam already completed");
        }

        var now = DateTime.UtcNow;
        var session = await conn.QuerySingleAsync<ExamSession>(
            @"INSERT INTO ""ExamSessions"" (""student_id"", ""exam_id"", ""total_questions"", ""status"", ""started_at"", ""time_remaining_seconds"", ""ip_address"", ""user_agent"", ""mode"", ""created_at"", ""updated_at"")
              VALUES (@StudentId, @ExamId, @TotalQuestions, 'in_progress', @StartedAt, @TimeRemaining, '0.0.0.0', 'mobile', 'exam', @CreatedAt, @UpdatedAt) RETURNING *",
            new
            {
                StudentId = studentId,
                ExamId = examId,
                TotalQuestions = exam.TotalQuestions,
                StartedAt = now,
                TimeRemaining = exam.DurationMinutes * 60,
                CreatedAt = now,
                UpdatedAt = now
            });

        return session;
    }

    public async Task<object> SubmitExamAsync(int sessionId, List<AnswerDto> answers)
    {
        using var conn = _db.CreateConnection();
        var result = await SubmitExamAnswersAsync(sessionId, answers);
        return result;
    }

    public async Task<List<object>> GetStudentExamQuestionsAsync(int studentId, int examId)
    {
        using var conn = _db.CreateConnection();

        var assignment = await conn.QueryFirstOrDefaultAsync<StudentExamAssignment>(
            "SELECT * FROM \"StudentExamAssignments\" WHERE \"student_id\" = @StudentId AND \"exam_id\" = @ExamId",
            new { StudentId = studentId, ExamId = examId });
        if (assignment == null) throw new AppException(403, "Not assigned to this exam");

        var assignedIds = assignment.QuestionIds ?? new();
        var allQuestions = (await conn.QueryAsync<QuestionPool>(
            "SELECT * FROM \"QuestionPool\" WHERE \"exam_id\" = @ExamId",
            new { ExamId = examId })).ToList();

        var assigned = allQuestions.Where(q => assignedIds.Contains(q.Id)).ToList();
        return assigned.Select(q => ShuffleOptions(q)).Cast<object>().ToList();
    }

    public async Task<ExamStatisticsResponse> GetExamStatisticsAsync(int examId, int teacherId)
    {
        using var conn = _db.CreateConnection();

        var exam = await conn.QueryFirstOrDefaultAsync<Exam>(
            "SELECT * FROM \"Exams\" WHERE \"id\" = @Id", new { Id = examId });
        if (exam == null || exam.TeacherId != teacherId)
            throw new AppException(404, "Exam not found or access denied");

        var sessions = (await conn.QueryAsync<ExamSession>(
            "SELECT * FROM \"ExamSessions\" WHERE \"exam_id\" = @ExamId ORDER BY \"score\" DESC",
            new { ExamId = examId })).ToList();

        var completed = sessions.Where(s => s.Status == "completed").ToList();
        var scores = completed.Select(s => (double)s.Score).ToList();
        var avgScore = scores.Count > 0 ? scores.Average() : 0;
        var maxScore = scores.Count > 0 ? scores.Max() : 0;
        var minScore = scores.Count > 0 ? scores.Min() : 0;
        var passThreshold = exam.TotalQuestions * 0.4;
        var passCount = completed.Count(s => s.Score >= (decimal)passThreshold);

        var scoreDist = new Dictionary<string, int>
        {
            ["0-25"] = 0, ["26-50"] = 0, ["51-75"] = 0, ["76-100"] = 0
        };
        foreach (var s in completed)
        {
            var pct = exam.TotalQuestions > 0 ? (double)(s.Score / exam.TotalQuestions) * 100 : 0;
            if (pct <= 25) scoreDist["0-25"]++;
            else if (pct <= 50) scoreDist["26-50"]++;
            else if (pct <= 75) scoreDist["51-75"]++;
            else scoreDist["76-100"]++;
        }

        var studentIds = sessions.Select(s => s.StudentId).Distinct().ToList();
        var students = studentIds.Any()
            ? (await conn.QueryAsync<User>("SELECT * FROM \"Users\" WHERE \"id\" = ANY(@Ids)", new { Ids = studentIds }))
                .ToDictionary(s => s.Id)
            : new Dictionary<int, User>();

        var studentResults = sessions.Select(s => new StudentResultItem
        {
            StudentId = s.StudentId,
            StudentName = students.ContainsKey(s.StudentId) ? students[s.StudentId].Name : "Unknown",
            Email = students.ContainsKey(s.StudentId) ? students[s.StudentId].Email : "",
            Score = s.Score,
            TotalQuestions = s.TotalQuestions,
            Status = s.Status,
            SubmittedAt = s.SubmittedAt,
            TimeUsed = s.StartedAt.HasValue && s.SubmittedAt.HasValue
                ? (int)Math.Round((s.SubmittedAt.Value - s.StartedAt.Value).TotalMinutes)
                : null
        }).ToList();

        return new ExamStatisticsResponse
        {
            ExamTitle = exam.Title,
            Subject = exam.Subject,
            TotalStudents = sessions.Count,
            CompletedCount = completed.Count,
            AverageScore = Math.Round(avgScore, 2),
            HighestScore = (int)maxScore,
            LowestScore = (int)minScore,
            PassCount = passCount,
            FailCount = completed.Count - passCount,
            PassRate = completed.Count > 0 ? (int)Math.Round((double)passCount / completed.Count * 100) : 0,
            ScoreDistribution = scoreDist,
            MaxPossibleScore = exam.TotalQuestions,
            StudentResults = studentResults
        };
    }

    public async Task<object> DisqualifyStudentAsync(int sessionId, string reason)
    {
        using var conn = _db.CreateConnection();

        var session = await conn.QueryFirstOrDefaultAsync<ExamSession>(
            "SELECT * FROM \"ExamSessions\" WHERE \"id\" = @Id", new { Id = sessionId });
        if (session == null) throw new AppException(404, "Session not found");

        var now = DateTime.UtcNow;
        await conn.ExecuteAsync(
            "UPDATE \"ExamSessions\" SET \"status\" = 'disqualified', \"disqualified_reason\" = @Reason, \"submitted_at\" = @Now, \"updated_at\" = @Now WHERE \"id\" = @Id",
            new { Reason = reason ?? "Disqualified", Now = now, Id = sessionId });

        await TriggerParentNotification(session.StudentId, session.ExamId, "disqualified", 0);

        return new { success = true };
    }

    public async Task<object> GenerateAndAssignPersonalizedQuestionsAsync(int examId, int teacherId, int questionCount, string difficulty)
    {
        using var conn = _db.CreateConnection();

        var exam = await conn.QueryFirstOrDefaultAsync<Exam>(
            "SELECT * FROM \"Exams\" WHERE \"id\" = @Id", new { Id = examId });
        if (exam == null || exam.TeacherId != teacherId)
            throw new AppException(404, "Exam not found or access denied");

        var students = (await conn.QueryAsync<StudentExamAssignment>(
            "SELECT * FROM \"StudentExamAssignments\" WHERE \"exam_id\" = @ExamId",
            new { ExamId = examId })).ToList();

        var totalGenerated = 0;
        foreach (var assignment in students)
        {
            var student = await conn.QueryFirstOrDefaultAsync<User>(
                "SELECT * FROM \"Users\" WHERE \"id\" = @Id", new { Id = assignment.StudentId });
            if (student == null) continue;

            var syllabusText = exam.SyllabusText ?? "";
            if (string.IsNullOrEmpty(syllabusText)) continue;

            try
            {
                var questions = await _gemini.GenerateQuestionsForStudent(student.Name, syllabusText, questionCount, difficulty);
                if (questions.Count == 0) continue;

                var savedIds = new List<int>();
                foreach (var q in questions)
                {
                    var saved = await conn.QuerySingleAsync<QuestionPool>(
                        @"INSERT INTO ""QuestionPool"" (""exam_id"", ""student_id"", ""question_text"", ""option_a"", ""option_b"", ""option_c"", ""option_d"", ""correct_answer"", ""difficulty"", ""points"", ""status"", ""created_at"", ""updated_at"")
                          VALUES (@ExamId, @StudentId, @QuestionText, @OptionA, @OptionB, @OptionC, @OptionD, @CorrectAnswer, @Difficulty, 1, 'published', @Now, @Now) RETURNING *",
                        new
                        {
                            ExamId = examId,
                            StudentId = student.Id,
                            QuestionText = q.question_text,
                            OptionA = q.option_a,
                            OptionB = q.option_b,
                            OptionC = q.option_c,
                            OptionD = q.option_d,
                            CorrectAnswer = q.correct_answer,
                            Difficulty = q.difficulty ?? difficulty,
                            Now = DateTime.UtcNow
                        });
                    savedIds.Add(saved.Id);
                }

                assignment.QuestionIds = savedIds;
                await conn.ExecuteAsync(
                    "UPDATE \"StudentExamAssignments\" SET \"question_ids\" = @Ids, \"updated_at\" = @Now WHERE \"id\" = @Id",
                    new { Ids = savedIds, Now = DateTime.UtcNow, Id = assignment.Id });
                totalGenerated += savedIds.Count;
            }
            catch (Exception ex)
            {
                _logger.LogWarning("[Personalized] Failed for student {StudentId}: {Error}", student.Id, ex.Message);
            }
        }

        return new { totalGenerated, totalStudents = students.Count };
    }

    public async Task<object> AssignQuestionsToStudentsAsync(int examId, List<int> studentIds)
    {
        using var conn = _db.CreateConnection();

        var allPool = (await conn.QueryAsync<QuestionPool>(
            "SELECT * FROM \"QuestionPool\" WHERE \"exam_id\" = @ExamId AND \"status\" = 'published'",
            new { ExamId = examId })).ToList();

        var exam = await conn.QueryFirstOrDefaultAsync<Exam>(
            "SELECT * FROM \"Exams\" WHERE \"id\" = @Id", new { Id = examId });

        var assignments = new List<object>();
        foreach (var studentId in studentIds)
        {
            var shuffled = FisherYatesShuffle(allPool);
            var qIds = shuffled.Take(exam?.TotalQuestions ?? shuffled.Count).Select(q => q.Id).ToList();

            var now = DateTime.UtcNow;
            await conn.ExecuteAsync(
                "INSERT INTO \"StudentExamAssignments\" (\"student_id\", \"exam_id\", \"question_ids\", \"created_at\", \"updated_at\") VALUES (@StudentId, @ExamId, @QuestionIds, @Now, @Now) ON CONFLICT (\"student_id\", \"exam_id\") DO UPDATE SET \"question_ids\" = @QuestionIds, \"updated_at\" = @Now",
                new { StudentId = studentId, ExamId = examId, QuestionIds = qIds, Now = now });

            assignments.Add(new { studentId, questionCount = qIds.Count });
        }

        return assignments;
    }

    public async Task<object> CreateExamSessionAsync(int studentId, int examId, string ipAddress, string userAgent)
    {
        return await StartExamAsync(studentId, examId);
    }

    public async Task<object> SubmitExamAnswersAsync(int sessionId, List<AnswerDto> answers)
    {
        using var conn = _db.CreateConnection();

        var session = await conn.QueryFirstOrDefaultAsync<ExamSession>(
            "SELECT * FROM \"ExamSessions\" WHERE \"id\" = @Id", new { Id = sessionId });
        if (session == null) throw new AppException(404, "Session not found");

        var assignedIds = session.Answers?.Select(a => a.QuestionId).ToList() ?? new();
        var allQuestions = (await conn.QueryAsync<QuestionPool>(
            "SELECT * FROM \"QuestionPool\" WHERE \"exam_id\" = @ExamId",
            new { ExamId = session.ExamId })).ToList();

        var score = 0m;
        var answeredCount = 0;
        var savedAnswers = new List<ExamSession.Answer>();

        foreach (var answer in answers)
        {
            var question = allQuestions.FirstOrDefault(q => q.Id == answer.QuestionId);
            if (question == null) continue;
            answeredCount++;

            var isCorrect = question.CorrectAnswer == answer.Answer;
            if (isCorrect) score += question.Points;

            savedAnswers.Add(new ExamSession.Answer
            {
                QuestionId = answer.QuestionId,
                SelectedAnswer = answer.Answer
            });
        }

        var now = DateTime.UtcNow;
        var answersJson = System.Text.Json.JsonSerializer.Serialize(savedAnswers);
        await conn.ExecuteAsync(
            @"UPDATE ""ExamSessions"" SET ""score"" = @Score, ""answered_count"" = @AnsweredCount, ""status"" = 'completed', ""submitted_at"" = @Now, ""answers"" = @Answers::jsonb, ""updated_at"" = @Now WHERE ""id"" = @Id",
            new { Score = score, AnsweredCount = answeredCount, Now = now, Answers = answersJson, Id = sessionId });

        await TriggerParentNotification(session.StudentId, session.ExamId, "completed", (int)score);

        return new { score, totalQuestions = session.TotalQuestions, correctAnswers = (int)score, wrongAnswers = answeredCount - (int)score };
    }

    public async Task<object> GetExamSessionAsync(int studentId, int examId)
    {
        using var conn = _db.CreateConnection();
        var session = await conn.QueryFirstOrDefaultAsync<ExamSession>(
            "SELECT * FROM \"ExamSessions\" WHERE \"student_id\" = @StudentId AND \"exam_id\" = @ExamId ORDER BY \"created_at\" DESC LIMIT 1",
            new { StudentId = studentId, ExamId = examId });
        if (session == null) throw new AppException(404, "Session not found");
        return session;
    }

    public async Task<object> DisqualifySessionAsync(int sessionId, string reason)
    {
        return await DisqualifyStudentAsync(sessionId, reason);
    }

    private object ShuffleOptions(QuestionPool question)
    {
        var options = new List<(string key, string val)>();
        if (!string.IsNullOrEmpty(question.OptionA)) options.Add(("A", question.OptionA));
        if (!string.IsNullOrEmpty(question.OptionB)) options.Add(("B", question.OptionB));
        if (!string.IsNullOrEmpty(question.OptionC)) options.Add(("C", question.OptionC));
        if (!string.IsNullOrEmpty(question.OptionD)) options.Add(("D", question.OptionD));

        var correctKey = question.CorrectAnswer;
        var shuffled = FisherYatesShuffle(options);

        var newKeyMap = new Dictionary<string, string>();
        for (int i = 0; i < shuffled.Count; i++)
            newKeyMap[shuffled[i].key] = Letters[i];

        var newCorrect = newKeyMap[correctKey];

        return new
        {
            questionText = question.QuestionText,
            difficulty = question.Difficulty,
            points = question.Points,
            options = shuffled.Select((opt, i) => new { key = Letters[i], value = opt.val }),
            correctAnswer = newCorrect,
            originalQuestionId = question.Id
        };
    }

    private async Task TriggerParentNotification(int studentId, int examId, string eventType, int score)
    {
        try
        {
            using var conn = _db.CreateConnection();
            var exam = await conn.QueryFirstOrDefaultAsync<Exam>(
                "SELECT * FROM \"Exams\" WHERE \"id\" = @Id", new { Id = examId });
            var student = await conn.QueryFirstOrDefaultAsync<User>(
                "SELECT * FROM \"Users\" WHERE \"id\" = @Id", new { Id = studentId });
            var contact = await conn.QueryFirstOrDefaultAsync<ParentContact>(
                "SELECT * FROM \"ParentContacts\" WHERE \"student_id\" = @StudentId", new { StudentId = studentId });
            if (contact == null) return;

            var msg = eventType switch
            {
                "completed" => $"{student?.Name ?? "Student"} completed \"{exam?.Title ?? "Exam"}\" with score {score}",
                "disqualified" => $"{student?.Name ?? "Student"} was disqualified from \"{exam?.Title ?? "Exam"}\"",
                _ => $"{student?.Name ?? "Student"} {eventType} in \"{exam?.Title ?? "Exam"}\""
            };

            await conn.ExecuteAsync(
                @"INSERT INTO ""ParentNotifications"" (""exam_id"", ""student_id"", ""parent_contact_id"", ""sent_to"", ""message"", ""sent_at"", ""created_at"")
                  VALUES (@ExamId, @StudentId, @ContactId, @SentTo, @Message, @Now, @Now)",
                new { ExamId = examId, StudentId = studentId, ContactId = contact.Id, SentTo = contact.ParentPhone ?? contact.ParentEmail ?? "", Message = msg, Now = DateTime.UtcNow });
        }
        catch (Exception ex)
        {
            _logger.LogWarning("[ParentNotification] Failed: {Error}", ex.Message);
        }
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
