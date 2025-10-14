using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services;

[Authorize]
public class NotificationHub(ILogger<NotificationHub> logger) : Hub {
    public override async Task OnConnectedAsync() {
        var userId = Context.UserIdentifier;
        logger.LogInformation("User {UserId} connected to NotificationHub. ConnectionId: {ConnectionId}", userId, Context.ConnectionId);
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception) {
        var userId = Context.UserIdentifier;
        if (exception != null) {
            logger.LogWarning(exception, "User {UserId} disconnected from NotificationHub with error. ConnectionId: {ConnectionId}", userId, Context.ConnectionId);
        }
        else {
            logger.LogInformation("User {UserId} disconnected from NotificationHub. ConnectionId: {ConnectionId}", userId, Context.ConnectionId);
        }

        await base.OnDisconnectedAsync(exception);
    }
}
