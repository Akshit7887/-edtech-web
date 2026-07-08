using Microsoft.AspNetCore.SignalR;

namespace EdTechApi.Hubs;

public class DashboardHub : Hub
{
    public async Task JoinTeacherGroup(int teacherId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"teacher_{teacherId}");
    }

    public async Task JoinStudentGroup(int studentId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"student_{studentId}");
    }

    public async Task LeaveTeacherGroup(int teacherId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"teacher_{teacherId}");
    }

    public async Task LeaveStudentGroup(int studentId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"student_{studentId}");
    }
}
