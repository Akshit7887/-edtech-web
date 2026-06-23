using System.Data;
using Dapper;
using EdTechApi.Data;
using EdTechApi.DTOs;
using EdTechApi.Models;

namespace EdTechApi.Services;

public interface IStudentService
{
    Task<StudentAnalyticsResponse> GetAnalyticsAsync(int studentId);
    Task<ExamReviewResponse> GetExamReviewAsync(int sessionId, int userId);
    Task<object> CreatePracticeSessionAsync(int studentId, int examId);
    Task<object> SubmitPracticeAsync(int studentId, int examId, List<AnswerDto> answers);
    Task<object> GetNotificationsAsync(int userId, int page = 1, int limit = 50);
    Task<Notification> MarkNotificationReadAsync(int notificationId, int userId);
    Task<object> MarkAllNotificationsReadAsync(int userId);
    Task<Notification> CreateNotificationAsync(int userId, string title, string message, string type = "general");
}

public class StudentService : IStudentService
{
    private readonly IDbConnectionFactory _db;

    public StudentService(IDbConnectionFactory db)
    {
        _db = db;
    }

    public async Task<StudentAnalyticsResponse> GetAnalyticsAsync(int studentId)
    {
        using var conn = _db.CreateConnection();

        var sessions = (await conn.QueryAsync<ExamSession>(
            "SELECT * FROM \"ExamSessions\" WHERE \"student_id\" = @StudentId AND \"mode\" = 'exam' ORDER BY \"created_at\" ASC",
            new { StudentId = studentId })).ToList();

        var examIds = sessions.Select(s => s.ExamId).Distinct().ToList();
        var exams = examIds.Any()
            ? (await conn.QueryAsync<Exam>("SELECT * FROM \"Exams\" WHERE \"id\" = ANY(@Ids)", new { Ids = examIds }))
                .ToDictionary(e => e.Id)
            : new Dictionary<int, Exam>();

        var completed = sessions.Where(s => s.Status == "completed").ToList();
        var disqualified = sessions.Where(s => s.Status == "disqualified").ToList();
        var inProgress = sessions.Where(s => s.Status == "in_progress").ToList();

        var avgScore = completed.Count > 0 ? completed.Average(s => (double)s.Score) : 0;
        var totalPossible = completed.Sum(s => (double)(exams.ContainsKey(s.ExamId) ? exams[s.ExamId].TotalQuestions : s.TotalQuestions));
        var totalEarned = completed.Sum(s => (double)s.Score);

        var subjectWise = new Dictionary<string, (double total, double earned, int count)>();
        foreach (var s in completed)
        {
            var subject = exams.ContainsKey(s.ExamId) ? exams[s.ExamId].Subject : "General";
            if (!subjectWise.ContainsKey(subject))
                subjectWise[subject] = (0, 0, 0);
            var entry = subjectWise[subject];
            subjectWise[subject] = (entry.total + (exams.ContainsKey(s.ExamId) ? exams[s.ExamId].TotalQuestions : s.TotalQuestions),
                                    entry.earned + (double)s.Score,
                                    entry.count + 1);
        }

        var subjectAverages = subjectWise.Select(kv => new
        {
            subject = kv.Key,
            average = kv.Value.total > 0 ? (int)Math.Round(kv.Value.earned / kv.Value.total * 100) : 0,
            examsTaken = kv.Value.count,
            totalScore = kv.Value.earned,
            maxScore = kv.Value.total
        }).ToList();

        var trend = completed.TakeLast(10).Select(s => new
        {
            examTitle = exams.ContainsKey(s.ExamId) ? exams[s.ExamId].Title : "Exam",
            score = s.Score,
            total = exams.ContainsKey(s.ExamId) ? exams[s.ExamId].TotalQuestions : s.TotalQuestions,
            percentage = (exams.ContainsKey(s.ExamId) ? exams[s.ExamId].TotalQuestions : s.TotalQuestions) > 0
                ? (int)Math.Round((double)s.Score / (exams.ContainsKey(s.ExamId) ? exams[s.ExamId].TotalQuestions : s.TotalQuestions) * 100)
                : 0,
            submittedAt = s.SubmittedAt
        }).ToList();

        return new StudentAnalyticsResponse
        {
            TotalExams = sessions.Count,
            CompletedExams = completed.Count,
            AverageScore = completed.Count > 0 ? Math.Round(avgScore, 2) : 0,
            HighestScore = completed.Count > 0 ? (int)completed.Max(s => s.Score) : 0,
            BestRank = completed.Count > 0 ? 1 : 0,
            ExamPerformances = trend.Select(t => new ExamPerformanceItem
            {
                ExamTitle = t.examTitle,
                Score = t.score,
                TotalQuestions = t.total,
                Percentage = t.percentage,
                Status = "completed",
                SubmittedAt = t.submittedAt
            }).ToList()
        };
    }

