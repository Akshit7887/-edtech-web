using System.Data;
using Dapper;
using EdTechApi.Data;
using EdTechApi.Models;

namespace EdTechApi.Services;

public interface IReportsService
{
    Task<object> SendParentReportsAsync(int examId, int teacherId);
    Task<object> GetPendingParentReportsAsync(int examId, int teacherId);
}

public class ReportsService : IReportsService
{
    private readonly IDbConnectionFactory _db;
    private readonly IConfiguration _config;
    private readonly IEmailService _email;
    private readonly ILogger<ReportsService> _logger;

    public ReportsService(IDbConnectionFactory db, IConfiguration config, IEmailService email, ILogger<ReportsService> logger)
    {
        _db = db;
        _config = config;
        _email = email;
        _logger = logger;
    }

    public async Task<object> SendParentReportsAsync(int examId, int teacherId)
    {
        using var conn = _db.CreateConnection();

        var exam = await conn.QueryFirstOrDefaultAsync<Exam>(
            "SELECT * FROM \"Exams\" WHERE \"id\" = @Id", new { Id = examId });
        if (exam == null || exam.TeacherId != teacherId)
            throw new AppException(404, "Exam not found or access denied");

        var sessions = (await conn.QueryAsync<ExamSession>(
            "SELECT * FROM \"ExamSessions\" WHERE \"exam_id\" = @ExamId",
            new { ExamId = examId })).ToList();

        var studentIds = sessions.Select(s => s.StudentId).Distinct().ToList();
        var students = (await conn.QueryAsync<User>(
            "SELECT * FROM \"Users\" WHERE \"id\" = ANY(@Ids)", new { Ids = studentIds }))
            .ToDictionary(s => s.Id);

        var parentContacts = (await conn.QueryAsync<ParentContact>(
            "SELECT * FROM \"ParentContacts\" WHERE \"student_id\" = ANY(@Ids)",
            new { Ids = studentIds }))
            .ToDictionary(pc => pc.StudentId);

        var teacher = await conn.QueryFirstOrDefaultAsync<User>(
            "SELECT * FROM \"Users\" WHERE \"id\" = @Id", new { Id = teacherId });

        var results = new List<object>();
        var smsCount = 0;
        var emailCount = 0;

        foreach (var session in sessions)
        {
            if (!students.ContainsKey(session.StudentId)) continue;
            var student = students[session.StudentId];

            if (!parentContacts.ContainsKey(session.StudentId))
            {
                results.Add(new { studentId = student.Id, studentName = student.Name, message = "No parent contact found", sentVia = (string?)null, status = "skipped" });
                continue;
            }

            var contact = parentContacts[session.StudentId];
            var score = (int)session.Score;
            var total = session.TotalQuestions;
            var status = session.Status == "disqualified" ? "Disqualified" : "Completed";
            var reason = session.Status == "disqualified" ? session.DisqualifiedReason ?? "Policy violation" : "";
            var msg = $"Dear Parent, {student.Name} scored {score}/{total} in {exam.Title}. Status: {status}{(string.IsNullOrEmpty(reason) ? "" : $". Reason: {reason}")}. -- {teacher?.Name ?? "Teacher"}";

            var smsResult = new { status = "skipped", message = "SMS not configured" };
            var emailResult = new { status = "skipped", message = "Email provider not configured" };

            if (!string.IsNullOrEmpty(contact.ParentPhone))
            {
                _logger.LogInformation("[SMS] Would send to {Phone}: {Message}", contact.ParentPhone, msg);
                smsResult = new { status = "logged", message = "SMS logged (no provider)" };
                smsCount++;
            }

            if (!string.IsNullOrEmpty(contact.ParentEmail))
            {
                var subject = $"Exam Result: {student.Name} - {exam.Title}";
                var html = $"<div><h2>Exam Result Notification</h2><p>{msg}</p></div>";
                var result = await _email.SendEmailAsync(contact.ParentEmail, subject, html);
                if (result.Status == "sent") emailCount++;
                emailResult = new { status = result.Status, message = result.Message ?? "" };
            }

            var now = DateTime.UtcNow;
            await conn.ExecuteAsync(
                @"INSERT INTO ""ParentNotifications"" (""exam_id"", ""student_id"", ""parent_contact_id"", ""sent_to"", ""message"", ""sent_at"", ""created_at"")
                  VALUES (@ExamId, @StudentId, @ContactId, @SentTo, @Message, @Now, @Now)",
                new { ExamId = examId, StudentId = student.Id, ContactId = contact.Id, SentTo = contact.ParentPhone ?? contact.ParentEmail ?? "", Message = msg, Now = now });

            results.Add(new
            {
                studentId = student.Id,
                studentName = student.Name,
                parentName = contact.ParentName,
                parentPhone = contact.ParentPhone,
                parentEmail = contact.ParentEmail,
                score, total,
                status = session.Status,
                messageSent = msg,
                sms = smsResult,
                email = emailResult
            });
        }

        return new
        {
            examId = exam.Id,
            examTitle = exam.Title,
            totalParents = parentContacts.Count,
            smsSent = smsCount,
            emailSent = emailCount,
            results
        };
    }

    public async Task<object> GetPendingParentReportsAsync(int examId, int teacherId)
    {
        using var conn = _db.CreateConnection();

        var exam = await conn.QueryFirstOrDefaultAsync<Exam>(
            "SELECT * FROM \"Exams\" WHERE \"id\" = @Id", new { Id = examId });
        if (exam == null || exam.TeacherId != teacherId)
            throw new AppException(404, "Exam not found or access denied");

        var sessions = (await conn.QueryAsync<ExamSession>(
            "SELECT * FROM \"ExamSessions\" WHERE \"exam_id\" = @ExamId AND \"status\" = 'completed'",
            new { ExamId = examId })).ToList();

        var studentIds = sessions.Select(s => s.StudentId).Distinct().ToList();
        var students = studentIds.Any()
            ? (await conn.QueryAsync<User>("SELECT * FROM \"Users\" WHERE \"id\" = ANY(@Ids)", new { Ids = studentIds }))
                .ToDictionary(s => s.Id)
            : new Dictionary<int, User>();

        var parentContacts = studentIds.Any()
            ? (await conn.QueryAsync<ParentContact>("SELECT * FROM \"ParentContacts\" WHERE \"student_id\" = ANY(@Ids)", new { Ids = studentIds }))
                .Select(pc => pc.StudentId).ToList()
            : new List<int>();

        var pendingStudents = sessions
            .Where(s => !parentContacts.Contains(s.StudentId))
            .Select(s => new
            {
                studentId = s.StudentId,
                studentName = students.ContainsKey(s.StudentId) ? students[s.StudentId].Name : "Unknown",
                score = s.Score
            }).ToList();

        return new
        {
            examId,
            examTitle = exam.Title,
            totalCompleted = sessions.Count,
            parentsContacted = parentContacts.Count,
            pending = pendingStudents.Count,
            students = pendingStudents
        };
    }
}
