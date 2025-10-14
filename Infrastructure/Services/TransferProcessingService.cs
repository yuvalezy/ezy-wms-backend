using Core.DTOs.Transfer;
using Core.Entities;
using Core.Enums;
using Core.Interfaces;
using Core.Models;
using Core.Services;
using Infrastructure.DbContexts;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Services;

public class TransferProcessingService(
    SystemDbContext db,
    IExternalSystemAdapter adapter,
    ITransferPackageService transferPackageService,
    ISettings settings,
    IExternalSystemAlertService alertService,
    IWmsAlertService wmsAlertService,
    IUserService userService) : ITransferProcessingService {
    public async Task<bool> CancelTransfer(Guid id, SessionInfo sessionInfo) {
        var transfer = await db.Transfers.FindAsync(id);
        if (transfer == null) {
            throw new KeyNotFoundException($"Transfer with ID {id} not found.");
        }

        if (transfer.Status != ObjectStatus.Open && transfer.Status != ObjectStatus.InProgress) {
            throw new InvalidOperationException("Cannot cancel transfer if the Status is not Open or In Progress");
        }

        // Update transfer status
        transfer.Status = ObjectStatus.Cancelled;
        transfer.UpdatedAt = DateTime.UtcNow;
        transfer.UpdatedByUserId = sessionInfo.Guid;

        // Update all open lines to cancelled
        var openLines = await db.TransferLines
        .Where(tl => tl.TransferId == id && tl.LineStatus != LineStatus.Closed)
        .ToListAsync();

        foreach (var line in openLines) {
            line.LineStatus = LineStatus.Closed;
            line.UpdatedAt = DateTime.UtcNow;
            line.UpdatedByUserId = sessionInfo.Guid;
        }

        // Clear package commitments if package feature is enabled
        if (settings.Options.EnablePackages) {
            await transferPackageService.ClearTransferCommitmentsAsync(id, sessionInfo);
        }

        await db.SaveChangesAsync();
        return true;
    }

    public async Task<ProcessTransferResponse> ProcessTransfer(Guid id, SessionInfo sessionInfo) {
        var transaction = await db.Database.BeginTransactionAsync();
        int? transferEntry = null;
        try {
            var transfer = await db.Transfers
            .Include(t => t.Lines.Where(l => l.LineStatus != LineStatus.Closed))
            .FirstOrDefaultAsync(t => t.Id == id);

            if (transfer == null) {
                throw new KeyNotFoundException($"Transfer with ID {id} not found.");
            }

            if (transfer.Status != ObjectStatus.InProgress) {
                throw new InvalidOperationException("Cannot process transfer if the Status is not In Progress");
            }

            // Check if this is a cross-warehouse transfer requiring approval
            bool isCrossWarehouse = transfer.TargetWhsCode != null && transfer.TargetWhsCode != transfer.WhsCode;
            bool isSupervisor = sessionInfo.Roles.Contains(RoleType.TransferSupervisor) || sessionInfo.SuperUser;

            if (settings.Options.EnableWarehouseTransfer && isCrossWarehouse && !isSupervisor) {
                // Non-supervisor attempting cross-warehouse transfer - require approval
                transfer.Status = ObjectStatus.WaitingForApproval;
                transfer.UpdatedAt = DateTime.UtcNow;
                transfer.UpdatedByUserId = sessionInfo.Guid;
                await db.SaveChangesAsync();

                // Get supervisors for the source warehouse
                var supervisors = await userService.GetUsersByRoleAndWarehouseAsync(
                    RoleType.TransferSupervisor,
                    transfer.WhsCode
                );

                // Create alert for each supervisor
                foreach (var supervisor in supervisors) {
                    await wmsAlertService.CreateAlertAsync(
                        supervisor.Id,
                        WmsAlertType.TransferApprovalRequest,
                        WmsAlertObjectType.Transfer,
                        transfer.Id,
                        "Transfer Approval Request",
                        $"{sessionInfo.Name} has requested approval for cross-warehouse transfer #{transfer.Number} from {transfer.WhsCode} to {transfer.TargetWhsCode}",
                        null,
                        $"/transfer/approve/{transfer.Id}"
                    );
                }

                await transaction.CommitAsync();

                return new ProcessTransferResponse {
                    Success = true,
                    Message = "Transfer submitted for approval"
                };
            }

            // Update transfer status to Processing
            transfer.Status = ObjectStatus.Processing;
            transfer.UpdatedAt = DateTime.UtcNow;
            transfer.UpdatedByUserId = sessionInfo.Guid;
            await db.SaveChangesAsync();

            // Prepare data for SAP B1 transfer creation
            var transferData = await PrepareTransferData(id);

            // Get alert recipients
            var alertRecipients = await alertService.GetAlertRecipientsAsync(AlertableObjectType.Transfer);

            // Call external system to create the transfer in SAP B1
            var result = await adapter.ProcessTransfer(transfer.Number, transfer.WhsCode, transfer.TargetWhsCode, transfer.Comments, transferData, alertRecipients);

            if (result.Success) {
                transferEntry = result.ExternalEntry;
                // Move packages if package feature is enabled
                if (settings.Options.EnablePackages) {
                    await transferPackageService.MovePackagesOnTransferProcessAsync(id, sessionInfo);

                    // Clear package commitments since transfer is now complete
                    await transferPackageService.ClearTransferCommitmentsAsync(id, sessionInfo);
                }

                // Update transfer status to Finished
                transfer.Status = ObjectStatus.Finished;
                transfer.UpdatedAt = DateTime.UtcNow;
                transfer.UpdatedByUserId = sessionInfo.Guid;

                // Update all open lines to Finished
                var openLines = await db.TransferLines
                .Where(tl => tl.TransferId == id && tl.LineStatus != LineStatus.Closed)
                .ToListAsync();

                foreach (var line in openLines) {
                    line.LineStatus = LineStatus.Finished;
                    line.UpdatedAt = DateTime.UtcNow;
                    line.UpdatedByUserId = sessionInfo.Guid;
                }

                await db.SaveChangesAsync();
            }
            else {
                throw new InvalidOperationException(result.ErrorMessage ?? "Unknown error");
            }

            await transaction.CommitAsync();
            return result;
        }
        catch (Exception ex) {
            if (transferEntry.HasValue) {
                await adapter.Canceltransfer(transferEntry.Value);
            }

            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task<Dictionary<string, TransferCreationDataResponse>> PrepareTransferData(Guid transferId) {
        var lines = await db.TransferLines
        .Where(tl => tl.TransferId == transferId && tl.LineStatus != LineStatus.Closed)
        .GroupBy(tl => tl.ItemCode)
        .Select(g => new {
            ItemCode = g.Key,
            Lines = g.ToList()
        })
        .ToListAsync();

        var transferData = new Dictionary<string, TransferCreationDataResponse>();

        foreach (var itemGroup in lines) {
            // Group source bins
            var sourceBins = itemGroup.Lines
            .Where(l => l is { Type: SourceTarget.Source, BinEntry: not null })
            .GroupBy(l => l.BinEntry.Value)
            .Select(g => new TransferCreationBinResponse {
                BinEntry = g.Key,
                Quantity = g.Sum(l => l.Quantity)
            })
            .ToList();

            // Group target bins
            var targetBins = itemGroup.Lines
            .Where(l => l is { Type: SourceTarget.Target, BinEntry: not null })
            .GroupBy(l => l.BinEntry.Value)
            .Select(g => new TransferCreationBinResponse {
                BinEntry = g.Key,
                Quantity = g.Sum(l => l.Quantity)
            })
            .ToList();

            // Calculate the transfer quantity - should be the source quantity (what we're transferring)
            decimal sourceQuantity = sourceBins.Sum(s => s.Quantity);
            decimal targetQuantity = targetBins.Sum(t => t.Quantity);

            // Use the maximum of source or target as the line quantity
            // In a proper transfer, these should be equal
            decimal transferQuantity = Math.Max(sourceQuantity, targetQuantity);

            var data = new TransferCreationDataResponse {
                ItemCode = itemGroup.ItemCode,
                Quantity = transferQuantity,
                SourceBins = sourceBins,
                TargetBins = targetBins
            };

            transferData[itemGroup.ItemCode] = data;
        }

        return transferData;
    }

    public async Task<CreateTransferRequestResponse> CreateTransferRequest(CreateTransferRequestRequest request, SessionInfo sessionInfo) {
        // This would typically integrate with a transfer request creation system
        // For now, we'll create a basic transfer based on the content
        try {
            var transfer = new Transfer {
                Name = $"Transfer Request {DateTime.UtcNow:yyyyMMdd-HHmmss}",
                CreatedByUserId = sessionInfo.Guid,
                Comments = "Created from transfer request",
                Date = DateTime.UtcNow.Date,
                Status = ObjectStatus.Open,
                WhsCode = sessionInfo.Warehouse,
                Lines = new List<TransferLine>()
            };

            // Add lines based on content
            foreach (var content in request.Contents) {
                var line = new TransferLine {
                    ItemCode = content.ItemCode,
                    BarCode = content.Barcode,
                    Date = DateTime.UtcNow,
                    Quantity = content.Quantity,
                    Type = SourceTarget.Source,
                    UnitType = content.Unit,
                    CreatedAt = DateTime.UtcNow,
                    CreatedByUserId = sessionInfo.Guid,
                    LineStatus = LineStatus.Open,
                };

                transfer.Lines.Add(line);
            }

            db.Transfers.Add(transfer);
            await db.SaveChangesAsync();

            return new CreateTransferRequestResponse {
                Number = transfer.Number,
                Success = true
            };
        }
        catch (Exception ex) {
            return new CreateTransferRequestResponse {
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }

    public async Task<ProcessTransferResponse> ApproveTransferRequest(TransferApprovalRequest request, SessionInfo sessionInfo) {
        var transaction = await db.Database.BeginTransactionAsync();
        try {
            var transfer = await db.Transfers
                .Include(t => t.CreatedByUser)
                .Include(t => t.Lines.Where(l => l.LineStatus != LineStatus.Closed))
                .FirstOrDefaultAsync(t => t.Id == request.TransferId);

            if (transfer == null) {
                throw new KeyNotFoundException($"Transfer with ID {request.TransferId} not found.");
            }

            if (transfer.Status != ObjectStatus.WaitingForApproval) {
                throw new InvalidOperationException("Cannot approve/reject transfer that is not waiting for approval");
            }

            // Verify user has supervisor role
            bool isSupervisor = sessionInfo.Roles.Contains(RoleType.TransferSupervisor) || sessionInfo.SuperUser;
            if (!isSupervisor) {
                throw new UnauthorizedAccessException("Only supervisors can approve or reject transfer requests");
            }

            if (request.Approved) {
                // Approval: Change status to InProgress and process the transfer
                transfer.Status = ObjectStatus.InProgress;
                transfer.UpdatedAt = DateTime.UtcNow;
                transfer.UpdatedByUserId = sessionInfo.Guid;
                await db.SaveChangesAsync();

                // Mark all approval request alerts for this transfer as read
                var approvalAlerts = await db.WmsAlerts
                    .Where(a => a.ObjectId == transfer.Id &&
                                a.AlertType == WmsAlertType.TransferApprovalRequest &&
                                !a.IsRead)
                    .ToListAsync();

                foreach (var alert in approvalAlerts) {
                    alert.IsRead = true;
                    alert.ReadAt = DateTime.UtcNow;
                }

                await db.SaveChangesAsync();
                await transaction.CommitAsync();

                // Process the transfer (this will create a new transaction internally)
                var processResult = await ProcessTransfer(request.TransferId, sessionInfo);

                // Notify the requester
                if (transfer.CreatedByUserId.HasValue) {
                    await wmsAlertService.CreateAlertAsync(
                        transfer.CreatedByUserId.Value,
                        WmsAlertType.TransferApproved,
                        WmsAlertObjectType.Transfer,
                        transfer.Id,
                        "Transfer Approved",
                        $"Your transfer request #{transfer.Number} from {transfer.WhsCode} to {transfer.TargetWhsCode} has been approved by {sessionInfo.Name}",
                        null,
                        $"/transfer/process/{transfer.Id}"
                    );
                }

                return processResult;
            }
            else {
                // Rejection: Change status to Cancelled
                transfer.Status = ObjectStatus.Cancelled;
                transfer.UpdatedAt = DateTime.UtcNow;
                transfer.UpdatedByUserId = sessionInfo.Guid;

                // Update all open lines to cancelled
                foreach (var line in transfer.Lines) {
                    line.LineStatus = LineStatus.Closed;
                    line.UpdatedAt = DateTime.UtcNow;
                    line.UpdatedByUserId = sessionInfo.Guid;
                }

                // Mark all approval request alerts for this transfer as read
                var approvalAlerts = await db.WmsAlerts
                    .Where(a => a.ObjectId == transfer.Id &&
                                a.AlertType == WmsAlertType.TransferApprovalRequest &&
                                !a.IsRead)
                    .ToListAsync();

                foreach (var alert in approvalAlerts) {
                    alert.IsRead = true;
                    alert.ReadAt = DateTime.UtcNow;
                }

                await db.SaveChangesAsync();
                await transaction.CommitAsync();

                // Notify the requester
                string rejectionMessage = $"Your transfer request #{transfer.Number} from {transfer.WhsCode} to {transfer.TargetWhsCode} has been rejected by {sessionInfo.Name}";
                if (!string.IsNullOrWhiteSpace(request.RejectionReason)) {
                    rejectionMessage += $". Reason: {request.RejectionReason}";
                }

                if (transfer.CreatedByUserId.HasValue) {
                    await wmsAlertService.CreateAlertAsync(
                        transfer.CreatedByUserId.Value,
                        WmsAlertType.TransferRejected,
                        WmsAlertObjectType.Transfer,
                        transfer.Id,
                        "Transfer Rejected",
                        rejectionMessage,
                        null,
                        $"/transfer/{transfer.Id}"
                    );
                }

                return new ProcessTransferResponse {
                    Success = false,
                    Message = "Transfer rejected"
                };
            }
        }
        catch (Exception ex) {
            await transaction.RollbackAsync();
            throw;
        }
    }
}