    public async Task<ExamReviewResponse> GetExamReviewAsync(int sessionId, int userId)
    {
        using var conn = _db.CreateConnection();

        var session = await conn.QueryFirstOrDefaultAsync<ExamSession>(
            "SELECT * FROM \"ExamSessions\" WHERE \"id\" = @Id", new { Id = sessionId });
        if (session == null) throw new AppException(404, "Session not found");
        if (session.StudentId != userId) throw new AppException(403, "Access denied");

        var exam = await conn.QueryFirstOrDefaultAsync<Exam>(
            "SELECT * FROM \"Exams\" WHERE \"id\" = @Id", new { Id = session.ExamId });

        var assignment = await conn.QueryFirstOrDefaultAsync<StudentExamAssignment>(
            "SELECT * FROM \"StudentExamAssignments\" WHERE \"student_id\" = @StudentId AND \"exam_id\" = @ExamId",
            new { StudentId = userId, ExamId = session.ExamId });

        var allQuestions = (await conn.QueryAsync<QuestionPool>(
            "SELECT * FROM \"QuestionPool\" WHERE \"exam_id\" = @ExamId ORDER BY \"id\"",
            new { ExamId = session.ExamId })).ToList();

        var assignedIds = assignment?.QuestionIds ?? allQuestions.Select(q => q.Id).ToList();
        var assignedQuestions = allQuestions.Where(q => assignedIds.Contains(q.Id)).ToList();

        var savedAnswers = session.Answers ?? new();

        var questions = assignedQuestions.Select(q =>
        {
            var studentAnswer = savedAnswers.FirstOrDefault(a => a.QuestionId == q.Id);
            return new ReviewQuestionItem
            {
                Id = q.Id,
                QuestionText = q.QuestionText,
                StudentAnswer = studentAnswer?.SelectedAnswer ?? "",
                CorrectAnswer = q.CorrectAnswer ?? "",
                IsCorrect = studentAnswer != null && studentAnswer.SelectedAnswer == q.CorrectAnswer,
                OptionA = q.OptionA,
                OptionB = q.OptionB,
                OptionC = q.OptionC,
                OptionD = q.OptionD
            };
        }).ToList();

        return new ExamReviewResponse
        {
            ExamId = session.ExamId,
            ExamTitle = exam?.Title ?? "Exam",
            Subject = exam?.Subject ?? "",
            TotalQuestions = session.TotalQuestions,
            Score = session.Score,
            Status = session.Status,
            SubmittedAt = session.SubmittedAt,
            TimeUsed = session.StartedAt.HasValue && session.SubmittedAt.HasValue
                ? (int)Math.Round((session.SubmittedAt.Value - session.StartedAt.Value).TotalMinutes)
                : null,
            Questions = questions
        };
    }

    public async Task<object> CreatePracticeSessionAsync(int studentId, int examId)
    {
        using var conn = _db.CreateConnection();

        var exam = await conn.QueryFirstOrDefaultAsync<Exam>(
            "SELECT * FROM \"Exams\" WHERE \"id\" = @Id", new { Id = examId });
        if (exam == null) throw new AppException(404, "Exam not found");
        if (exam.Status != "active") throw new AppException(400, "Exam is not active");

        var assignment = await conn.QueryFirstOrDefaultAsync<StudentExamAssignment>(
            "SELECT * FROM \"StudentExamAssignments\" WHERE \"student_id\" = @StudentId AND \"exam_id\" = @ExamId",
            new { StudentId = studentId, ExamId = examId });
        if (assignment == null) throw new AppException(400, "No questions assigned to you for this exam");

        var allQuestions = (await conn.QueryAsync<QuestionPool>(
            "SELECT * FROM \"QuestionPool\" WHERE \"exam_id\" = @ExamId ORDER BY \"id\"",
            new { ExamId = examId })).ToList();

        var assignedIds = assignment.QuestionIds ?? new();
        var assignedQuestions = allQuestions.Where(q => assignedIds.Contains(q.Id)).ToList();

        var safeQuestions = assignedQuestions.Select(q => new
        {
            q.Id,
            q.QuestionText,
            q.OptionA,
            q.OptionB,
            q.OptionC,
            q.OptionD,
            q.CorrectAnswer,
            q.Difficulty,
            q.Points
        }).ToList();

        return new
        {
            session = new { studentId, examId, totalQuestions = assignedQuestions.Count, mode = "practice" },
            questions = safeQuestions
        };
    }

