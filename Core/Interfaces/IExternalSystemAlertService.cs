using Core.Entities;
using Core.Enums;

namespace Core.Interfaces;

public interface IExternalSystemAlertService {
    Task<string[]> GetAlertRecipientsAsync(AlertableObjectType type);
    Task<IEnumerable<ExternalSystemAlert>> GetAlertsAsync();
    Task<ExternalSystemAlert> CreateAlertAsync(ExternalSystemAlert alert, Guid userId);
    Task<ExternalSystemAlert> UpdateAlertAsync(Guid id, bool enabled, Guid userId);
    Task DeleteAlertAsync(Guid id);
}
