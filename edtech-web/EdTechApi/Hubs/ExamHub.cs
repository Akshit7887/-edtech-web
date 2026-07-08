using Microsoft.AspNetCore.SignalR;

namespace EdTechApi.Hubs;

public class ExamHub : Hub
{
    public async Task JoinExamGroup(int examId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, "exam_" + examId);
    }

    public async Task LeaveExamGroup(int examId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, "exam_" + examId);
    }
}
