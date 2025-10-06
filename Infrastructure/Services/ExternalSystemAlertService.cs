using Core.Entities;
using Core.Enums;
using Core.Interfaces;
using Infrastructure.DbContexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services;

public class ExternalSystemAlertService(
    SystemDbContext context,
    ILogger<ExternalSystemAlertService> logger) : IExternalSystemAlertService {

    public async Task<string[]> GetAlertRecipientsAsync(AlertableObjectType type) {
        try {
            var recipients = await context.ExternalSystemAlerts
                .Where(a => a.ObjectType == type && a.Enabled)
                .Select(a => a.ExternalUserId)
                .ToArrayAsync();

            logger.LogDebug("Found {Count} alert recipients for {ObjectType}", recipients.Length, type);
            return recipients;
        }
        catch (Exception ex) {
            logger.LogError(ex, "Error retrieving alert recipients for {ObjectType}", type);
            return Array.Empty<string>();
        }
    }

    public async Task<IEnumerable<ExternalSystemAlert>> GetAlertsAsync() {
        return await context.ExternalSystemAlerts
            .OrderBy(a => a.ObjectType)
            .ThenBy(a => a.ExternalUserId)
            .ToListAsync();
    }

    public async Task<ExternalSystemAlert> CreateAlertAsync(ExternalSystemAlert alert, Guid userId) {
        alert.CreatedAt = DateTime.UtcNow;
        alert.CreatedByUserId = userId;

        context.ExternalSystemAlerts.Add(alert);
        await context.SaveChangesAsync();

        logger.LogInformation("Created alert for {ObjectType} and user {ExternalUserId}", alert.ObjectType, alert.ExternalUserId);
        return alert;
    }

    public async Task<ExternalSystemAlert> UpdateAlertAsync(Guid id, bool enabled, Guid userId) {
        var alert = await context.ExternalSystemAlerts.FindAsync(id);
        if (alert == null) {
            throw new KeyNotFoundException($"Alert with ID {id} not found");
        }

        alert.Enabled = enabled;
        alert.UpdatedAt = DateTime.UtcNow;
        alert.UpdatedByUserId = userId;

        await context.SaveChangesAsync();

        logger.LogInformation("Updated alert {Id} enabled status to {Enabled}", id, enabled);
        return alert;
    }

    public async Task DeleteAlertAsync(Guid id) {
        var alert = await context.ExternalSystemAlerts.FindAsync(id);
        if (alert == null) {
            throw new KeyNotFoundException($"Alert with ID {id} not found");
        }

        context.ExternalSystemAlerts.Remove(alert);
        await context.SaveChangesAsync();

        logger.LogInformation("Deleted alert {Id} for {ObjectType} and user {ExternalUserId}", id, alert.ObjectType, alert.ExternalUserId);
    }
}
