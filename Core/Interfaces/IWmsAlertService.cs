using Core.DTOs.Alerts;
using Core.Entities;
using Core.Enums;

namespace Core.Interfaces;

public interface IWmsAlertService {
    Task<WmsAlert> CreateAlertAsync(Guid userId, WmsAlertType alertType, WmsAlertObjectType objectType, Guid objectId, string title, string message, string? actionUrl, string? data = null);
    Task<IEnumerable<WmsAlertResponse>> GetUserAlertsAsync(Guid userId, bool unreadOnly = false, int? limit = null);
    Task<int> GetUnreadCountAsync(Guid userId);
    Task MarkAsReadAsync(Guid alertId, Guid userId);
    Task MarkAllAsReadAsync(Guid userId);
}
