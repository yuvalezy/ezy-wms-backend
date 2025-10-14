using Core.DTOs.Transfer;
using Core.Entities;
using Core.Enums;
using Core.Interfaces;
using Core.Models;
using Core.Services;
using Infrastructure.DbContexts;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Services;

public class TransferService(
    SystemDbContext db,
    IExternalSystemAdapter adapter,
    ITransferPackageService transferPackageService,
    ISettings settings,
    IExternalSystemAlertService alertService,
    IWmsAlertService wmsAlertService,
    IUserService userService) : ITransferService {
    public async Task<TransferResponse> CreateTransfer(CreateTransferRequest request, SessionInfo sessionInfo) {
        var now = DateTime.UtcNow.Date;
        var transfer = new Transfer {
            Name = request.Name,
            CreatedByUserId = sessionInfo.Guid,
            Comments = request.Comments,
            Date = now,
            Status = ObjectStatus.Open,
            WhsCode = sessionInfo.Warehouse,
            TargetWhsCode = request.TargetWhsCode ?? sessionInfo.Warehouse,
            Lines = []
        };

        await db.Transfers.AddAsync(transfer);
        await db.SaveChangesAsync();
        return TransferResponse.FromTransfer(transfer);
    }

    public async Task<TransferResponse> GetTransfer(Guid id, bool progress = false) {
        var query = db.Transfers.AsQueryable();

        if (progress) {
            query = query.Include(t => t.Lines.Where(l => l.LineStatus != LineStatus.Closed));
        }

        query = query.Include(t => t.CreatedByUser);

        var transfer = await query.FirstOrDefaultAsync(t => t.Id == id);
        if (transfer == null) {
            throw new KeyNotFoundException($"Transfer with ID {id} not found.");
        }

        return GetTransferResponse(progress, transfer);
    }

    public async Task<IEnumerable<TransferResponse>> GetTransfers(TransfersRequest request, string warehouse) {
        var query = db.Transfers
        .Include(t => t.CreatedByUser)
        .Where(t => t.WhsCode == warehouse)
        .AsQueryable();

        // Apply filters
        if (request.Date.HasValue) {
            query = query.Where(t => t.Date == request.Date.Value.Date);
        }

        if (request.Status?.Length > 0) {
            query = query.Where(t => request.Status.Contains(t.Status));
        }

        if (request.ID.HasValue) {
            // Assuming ID is some sort of display ID, not the GUID
            // You may need to adjust this based on your business logic
            query = query.Where(t => t.Id == new Guid(request.ID.Value.ToString()));
        }

        if (request.Number is > 0) {
            query = query.Where(t => t.Number == request.Number);
        }

        // Include lines for progress calculation if requested
        if (request.Progress) {
            query = query.Include(t => t.Lines.Where(l => l.LineStatus != LineStatus.Closed));
        }

        // Apply ordering
        switch (request.OrderBy) {
            case TransferOrderBy.Date:
                query = request.Desc
                ? query.OrderByDescending(t => t.Date).ThenByDescending(t => t.Id)
                : query.OrderBy(t => t.Date).ThenBy(t => t.Id);

                break;
            case TransferOrderBy.ID:
            default:
                query = request.Desc
                ? query.OrderByDescending(t => t.Id)
                : query.OrderBy(t => t.Id);

                break;
        }


        var transfers = await query.ToListAsync();

        return transfers.Select(transfer => GetTransferResponse(request.Progress, transfer)).ToList();
    }

    private static TransferResponse GetTransferResponse(bool progress, Transfer transfer) {
        var response = TransferResponse.FromTransfer(transfer);

        if (progress && transfer.Lines.Any()) {
            decimal sourceQuantity = transfer.Lines
            .Where(l => l.Type == SourceTarget.Source && l.LineStatus != LineStatus.Closed)
            .Sum(l => l.Quantity);

            decimal targetQuantity = transfer.Lines
            .Where(l => l.Type == SourceTarget.Target && l.LineStatus != LineStatus.Closed)
            .Sum(l => l.Quantity);

            response.Progress = sourceQuantity > 0 ? (int?)((targetQuantity * 100) / sourceQuantity) : 0;
        }

        return response;
    }

    public async Task<TransferResponse> GetProcessInfo(Guid id) {
        var transfer = await GetTransfer(id, true);

        bool hasIncompleteItems = await db.TransferLines
        .Where(l => l.TransferId == id && l.LineStatus != LineStatus.Closed)
        .GroupBy(l => l.ItemCode)
        .AnyAsync(g => g.Where(l => l.Type == SourceTarget.Source).Sum(l => l.Quantity) !=
                       g.Where(l => l.Type == SourceTarget.Target).Sum(l => l.Quantity));

        bool hasItems = await db.TransferLines
        .AnyAsync(l => l.TransferId == id && l.LineStatus != LineStatus.Closed);

        transfer.IsComplete = !hasIncompleteItems && hasItems;

        return transfer;
    }

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
            var result = await adapter.ProcessTransfer(transfer.Number, transfer.WhsCode, transfer.Comments, transferData, alertRecipients);

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

    public async Task<IEnumerable<TransferContentResponse>> GetTransferContent(TransferContentRequest request) {
        var transferLines = db.TransferLines
        .Include(tl => tl.Transfer)
        .Where(tl => tl.TransferId == request.ID && tl.LineStatus != LineStatus.Closed);

        // Apply filters based on request
        if (request.BinEntry.HasValue && request.BinEntry > 0) {
            transferLines = transferLines.Where(tl => tl.BinEntry == request.BinEntry.Value);
        }

        if (!string.IsNullOrWhiteSpace(request.ItemCode)) {
            transferLines = transferLines.Where(tl => tl.ItemCode == request.ItemCode);
        }

        transferLines = transferLines.Where(tl => tl.Type == request.Type);

        var lines = await transferLines.ToListAsync();

        // Group by ItemCode and aggregate data
        var groupedLines = lines
        .GroupBy(tl => tl.ItemCode)
        .Select(g => new {
            ItemCode = g.Key,
            Lines = g.ToList()
        })
        .ToList();

        var result = new List<TransferContentResponse>();

        foreach (var group in groupedLines) {
            var firstLine = group.Lines.First();

            // Get item information from external adapter
            var itemInfo = await adapter.ItemCheckAsync(group.ItemCode, null);
            var item = itemInfo.FirstOrDefault();

            var content = new TransferContentResponse {
                ItemCode = group.ItemCode,
                ItemName = item?.ItemName ?? "",
                Quantity = group.Lines.Sum(l => l.Quantity),
                NumInBuy = item?.NumInBuy ?? 1,
                BuyUnitMsr = item?.BuyUnitMsr ?? "",
                PurPackUn = item?.PurPackUn ?? 1,
                PurPackMsr = item?.PurPackMsr ?? "",
                Factor1 = item?.Factor1 ?? 1,
                Factor2 = item?.Factor2 ?? 2,
                Factor3 = item?.Factor3 ?? 3,
                Factor4 = item?.Factor4 ?? 4,
                CustomFields = item?.CustomFields,
                Unit = firstLine.UnitType
            };

            if (request.Type == SourceTarget.Target) {
                // Calculate progress and open quantity for target
                var allItemLines = await db.TransferLines
                .Where(tl => tl.TransferId == request.ID &&
                             tl.ItemCode == group.ItemCode &&
                             tl.LineStatus != LineStatus.Closed)
                .ToListAsync();

                var sourceQuantity = allItemLines.Where(l => l.Type == SourceTarget.Source).Sum(l => l.Quantity);
                var targetQuantity = allItemLines.Where(l => l.Type == SourceTarget.Target).Sum(l => l.Quantity);

                content.Progress = sourceQuantity > 0 ? (int?)((targetQuantity * 100) / sourceQuantity) : 0;
                content.OpenQuantity = sourceQuantity - targetQuantity;

                if (request.TargetBinQuantity && request.BinEntry.HasValue) {
                    content.BinQuantity = (int?)group.Lines
                    .Where(l => l.BinEntry == request.BinEntry.Value)
                    .Sum(l => l.Quantity);
                }
            }

            result.Add(content);
        }

        // Add bin information if requested
        if (request.Type == SourceTarget.Target && request.TargetBins) {
            await AddBinInformation(result, request.ID);
        }

        return result.OrderBy(r => r.ItemCode);
    }

    private async Task AddBinInformation(List<TransferContentResponse> contents, Guid transferId) {
        var binData = await db.TransferLines
        .Where(tl => tl.TransferId == transferId &&
                     tl.Type == SourceTarget.Target &&
                     tl.LineStatus != LineStatus.Closed &&
                     tl.BinEntry.HasValue)
        .GroupBy(tl => new { tl.ItemCode, tl.BinEntry })
        .Select(g => new {
            ItemCode = g.Key.ItemCode,
            BinEntry = g.Key.BinEntry!.Value,
            Quantity = g.Sum(l => l.Quantity)
        })
        .ToListAsync();

        // Get bin codes from external adapter
        var binEntries = binData.Select(b => b.BinEntry).Distinct().ToList();
        var binInfoTasks = binEntries.Select(async binEntry =>
        {
            var binContents = await adapter.BinCheckAsync(binEntry);
            return new { BinEntry = binEntry, BinCode = binContents.FirstOrDefault()?.ItemCode ?? binEntry.ToString() };
        });

        var binInfos = await Task.WhenAll(binInfoTasks);
        var binCodeLookup = binInfos.ToDictionary(b => b.BinEntry, b => b.BinCode);

        foreach (var content in contents) {
            var itemBins = binData.Where(b => b.ItemCode == content.ItemCode).ToList();
            if (itemBins.Any()) {
                content.Bins = itemBins.Select(b => new TransferContentBin {
                    Entry = b.BinEntry,
                    Code = binCodeLookup.GetValueOrDefault(b.BinEntry, b.BinEntry.ToString()),
                    Quantity = b.Quantity
                }).ToList();
            }
        }
    }

    public async Task<IEnumerable<TransferContentTargetDetailResponse>> GetTransferContentTargetDetail(TransferContentTargetDetailRequest request) {
        var query = db.TransferLines
        .Include(tl => tl.CreatedByUser)
        .Where(tl => tl.TransferId == request.ID &&
                     tl.ItemCode == request.ItemCode &&
                     tl.Type == SourceTarget.Target &&
                     tl.LineStatus != LineStatus.Closed);

        if (request.BinEntry.HasValue) {
            query = query.Where(tl => tl.BinEntry == request.BinEntry.Value);
        }

        var lines = await query
        .OrderBy(tl => tl.Date)
        .ToListAsync();

        return lines.Select(line => new TransferContentTargetDetailResponse {
            LineId = line.Id,
            CreatedByName = line.CreatedByUser?.FullName ?? "Unknown",
            TimeStamp = line.Date,
            Quantity = line.Quantity
        });
    }

    public async Task UpdateContentTargetDetail(TransferUpdateContentTargetDetailRequest request, SessionInfo sessionInfo) {
        var transaction = await db.Database.BeginTransactionAsync();
        try {
            // Handle quantity changes
            if (request.QuantityChanges?.Any() == true) {
                foreach (var change in request.QuantityChanges) {
                    var line = await db.TransferLines.FindAsync(change.Key);
                    if (line == null) continue;

                    // Validate line belongs to transfer and is not closed
                    if (line.TransferId != request.ID || line.LineStatus == LineStatus.Closed) continue;

                    // Use existing UpdateLine validation logic
                    var updateRequest = new TransferUpdateLineRequest {
                        Id = request.ID,
                        LineId = change.Key,
                        Quantity = change.Value
                    };

                    // Note: This would ideally reuse the existing UpdateLine validation
                    // For now, we'll do basic validation
                    line.Quantity = change.Value;
                    line.UpdatedAt = DateTime.UtcNow;
                    line.UpdatedByUserId = sessionInfo.Guid;
                }
            }

            // Handle line removal
            if (request.RemoveRows?.Any() == true) {
                foreach (var lineId in request.RemoveRows) {
                    var line = await db.TransferLines.FindAsync(lineId);
                    if (line == null) continue;

                    // Validate line belongs to transfer and is not already closed
                    if (line.TransferId != request.ID || line.LineStatus == LineStatus.Closed) continue;

                    line.LineStatus = LineStatus.Closed;
                    line.UpdatedAt = DateTime.UtcNow;
                    line.UpdatedByUserId = sessionInfo.Guid;
                }
            }

            await db.SaveChangesAsync();
            await transaction.CommitAsync();
        }
        catch {
            await transaction.RollbackAsync();
            throw;
        }
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