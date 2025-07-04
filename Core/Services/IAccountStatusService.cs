using Core.Entities;
using Core.Enums;

namespace Core.Services;

public interface IAccountStatusService {
    Task<AccountStatus>           GetCurrentAccountStatusAsync();
    Task<AccountStatus>           UpdateAccountStatusAsync(AccountState newStatus, string reason);
    Task<bool>                    IsAccountActiveAsync();
    Task<bool>                    IsPaymentDueAsync();
    Task<bool>                    IsSystemAccessAllowedAsync();
    Task<List<AccountStatusAudit>> GetStatusHistoryAsync();
    Task                          ProcessAccountStatusTransitionAsync();
}