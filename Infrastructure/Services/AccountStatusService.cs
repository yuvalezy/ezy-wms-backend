using Core.Entities;
using Core.Enums;
using Core.Services;
using Infrastructure.DbContexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services;

public class AccountStatusService(SystemDbContext context, ILogger<AccountStatusService> logger) : IAccountStatusService {
    public async Task<AccountStatus> GetCurrentAccountStatusAsync() {
        var accountStatus = await context.AccountStatus.FirstOrDefaultAsync(a => a.Id == 1);
        if (accountStatus == null) {
            accountStatus = CreateInitialAccountStatus();
            context.AccountStatus.Add(accountStatus);
            await context.SaveChangesAsync();
        }
        return accountStatus;
    }

    public async Task<AccountStatus> UpdateAccountStatusAsync(AccountState newStatus, string reason) {
        var current = await GetCurrentAccountStatusAsync();

        if (current.Status == newStatus) {
            logger.LogInformation("Account status already {Status}", newStatus);
            return current;
        }

        var previousStatus = current.Status;
        current.Status                  = newStatus;
        current.UpdatedAt               = DateTime.UtcNow;
        current.LastValidationTimestamp = DateTime.UtcNow;

        // Update specific fields based on new status
        switch (newStatus) {
            case AccountState.PaymentDue:
                if (current.PaymentCycleDate.HasValue && current.PaymentCycleDate < DateTime.UtcNow) {
                    current.ExpirationDate = DateTime.UtcNow.AddDays(30); // Grace period
                }

                break;
            case AccountState.PaymentDueUnknown:
                current.ExpirationDate = DateTime.UtcNow.AddDays(7); // 7-day grace period
                break;
            case AccountState.Disabled:
                current.InactiveReason = reason ?? "Account disabled due to payment issues";
                break;
            case AccountState.DemoExpired:
                current.InactiveReason = reason ?? "Demo period expired";
                break;
        }

        await context.SaveChangesAsync();

        // Log audit record
        await LogStatusChangeAsync(previousStatus, newStatus, reason);

        logger.LogInformation("Account status changed from {PreviousStatus} to {NewStatus}: {Reason}",
            previousStatus, newStatus, reason);

        return current;
    }

    public async Task<bool> IsAccountActiveAsync() {
        var status = await GetCurrentAccountStatusAsync();
        return status.Status == AccountState.Active;
    }

    public async Task<bool> IsPaymentDueAsync() {
        var status = await GetCurrentAccountStatusAsync();
        return status.Status is AccountState.PaymentDue or AccountState.PaymentDueUnknown;
    }

    public async Task<bool> IsSystemAccessAllowedAsync() {
        var status = await GetCurrentAccountStatusAsync();
        return status.Status is AccountState.Active or AccountState.PaymentDue or AccountState.PaymentDueUnknown or AccountState.Demo;
    }

    public async Task<List<AccountStatusAudit>> GetStatusHistoryAsync() {
        return await context.AccountStatusAudits
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync();
    }

    public async Task ProcessAccountStatusTransitionAsync() {
        var status = await GetCurrentAccountStatusAsync();
        var now    = DateTime.UtcNow;

        switch (status.Status) {
            case AccountState.Active when status.PaymentCycleDate.HasValue && status.PaymentCycleDate < now:
                await UpdateAccountStatusAsync(AccountState.PaymentDue, "Payment cycle date reached");
                break;
            case AccountState.PaymentDueUnknown when status.ExpirationDate.HasValue && status.ExpirationDate < now:
                await UpdateAccountStatusAsync(AccountState.Disabled, "Grace period expired");
                break;

            case AccountState.Demo when status.DemoExpirationDate.HasValue && status.DemoExpirationDate < now:
                await UpdateAccountStatusAsync(AccountState.DemoExpired, "Demo period expired");
                break;
        }
    }

    private AccountStatus CreateInitialAccountStatus() {
        var now = DateTime.UtcNow;
        return new AccountStatus {
            Id                      = 1,
            Status                  = AccountState.Invalid,
            LastValidationTimestamp = now,
            CreatedAt              = now,
            UpdatedAt              = now
        };
    }

    private async Task LogStatusChangeAsync(AccountState previousStatus, AccountState newStatus, string? reason) {
        var audit = new AccountStatusAudit {
            PreviousStatus  = previousStatus,
            NewStatus       = newStatus,
            Reason          = reason
            // CreatedByUserId will be null for system actions
        };

        context.AccountStatusAudits.Add(audit);
        await context.SaveChangesAsync();
    }
}