using Core.DTOs.Alerts;
using Core.Entities;
using Core.Enums;
using Core.Interfaces;
using Infrastructure.DbContexts;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services;

public class WmsAlertService(
    SystemDbContext context,
    IHubContext<NotificationHub> hubContext,
    ILogger<WmsAlertService> logger) : IWmsAlertService {

    public async Task<WmsAlert> CreateAlertAsync(
        Guid userId,
        WmsAlertType alertType,
        WmsAlertObjectType objectType,
        Guid objectId,
        string title,
        string message,
        string? actionUrl,
        string? data = null) {

        var alert = new WmsAlert {
            UserId = userId,
            AlertType = alertType,
            ObjectType = objectType,
            ObjectId = objectId,
            Title = title,
            Message = message,
            ActionUrl = actionUrl,
            Data = data,
            IsRead = false,
            CreatedAt = DateTime.UtcNow
        };

        context.WmsAlerts.Add(alert);
        await context.SaveChangesAsync();

        logger.LogInformation("Created WmsAlert {AlertId} for user {UserId}: {Title}", alert.Id, userId, title);

        // Send real-time notification via SignalR
        try {
            var alertResponse = MapToResponse(alert);
            await hubContext.Clients.User(userId.ToString()).SendAsync("ReceiveAlert", alertResponse);
            logger.LogDebug("Sent SignalR notification for alert {AlertId} to user {UserId}", alert.Id, userId);
        }
        catch (Exception ex) {
            logger.LogWarning(ex, "Failed to send SignalR notification for alert {AlertId}", alert.Id);
            // Don't throw - alert is still saved in database
        }

        return alert;
    }

    public async Task<IEnumerable<WmsAlertResponse>> GetUserAlertsAsync(Guid userId, bool unreadOnly = false, int? limit = null) {
        var query = context.WmsAlerts
            .Where(a => a.UserId == userId);

        if (unreadOnly) {
            query = query.Where(a => !a.IsRead);
        }

        query = query.OrderByDescending(a => a.CreatedAt);

        if (limit.HasValue && limit.Value > 0) {
            query = query.Take(limit.Value);
        }

        var alerts = await query.ToListAsync();
        return alerts.Select(MapToResponse);
    }

    public async Task<int> GetUnreadCountAsync(Guid userId) {
        return await context.WmsAlerts
            .Where(a => a.UserId == userId && !a.IsRead)
            .CountAsync();
    }

    public async Task MarkAsReadAsync(Guid alertId, Guid userId) {
        var alert = await context.WmsAlerts
            .FirstOrDefaultAsync(a => a.Id == alertId && a.UserId == userId);

        if (alert == null) {
            throw new KeyNotFoundException($"Alert {alertId} not found for user {userId}");
        }

        if (!alert.IsRead) {
            alert.IsRead = true;
            alert.ReadAt = DateTime.UtcNow;
            await context.SaveChangesAsync();

            logger.LogInformation("Marked alert {AlertId} as read for user {UserId}", alertId, userId);

            // Notify client of updated unread count
            try {
                var unreadCount = await GetUnreadCountAsync(userId);
                await hubContext.Clients.User(userId.ToString()).SendAsync("UnreadCountUpdate", unreadCount);
            }
            catch (Exception ex) {
                logger.LogWarning(ex, "Failed to send unread count update for user {UserId}", userId);
            }
        }
    }

    public async Task MarkAllAsReadAsync(Guid userId) {
        var unreadAlerts = await context.WmsAlerts
            .Where(a => a.UserId == userId && !a.IsRead)
            .ToListAsync();

        if (unreadAlerts.Any()) {
            var now = DateTime.UtcNow;
            foreach (var alert in unreadAlerts) {
                alert.IsRead = true;
                alert.ReadAt = now;
            }

            await context.SaveChangesAsync();
            logger.LogInformation("Marked {Count} alerts as read for user {UserId}", unreadAlerts.Count, userId);

            // Notify client
            try {
                await hubContext.Clients.User(userId.ToString()).SendAsync("UnreadCountUpdate", 0);
            }
            catch (Exception ex) {
                logger.LogWarning(ex, "Failed to send unread count update for user {UserId}", userId);
            }
        }
    }

    private static WmsAlertResponse MapToResponse(WmsAlert alert) {
        return new WmsAlertResponse {
            Id = alert.Id,
            UserId = alert.UserId,
            AlertType = alert.AlertType,
            ObjectType = alert.ObjectType,
            ObjectId = alert.ObjectId,
            Title = alert.Title,
            Message = alert.Message,
            Data = alert.Data,
            IsRead = alert.IsRead,
            ReadAt = alert.ReadAt,
            ActionUrl = alert.ActionUrl,
            CreatedAt = alert.CreatedAt
        };
    }
}
