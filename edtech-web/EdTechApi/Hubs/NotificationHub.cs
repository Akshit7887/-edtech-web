using Microsoft.AspNetCore.SignalR;

namespace EdTechApi.Hubs;

public class NotificationHub : Hub
{
    public async Task JoinUserGroup(int userId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, "user_" + userId);
    }

    public async Task LeaveUserGroup(int userId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, "user_" + userId);
    }
}
