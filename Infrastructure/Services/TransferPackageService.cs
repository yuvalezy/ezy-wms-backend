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
    SystemDbContext db,
    IExternalSystemAdapter adapter,
    IPackageService packageService,
    IPackageContentService packageContentService,
    IPackageLocationService packageLocationService) : ITransferPackageService {
    
    public async Task<TransferAddItemResponse> HandleSourcePackageScanAsync(TransferAddSourcePackageRequest request, SessionInfo sessionInfo) {
        await using var transaction = await db.Database.BeginTransactionAsync();
        try {
            // Load package by barcode
            var scannedPackage = await packageService.GetPackageAsync(request.PackageId);
            
            if (scannedPackage == null) {
                throw new ValidationException($"Package with id {request.PackageId} not found");
            }
            
            // Get package contents
            var packageContents = scannedPackage.Contents;
            if (!packageContents.Any()) {
                throw new ValidationException("Package is empty");
            }

            var transfer = await db.Transfers.FindAsync(request.TransferId);
            if (transfer == null) {
                throw new KeyNotFoundException($"Transfer with ID {request.TransferId} not found.");
            }

            // Create source transfer lines for ALL package contents automatically
            foreach (var content in packageContents) {
                var line = new TransferLine {
                    ItemCode        = content.ItemCode,
                    BarCode         = content.ItemCode, // Use item code as barcode
                    BinEntry        = request.BinEntry ?? scannedPackage.BinEntry,
                    Date            = DateTime.UtcNow,
                    Quantity        = (int)content.Quantity,
                    Type            = SourceTarget.Source,
                    UnitType        = UnitType.Unit,
                    TransferId      = request.TransferId,
                    CreatedAt       = DateTime.UtcNow,
                    CreatedByUserId = sessionInfo.Guid,
                    LineStatus      = LineStatus.Open,
                    Comments        = $"Package: {scannedPackage.Barcode}"
                };

                db.TransferLines.Add(line);
            }

            // Update transfer status
            if (transfer.Status == ObjectStatus.Open)
                transfer.Status = ObjectStatus.InProgress;

            db.Update(transfer);
            await db.SaveChangesAsync();
            await transaction.CommitAsync();

            return new TransferAddItemResponse {
                IsPackageScan   = true,
                PackageId       = scannedPackage.Id,
                PackageContents = (await Task.WhenAll(packageContents.Select(async c => await c.ToDto(adapter)))).ToList()
            };
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

            // Note: Package movement will happen during ProcessTransfer when result.Success is true
            await db.SaveChangesAsync();
            await transaction.CommitAsync();

            return new TransferAddItemResponse {
                IsPackageTransfer = true,
                PackageId         = package.Id
            };
        }
        catch {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task MovePackagesOnTransferProcessAsync(Guid transferId, SessionInfo sessionInfo) {
        // Get all package transfers for this transfer
        var packageTransferLines = await db.TransferLines
            .Where(tl => tl.TransferId == transferId &&
                         tl.Type == SourceTarget.Target &&
                         tl.Comments != null && tl.Comments.StartsWith("Package:") &&
                         tl.LineStatus == LineStatus.Open)
            .GroupBy(tl => tl.Comments) // Group by package barcode
            .ToListAsync();
        
        if (packageTransferLines.Count == 0) 
            return; // No packages to move, exit

        var transfer = await db.Transfers.FindAsync(transferId);
        if (transfer == null) {
            throw new KeyNotFoundException($"Transfer with ID {transferId} not found.");
        }

        foreach (var packageGroup in packageTransferLines) {
            var packageBarcode = packageGroup.Key?.Replace("Package: ", "");
            if (string.IsNullOrEmpty(packageBarcode)) continue;

            var package = await packageService.GetPackageByBarcodeAsync(new PackageByBarcodeRequest {
                Barcode = packageBarcode,
                Contents = false
            });

            if (package == null) continue;

            // Get target bin from the first line in the group
            var firstLine = packageGroup.First();
            if (firstLine.BinEntry.HasValue) {
                await packageLocationService.MovePackageAsync(new MovePackageRequest {
                    PackageId           = package.Id,
                    ToWhsCode           = transfer.WhsCode,
                    ToBinEntry          = firstLine.BinEntry,
                    UserId              = sessionInfo.Guid,
                    SourceOperationType = ObjectType.Transfer,
                    SourceOperationId   = transferId
                });
            }
        }
    }
}