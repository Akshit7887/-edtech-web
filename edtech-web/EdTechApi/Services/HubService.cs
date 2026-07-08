using EdTechApi.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace EdTechApi.Services;

public interface IHubService
{
    Task NotifyTeacherDashboard(int teacherId, string eventType, object data);
    Task NotifyStudentDashboard(int studentId, string eventType, object data);
    Task NotifyExamGroup(int examId, string eventType, object data);
    Task NotifyUser(int userId, string eventType, object data);
}

public class HubService : IHubService
{
    private readonly IHubContext<DashboardHub> _dashboardHub;
    private readonly IHubContext<ExamHub> _examHub;
    private readonly IHubContext<NotificationHub> _notificationHub;

    public HubService(
        IHubContext<DashboardHub> dashboardHub,
        IHubContext<ExamHub> examHub,
        IHubContext<NotificationHub> notificationHub)
    {
        _dashboardHub = dashboardHub;
        _examHub = examHub;
        _notificationHub = notificationHub;
    }

    public async Task NotifyTeacherDashboard(int teacherId, string eventType, object data)
    {
        await _dashboardHub.Clients.Group("teacher_" + teacherId).SendAsync(eventType, data);
    }

    public async Task NotifyStudentDashboard(int studentId, string eventType, object data)
    {
        await _dashboardHub.Clients.Group("student_" + studentId).SendAsync(eventType, data);
    }

    public async Task NotifyExamGroup(int examId, string eventType, object data)
    {
        await _examHub.Clients.Group("exam_" + examId).SendAsync(eventType, data);
    }

    public async Task NotifyUser(int userId, string eventType, object data)
    {
        await _notificationHub.Clients.Group("user_" + userId).SendAsync(eventType, data);
    }
}
