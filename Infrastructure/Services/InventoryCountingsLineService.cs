using System.ComponentModel.DataAnnotations;
using Core.DTOs.General;
using Core.DTOs.InventoryCounting;
using Core.DTOs.Items;
using Core.DTOs.Package;
using Core.Entities;
using Core.Enums;
using Core.Exceptions;
using Core.Interfaces;
using Core.Models;
using Core.Services;
using Infrastructure.DbContexts;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Services;

public class InventoryCountingsLineService(SystemDbContext db, IExternalSystemAdapter adapter, ISettings settings, IPackageContentService packageContentService, IPackageService packageService)
    : IInventoryCountingsLineService {
    public async Task<InventoryCountingAddItemResponse> AddItem(SessionInfo sessionInfo, InventoryCountingAddItemRequest request) {
        // Validate the counting exists and is in a valid state
        var counting = await db.InventoryCountings.FindAsync(request.ID);
        if (counting == null) {
            throw new KeyNotFoundException($"Inventory counting with ID {request.ID} not found.");
        }

        if (counting.Status != ObjectStatus.Open && counting.Status != ObjectStatus.InProgress) {
            return new InventoryCountingAddItemResponse {
                Status         = ResponseStatus.Error,
                ErrorMessage   = "Counting must be Open or In Progress to add items",
                ClosedCounting = true
            };
        }

        // Validate the item and barcode
        var validationResult = await adapter.GetItemValidationInfo(request.ItemCode, request.BarCode, sessionInfo.Warehouse, request.BinEntry, sessionInfo.EnableBinLocations);

        if (!validationResult.IsValidBarCode) {
            throw new ApiErrorException((int)AddItemReturnValueType.ItemCodeBarCodeMismatch, new { request.ItemCode, request.BarCode });
        }

        if (!validationResult.IsInventoryItem) {
            throw new ApiErrorException((int)AddItemReturnValueType.NotStockItem, new { request.ItemCode, request.BarCode });
        }

        if (request.PackageId.HasValue) {
            if (!await ValidateScanPackage(request.PackageId.Value, request.ID, request.BinEntry)) {
                throw new ApiErrorException((int)AddItemReturnValueType.PackageBinLocation, new { request.ItemCode, request.BarCode });
            }
        }

        // Calculate total quantity including unit conversion
        int totalQuantity = request.Quantity;
        if (request.Unit != UnitType.Unit) {
            totalQuantity *= validationResult.NumInBuy;
            if (request.Unit == UnitType.Pack) {
                totalQuantity *= validationResult.PurPackUn;
            }
        }

        var transaction = await db.Database.BeginTransactionAsync();
        try {
            // Handle new package creation
            if (request.StartNewPackage && settings.Options.EnablePackages) {
                var package = await packageService.CreatePackageAsync(sessionInfo, new CreatePackageRequest {
                    BinEntry            = request.BinEntry,
                    SourceOperationType = ObjectType.InventoryCounting,
                    SourceOperationId   = request.ID
                });

                request.PackageId = package.Id;
            }

            // Create counting line
            var line = new InventoryCountingLine {
                InventoryCountingId = request.ID,
                ItemCode            = request.ItemCode,
                BarCode             = request.BarCode,
                Quantity            = totalQuantity,
                BinEntry            = request.BinEntry,
                Unit                = request.Unit,
                Date                = DateTime.UtcNow,
                LineStatus          = LineStatus.Open,
                CreatedByUserId     = sessionInfo.Guid,
                PackageId           = request.PackageId
            };

            await db.InventoryCountingLines.AddAsync(line);

            // Update counting status if it was Open
            if (counting.Status == ObjectStatus.Open) {
                counting.Status          = ObjectStatus.InProgress;
                counting.UpdatedAt       = DateTime.UtcNow;
                counting.UpdatedByUserId = sessionInfo.Guid;
                db.Update(counting);
            }

            // If item is being counted into a package, update the counting package content
            string? packageBarcode = null;
            if (request.PackageId.HasValue) {
                var countingPackage = await UpdateCountingPackageContent(request.ID, request.PackageId.Value, request.ItemCode, totalQuantity, request.Unit, sessionInfo);
                packageBarcode = countingPackage.PackageBarcode;
            }

            await db.SaveChangesAsync();
            await transaction.CommitAsync();

            var response = InventoryCountingAddItemResponse.Success(line.Id);

            if (request.PackageId.HasValue && response.Status == ResponseStatus.Ok) {
                response.PackageId      = request.PackageId.Value;
                response.PackageBarcode = packageBarcode;
            }

            return response;
        }
        catch {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task<UpdateLineResponse> UpdateLine(SessionInfo sessionInfo, InventoryCountingUpdateLineRequest request) {
        var response = new UpdateLineResponse();

        var transaction = await db.Database.BeginTransactionAsync();
        try {
            // Validate counting exists and is in valid state
            var counting = await db.InventoryCountings
                .Where(ic => ic.Id == request.Id)
                .Select(ic => new { ic.Status })
                .FirstOrDefaultAsync();

            if (counting == null) {
                response.ReturnValue  = UpdateLineReturnValue.Status;
                response.ErrorMessage = "Inventory counting not found";
                return response;
            }

            if (counting.Status != ObjectStatus.Open && counting.Status != ObjectStatus.InProgress) {
                response.ReturnValue  = UpdateLineReturnValue.Status;
                response.ErrorMessage = "Counting status is not Open or In Progress";
                return response;
            }

            // Find the line to update
            var line = await db.InventoryCountingLines
                .Where(icl => icl.Id == request.LineId && icl.InventoryCountingId == request.Id)
                .FirstOrDefaultAsync();

            if (line == null) {
                response.ReturnValue  = UpdateLineReturnValue.LineStatus;
                response.ErrorMessage = "Counting line not found";
                return response;
            }

            if (line.LineStatus == LineStatus.Closed) {
                response.ReturnValue  = UpdateLineReturnValue.LineStatus;
                response.ErrorMessage = "Line is already closed";
                return response;
            }

            // Update comments if provided
            if (request.Comment != null) {
                line.Comments = request.Comment;
            }

            // Update quantity if provided
            if (request.Quantity.HasValue) {
                int newQuantity = request.Quantity.Value;
                var items       = await adapter.ItemCheckAsync(line.ItemCode, null);
                var item        = items.FirstOrDefault();
                if (line.Unit != UnitType.Unit && item != null) {
                    newQuantity *= item.NumInBuy;
                    if (line.Unit == UnitType.Pack) {
                        newQuantity *= item.PurPackUn;
                    }
                }

                line.Quantity = newQuantity;
                decimal diff = newQuantity - line.Quantity;
                await UpdatePackageContentBasedOnLineQuantity(sessionInfo, line, item!, diff);
            }

            // Handle line closure
            if (request.CancellationReasonId.HasValue) {
                // Validate the cancellation reason exists and is enabled
                var cancellationReason = await db.CancellationReasons
                    .Where(cr => cr.Id == request.CancellationReasonId.Value && cr.IsEnabled && cr.Counting)
                    .FirstOrDefaultAsync();

                if (cancellationReason == null) {
                    response.ReturnValue  = UpdateLineReturnValue.CloseReason;
                    response.ErrorMessage = "Invalid or disabled cancellation reason for counting";
                    return response;
                }

                line.LineStatus           = LineStatus.Closed;
                line.CancellationReasonId = request.CancellationReasonId.Value;
            }

            // Update modification tracking
            line.UpdatedAt       = DateTime.UtcNow;
            line.UpdatedByUserId = sessionInfo.Guid;

            db.Update(line);
            await db.SaveChangesAsync();
            await transaction.CommitAsync();

            return response;
        }
        catch {
            await transaction.RollbackAsync();
            throw;
        }
    }

    private async Task UpdatePackageContentBasedOnLineQuantity(SessionInfo session, InventoryCountingLine line, ItemCheckResponse item, decimal unitQuantity) {
        if (unitQuantity == 0) {
            return;
        }

        var transaction = await db
            .PackageTransactions
            .Include(v => v.Package)
            .FirstOrDefaultAsync(v => v.SourceOperationLineId == line.Id);

        if (transaction == null) {
            return;
        }

        decimal quantity = unitQuantity;
        if (line.Unit != UnitType.Unit) {
            quantity /= item.NumInBuy;
            if (line.Unit == UnitType.Pack)
                quantity /= item.PurPackUn;
        }

        switch (unitQuantity) {
            case > 0: {
                var addRequest = new AddItemToPackageRequest {
                    PackageId             = transaction.PackageId,
                    ItemCode              = line.ItemCode,
                    Quantity              = quantity,
                    UnitQuantity          = unitQuantity,
                    UnitType              = line.Unit,
                    BinEntry              = line.BinEntry,
                    SourceOperationType   = ObjectType.InventoryCounting,
                    SourceOperationId     = line.InventoryCountingId,
                    SourceOperationLineId = line.Id
                };
                await packageContentService.AddItemToPackageAsync(addRequest, session);
                break;
            }
            case < 0: {
                var removeRequest = new RemoveItemFromPackageRequest {
                    PackageId           = transaction.PackageId,
                    ItemCode            = line.ItemCode,
                    Quantity            = quantity * -1,
                    UnitQuantity        = unitQuantity * -1,
                    UnitType            = line.Unit,
                    SourceOperationType = ObjectType.InventoryCounting,
                    SourceOperationId   = line.InventoryCountingId
                };
                await packageContentService.RemoveItemFromPackageAsync(removeRequest, session);
                break;
            }
        }
    }

    public async Task<bool> ValidateScanPackage(Guid packageId, Guid id, int? binEntry) {
        // Check if package exists in counting with different bin location
        bool isDifferentBinLocation = await db.InventoryCountingLines.AnyAsync(c =>
            c.InventoryCountingId == id &&
            c.PackageId == packageId &&
            c.BinEntry != binEntry);

        // Return true if package is not found in counting or is in same bin location
        return !isDifferentBinLocation;
    }

    private async Task<InventoryCountingPackage> UpdateCountingPackageContent(Guid countingId, Guid packageId, string itemCode, int quantity, UnitType unit, SessionInfo sessionInfo) {
        var countingPackage = await db.InventoryCountingPackages
            .FirstOrDefaultAsync(cp => cp.InventoryCountingId == countingId && cp.PackageId == packageId);

        if (countingPackage == null) {
            throw new ValidationException($"Package {packageId} not found in counting {countingId}.");
        }

        var countingContent = await db.InventoryCountingPackageContents
            .FirstOrDefaultAsync(c => c.InventoryCountingPackageId == countingPackage.Id && c.ItemCode == itemCode);

        if (countingContent == null) {
            // New item being counted into package
            countingContent = new InventoryCountingPackageContent {
                Id                         = Guid.NewGuid(),
                InventoryCountingPackageId = countingPackage.Id,
                ItemCode                   = itemCode,
                CountedQuantity            = quantity,
                OriginalQuantity           = 0, // New item to package
                Unit                       = unit,
                CreatedAt                  = DateTime.UtcNow,
                CreatedByUserId            = sessionInfo.Guid
            };

            db.InventoryCountingPackageContents.Add(countingContent);
        }
        else {
            // Update existing counted quantity
            countingContent.CountedQuantity += quantity;
            countingContent.UpdatedAt       =  DateTime.UtcNow;
            countingContent.UpdatedByUserId =  sessionInfo.Guid;
        }

        return countingPackage;
    }
}