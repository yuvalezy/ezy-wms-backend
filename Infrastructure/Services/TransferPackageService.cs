using System.ComponentModel.DataAnnotations;
using Core.DTOs.Package;
using Core.DTOs.Transfer;
using Core.Entities;
using Core.Enums;
using Core.Extensions;
using Core.Interfaces;
using Core.Models;
using Core.Services;
using Infrastructure.DbContexts;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Services;

public class TransferPackageService(
    SystemDbContext         db,
    IExternalSystemAdapter  adapter,
    IPackageService         packageService,
    IPackageContentService  packageContentService,
    IPackageLocationService packageLocationService) : ITransferPackageService {
    public async Task<TransferAddItemResponse> HandleSourcePackageScanAsync(TransferAddSourcePackageRequest request, SessionInfo sessionInfo) {
        await using var transaction = await db.Database.BeginTransactionAsync();
        try {
            // Load package by barcode
            var scannedPackage = await packageService.GetPackageAsync(request.PackageId);

            if (scannedPackage == null) {
                throw new ValidationException($"Package with id {request.PackageId} not found");
            }

            // Get package contents - need to load them through EF to track changes
            var packageContents = await db.PackageContents
                .Where(pc => pc.PackageId == request.PackageId)
                .ToListAsync();

            if (!packageContents.Any()) {
                throw new ValidationException("Package is empty");
            }

            var transfer = await db.Transfers.FindAsync(request.TransferId);
            if (transfer == null) {
                throw new KeyNotFoundException($"Transfer with ID {request.TransferId} not found.");
            }

            // Check if package is already added as source
            var existingSourcePackage = await db.TransferPackages
                .FirstOrDefaultAsync(tp => tp.TransferId == request.TransferId &&
                                           tp.PackageId == request.PackageId &&
                                           tp.Type == SourceTarget.Source);

            if (existingSourcePackage != null) {
                throw new ValidationException("Package is already added as source to this transfer");
            }

            // Create source transfer lines and update committed quantities
            var transferLines = new List<TransferLine>();
            foreach (var content in packageContents) {
                var line = new TransferLine {
                    Id              = Guid.NewGuid(),
                    ItemCode        = content.ItemCode,
                    BarCode         = content.ItemCode, // Use item code as barcode
                    BinEntry        = request.BinEntry ?? scannedPackage.BinEntry,
                    Date            = DateTime.UtcNow,
                    Quantity        = content.Quantity,
                    Type            = SourceTarget.Source,
                    UnitType        = UnitType.Unit,
                    TransferId      = request.TransferId,
                    CreatedAt       = DateTime.UtcNow,
                    CreatedByUserId = sessionInfo.Guid,
                    LineStatus      = LineStatus.Open,
                    Comments        = $"Package: {scannedPackage.Barcode}"
                };

                db.TransferLines.Add(line);
                transferLines.Add(line);

                // Update committed quantity in package content
                content.CommittedQuantity += content.Quantity;
                db.PackageContents.Update(content);

                // Create package commitment record
                var commitment = new PackageCommitment {
                    PackageId             = request.PackageId,
                    ItemCode              = content.ItemCode,
                    Quantity              = content.Quantity,
                    SourceOperationType   = ObjectType.Transfer,
                    SourceOperationId     = request.TransferId,
                    SourceOperationLineId = line.Id,
                    CommittedAt           = DateTime.UtcNow,
                    CreatedAt             = DateTime.UtcNow,
                    CreatedByUserId       = sessionInfo.Guid
                };

                db.PackageCommitments.Add(commitment);
            }

            // Create TransferPackage record to track package addition
            var transferPackage = new TransferPackage {
                TransferId      = request.TransferId,
                PackageId       = request.PackageId,
                Type            = SourceTarget.Source,
                BinEntry        = request.BinEntry ?? scannedPackage.BinEntry,
                AddedAt         = DateTime.UtcNow,
                AddedByUserId   = sessionInfo.Guid,
                CreatedAt       = DateTime.UtcNow,
                CreatedByUserId = sessionInfo.Guid
            };

            db.TransferPackages.Add(transferPackage);

            // Update transfer status
            if (transfer.Status == ObjectStatus.Open)
                transfer.Status = ObjectStatus.InProgress;

            db.Update(transfer);
            await db.SaveChangesAsync();

            // Prepare response before committing transaction
            var response = new TransferAddItemResponse {
                LinesIds = transferLines.Select(v => v.Id).ToArray(),
                IsPackageScan   = true,
                PackageId       = scannedPackage.Id,
                PackageContents = (await Task.WhenAll(packageContents.Select(async c => await c.ToDto(adapter)))).ToList()
            };

            await transaction.CommitAsync();
            return response;
        }
        catch {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task<TransferAddItemResponse> HandleTargetPackageTransferAsync(TransferAddTargetPackageRequest request, SessionInfo sessionInfo) {
        await using var transaction = await db.Database.BeginTransactionAsync();
        try {
            var package = await packageService.GetPackageAsync(request.PackageId);
            if (package == null) {
                throw new KeyNotFoundException($"Package with ID {request.PackageId} not found.");
            }

            var transfer = await db.Transfers.FindAsync(request.TransferId);
            if (transfer == null) {
                throw new KeyNotFoundException($"Transfer with ID {request.TransferId} not found.");
            }

            // Check if package was added as source first
            var sourcePackage = await db.TransferPackages
                .FirstOrDefaultAsync(tp => tp.TransferId == request.TransferId &&
                                           tp.PackageId == request.PackageId &&
                                           tp.Type == SourceTarget.Source);

            if (sourcePackage == null) {
                throw new ValidationException("Package must be added as source before it can be transferred to target");
            }

            // Check if package is already added as target
            var existingTargetPackage = await db.TransferPackages
                .FirstOrDefaultAsync(tp => tp.TransferId == request.TransferId &&
                                           tp.PackageId == request.PackageId &&
                                           tp.Type == SourceTarget.Target);

            if (existingTargetPackage != null) {
                throw new ValidationException("Package is already added as target to this transfer");
            }

            // Get all source lines for this package (created during source scan)
            var packageSourceLines = await db.TransferLines
                .Where(tl => tl.TransferId == request.TransferId &&
                             tl.Type == SourceTarget.Source &&
                             tl.Comments != null && tl.Comments.Contains(package.Barcode) &&
                             tl.LineStatus == LineStatus.Open)
                .ToListAsync();

            if (!packageSourceLines.Any()) {
                throw new ValidationException("No source lines found for this package");
            }

            // Create target lines for all package contents
            foreach (var sourceLine in packageSourceLines) {
                var targetLine = new TransferLine {
                    ItemCode        = sourceLine.ItemCode,
                    BarCode         = sourceLine.ItemCode,
                    BinEntry        = request.TargetBinEntry,
                    Date            = DateTime.UtcNow,
                    Quantity        = sourceLine.Quantity,
                    Type            = SourceTarget.Target,
                    UnitType        = UnitType.Unit,
                    TransferId      = request.TransferId,
                    CreatedAt       = DateTime.UtcNow,
                    CreatedByUserId = sessionInfo.Guid,
                    LineStatus      = LineStatus.Open,
                    Comments        = $"Package: {package.Barcode}"
                };

                db.TransferLines.Add(targetLine);
            }

            // Create TransferPackage record to track package addition
            var transferPackage = new TransferPackage {
                TransferId      = request.TransferId,
                PackageId       = request.PackageId,
                Type            = SourceTarget.Target,
                BinEntry        = request.TargetBinEntry,
                AddedAt         = DateTime.UtcNow,
                AddedByUserId   = sessionInfo.Guid,
                CreatedAt       = DateTime.UtcNow,
                CreatedByUserId = sessionInfo.Guid
            };

            db.TransferPackages.Add(transferPackage);

            // Note: Package movement will happen during ProcessTransfer when result.Success is true
            await db.SaveChangesAsync();

            // Prepare response before committing transaction
            var response = new TransferAddItemResponse {
                IsPackageTransfer = true,
                PackageId         = package.Id,
                PackageContents   = (await Task.WhenAll(package.Contents.Select(async c => await c.ToDto(adapter)))).ToList()
            };

            await transaction.CommitAsync();
            return response;
        }
        catch {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task MovePackagesOnTransferProcessAsync(Guid transferId, SessionInfo sessionInfo) {
        // Get all target packages for this transfer
        var targetPackages = await db.TransferPackages
            .Where(tp => tp.TransferId == transferId && tp.Type == SourceTarget.Target)
            .ToListAsync();

        if (targetPackages.Count == 0)
            return; // No packages to move, exit

        var transfer = await db.Transfers.FindAsync(transferId);
        if (transfer == null) {
            throw new KeyNotFoundException($"Transfer with ID {transferId} not found.");
        }

        foreach (var targetPackage in targetPackages) {
            if (targetPackage.BinEntry.HasValue) {
                await packageLocationService.MovePackageAsync(new MovePackageRequest {
                    PackageId           = targetPackage.PackageId,
                    ToWhsCode           = transfer.WhsCode,
                    ToBinEntry          = targetPackage.BinEntry,
                    UserId              = sessionInfo.Guid,
                    SourceOperationType = ObjectType.Transfer,
                    SourceOperationId   = transferId
                });
            }
        }
    }

    public async Task ClearTransferCommitmentsAsync(Guid transferId, SessionInfo sessionInfo) {
        // Get all package commitments for this transfer
        var packageCommitments = await db.PackageCommitments
            .Where(pc => pc.SourceOperationType == ObjectType.Transfer &&
                         pc.SourceOperationId == transferId)
            .ToListAsync();

        foreach (var commitment in packageCommitments) {
            // Find the corresponding package content and reduce committed quantity
            var packageContent = await db.PackageContents
                .FirstOrDefaultAsync(pc => pc.PackageId == commitment.PackageId &&
                                           pc.ItemCode == commitment.ItemCode);

            if (packageContent != null) {
                packageContent.CommittedQuantity -= commitment.Quantity;
                db.PackageContents.Update(packageContent);
            }

            // Remove the commitment record
            db.PackageCommitments.Remove(commitment);
        }

        await db.SaveChangesAsync();
    }
}