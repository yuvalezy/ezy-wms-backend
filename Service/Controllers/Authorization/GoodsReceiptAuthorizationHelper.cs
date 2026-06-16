using System;
using System.Linq;
using Core.Enums;
using Core.Interfaces;
using Core.Models;

namespace Service.Controllers.Authorization;

internal static class GoodsReceiptAuthorizationHelper {
    internal static RoleType[] GetRequiredRole(GoodsReceiptType type, bool supervisorOnly, ISettings settings) {
        switch (type) {
            case GoodsReceiptType.All:
                if (supervisorOnly)
                    return [RoleType.GoodsReceiptSupervisor, RoleType.GoodsReceiptConfirmationSupervisor, RoleType.TransferConfirmationSupervisor];
                else
                    return [RoleType.GoodsReceipt, RoleType.GoodsReceiptSupervisor, RoleType.GoodsReceiptConfirmation, RoleType.GoodsReceiptConfirmationSupervisor, RoleType.TransferConfirmation, RoleType.TransferConfirmationSupervisor];
            case GoodsReceiptType.SpecificTransfers:
                if (supervisorOnly)
                    return [RoleType.TransferConfirmationSupervisor];
                else if (settings.Options.GoodsReceiptCreateSupervisorRequired)
                    return [RoleType.TransferConfirmation];
                else
                    return [RoleType.TransferConfirmation, RoleType.TransferConfirmationSupervisor];
            case GoodsReceiptType.SpecificReceipts:
                if (supervisorOnly)
                    return [RoleType.GoodsReceiptConfirmationSupervisor];
                else if (settings.Options.GoodsReceiptCreateSupervisorRequired)
                    return [RoleType.GoodsReceiptConfirmation];
                else
                    return [RoleType.GoodsReceiptConfirmation, RoleType.GoodsReceiptConfirmationSupervisor];
            case GoodsReceiptType.SpecificOrders:
                if (supervisorOnly)
                    return [RoleType.GoodsReceiptSupervisor];
                else if (settings.Options.GoodsReceiptCreateSupervisorRequired)
                    return [RoleType.GoodsReceipt];
                else
                    return [RoleType.GoodsReceipt, RoleType.GoodsReceiptSupervisor];
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    internal static bool HasRequiredRole(SessionInfo sessionInfo, GoodsReceiptType type, bool supervisorOnly, ISettings settings) {
        if (sessionInfo.SuperUser) return true;

        var requiredRole = GetRequiredRole(type, supervisorOnly, settings);
        if (sessionInfo.Roles.Any(r => requiredRole.Contains(r)))
            return true;

        // Check if user has supervisor role when non-supervisor role is required
        if (!supervisorOnly) {
            var supervisorRole = type switch {
                GoodsReceiptType.SpecificTransfers => RoleType.TransferConfirmationSupervisor,
                GoodsReceiptType.SpecificReceipts => RoleType.GoodsReceiptConfirmationSupervisor,
                _ => RoleType.GoodsReceiptSupervisor
            };

            return sessionInfo.Roles.Contains(supervisorRole);
        }

        return false;
    }
}