    public async Task<object> SubmitPracticeAsync(int studentId, int examId, List<AnswerDto> answers)
    {
        using var conn = _db.CreateConnection();

        var exam = await conn.QueryFirstOrDefaultAsync<Exam>(
            "SELECT * FROM \"Exams\" WHERE \"id\" = @Id", new { Id = examId });
        if (exam == null) throw new AppException(404, "Exam not found");

        var allQuestions = (await conn.QueryAsync<QuestionPool>(
            "SELECT * FROM \"QuestionPool\" WHERE \"exam_id\" = @ExamId",
            new { ExamId = examId })).ToList();

        var totalScore = 0;
        var correctCount = 0;
        var answeredCount = 0;

        var questionResults = answers.Select(answer =>
        {
            var question = allQuestions.FirstOrDefault(q => q.Id == answer.QuestionId);
            if (question == null) return null;

            answeredCount++;
            var isCorrect = question.CorrectAnswer == answer.Answer;
            if (isCorrect) { totalScore += question.Points > 0 ? question.Points : 1; correctCount++; }

            return new
            {
                questionId = answer.QuestionId,
                questionText = question.QuestionText,
                optionA = question.OptionA,
                optionB = question.OptionB,
                optionC = question.OptionC,
                optionD = question.OptionD,
                correctAnswer = question.CorrectAnswer,
                studentAnswer = answer.Answer,
                isCorrect,
                points = question.Points > 0 ? question.Points : 1
            };
        }).Where(x => x != null).ToList();

        return new
        {
            score = totalScore,
            totalQuestions = exam.TotalQuestions > 0 ? exam.TotalQuestions : allQuestions.Count,
            correctAnswers = correctCount,
            wrongAnswers = answeredCount - correctCount,
            questionResults
        };
    }

    public async Task<object> GetNotificationsAsync(int userId, int page = 1, int limit = 50)
    {
        using var conn = _db.CreateConnection();

        page = Math.Max(1, page);
        limit = Math.Min(100, Math.Max(1, limit));
        var offset = (page - 1) * limit;

        var total = await conn.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM \"Notifications\" WHERE \"user_id\" = @UserId", new { UserId = userId });

        var notifications = (await conn.QueryAsync<Notification>(
            "SELECT * FROM \"Notifications\" WHERE \"user_id\" = @UserId ORDER BY \"created_at\" DESC LIMIT @Limit OFFSET @Offset",
            new { UserId = userId, Limit = limit, Offset = offset })).ToList();

        var unreadCount = await conn.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM \"Notifications\" WHERE \"user_id\" = @UserId AND \"is_read\" = false",
            new { UserId = userId });

        return new
        {
            notifications = notifications.Select(n => new NotificationItem
            {
                Id = n.Id,
                Title = n.Title,
                Message = n.Message,
                Type = n.Type,
                IsRead = n.IsRead,
                CreatedAt = n.CreatedAt
            }),
            unreadCount,
            pagination = new { page, limit, total, totalPages = (int)Math.Ceiling((double)total / limit) }
        };
    }

    public async Task<Notification> MarkNotificationReadAsync(int notificationId, int userId)
    {
        using var conn = _db.CreateConnection();

        var notification = await conn.QueryFirstOrDefaultAsync<Notification>(
            "SELECT * FROM \"Notifications\" WHERE \"id\" = @Id AND \"user_id\" = @UserId",
            new { Id = notificationId, UserId = userId });
        if (notification == null) throw new AppException(404, "Notification not found");

        await conn.ExecuteAsync(
            "UPDATE \"Notifications\" SET \"is_read\" = true WHERE \"id\" = @Id",
            new { Id = notificationId });

        notification.IsRead = true;
        return notification;
    }

    public async Task<object> MarkAllNotificationsReadAsync(int userId)
    {
        using var conn = _db.CreateConnection();

        await conn.ExecuteAsync(
            "UPDATE \"Notifications\" SET \"is_read\" = true WHERE \"user_id\" = @UserId AND \"is_read\" = false",
            new { UserId = userId });

        return new { success = true };
    }

    public async Task<Notification> CreateNotificationAsync(int userId, string title, string message, string type = "general")
    {
        using var conn = _db.CreateConnection();

        var now = DateTime.UtcNow;
        return await conn.QuerySingleAsync<Notification>(
            @"INSERT INTO ""Notifications"" (""user_id"", ""title"", ""message"", ""type"", ""created_at"")
              VALUES (@UserId, @Title, @Message, @Type, @CreatedAt) RETURNING *",
            new { UserId = userId, Title = title, Message = message, Type = type, CreatedAt = now });
    }
}
