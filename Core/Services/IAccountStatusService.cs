using Core.Entities;
using Core.Enums;
using Core.Models;

namespace Core.Services;

public interface IAccountStatusService {
    Task<AccountStatus>           GetCurrentAccountStatusAsync();
    Task<AccountStatus>           UpdateAccountStatusAsync(AccountState newStatus, string reason);
    /// <summary>
    /// Mirrors a cloud validate-account response onto the singleton AccountStatus
    /// entity — status AND the date fields (expiry / payment cycle / demo expiry) —
    /// so the local countdown and auto-transition logic can see them. Unlike
    /// <see cref="UpdateAccountStatusAsync"/>, this always refreshes the dates even
    /// when the status itself is unchanged (e.g. Demo → Demo).
    /// </summary>
    Task<AccountStatus>           SyncFromCloudAsync(LicenseData licenseData, string reason);
    Task<bool>                    IsAccountActiveAsync();
    Task<bool>                    IsPaymentDueAsync();
    Task<bool>                    IsSystemAccessAllowedAsync();
    Task<List<AccountStatusAudit>> GetStatusHistoryAsync();
    Task                          ProcessAccountStatusTransitionAsync();
}