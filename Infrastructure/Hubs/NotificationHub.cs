using Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services;

[Authorize]
public class NotificationHub(
    ILogger<NotificationHub> logger,
    IPresenceTracker presenceTracker,
    IHubContext<NotificationHub> hubContext) : Hub {

    public override async Task OnConnectedAsync() {
        var userId = Context.UserIdentifier;
        logger.LogInformation("User {UserId} connected to NotificationHub. ConnectionId: {ConnectionId}", userId, Context.ConnectionId);

        if (!string.IsNullOrEmpty(userId)) {
            await presenceTracker.UserConnectedAsync(userId);

            // Broadcast to all clients that this user is now online
            await hubContext.Clients.All.SendAsync("UserConnected", userId);
        }

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

        if (!string.IsNullOrEmpty(userId)) {
            await presenceTracker.UserDisconnectedAsync(userId);

            // Check if user is still online (may have other connections)
            var isStillOnline = await presenceTracker.IsUserOnlineAsync(userId);

            // Only broadcast disconnect if user has no more active connections
            if (!isStillOnline) {
                await hubContext.Clients.All.SendAsync("UserDisconnected", userId);
            }
        }

        await base.OnDisconnectedAsync(exception);
    }

    /// <summary>
    /// Get list of all currently online user IDs
    /// </summary>
    public async Task<string[]> GetOnlineUsers() {
        return await presenceTracker.GetOnlineUsersAsync();
    }
}
