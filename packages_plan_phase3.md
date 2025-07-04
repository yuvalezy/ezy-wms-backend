# Phase 3: Operation Integration

## 3.1 Goods Receipt Integration

### 3.1.1 Enhanced GoodsReceiptController ✅ COMPLETED

**Status**: Implemented with service layer refactoring for better architecture

**Key Changes from Original Plan**:
- CompleteGoodsReceipt endpoint was removed (duplicated Process endpoint functionality)
- Package activation logic moved to GoodsReceiptService.ProcessGoodsReceipt method
- Enhanced with specialized service injection for better separation of concerns

```csharp
// Enhanced AddItem method in GoodsReceiptController
[HttpPost("{id}/items")]
public async Task<ActionResult<GoodsReceiptAddItemResponse>> AddItem(Guid id, [FromBody] GoodsReceiptAddItemRequest request)
{
    // Package creation logic when StartNewPackage is true
    if (request.StartNewPackage && IsPackageFeatureEnabled())
    {
        var package = await packageService.CreatePackageAsync(sessionInfo, new CreatePackageRequest
        {
            BinEntry = request.BinEntry,
            BinCode = request.BinCode,
            SourceOperationType = ObjectType.GoodsReceipt,
            SourceOperationId = request.Id
        });
        
        request.PackageId = package.Id;
    }
    
    // Add item to package if package operation is active
    if (request.PackageId.HasValue)
    {
        await packageService.AddItemToPackageAsync(new AddItemToPackageRequest
        {
            PackageId = request.PackageId.Value,
            ItemCode = request.ItemCode,
            Quantity = request.Quantity,
            UnitType = request.UnitType,
            BinEntry = request.BinEntry,
            SourceOperationType = ObjectType.GoodsReceipt,
            SourceOperationId = request.Id,
            SourceOperationLineId = line.Id
        }, sessionInfo);
    }
}

// Package activation moved to GoodsReceiptService.ProcessGoodsReceipt
public class GoodsReceiptService 
{
    public async Task<ProcessGoodsReceiptResponse> ProcessGoodsReceipt(Guid id, SessionInfo session)
    {
        // Process goods receipt logic...
        
        // Activate packages created during this operation
        int activatedPackagesCount = 0;
        if (IsPackageFeatureEnabled())
        {
            activatedPackagesCount = await packageService.ActivatePackagesBySourceAsync(ObjectType.GoodsReceipt, id, session);
        }
        
        return new ProcessGoodsReceiptResponse {
            Success = true,
            ActivatedPackages = activatedPackagesCount
        };
    }
}
```

### 3.1.2 Enhanced AddGoodsReceiptItemRequest ✅ COMPLETED

**Status**: Implemented with simplified structure (removed unnecessary fields)

**Key Changes from Original Plan**:
- Removed BatchNo, SerialNo, ExpiryDate (complexity not needed in current phase)
- Simplified to essential package properties only
- UnitCode changed to UnitType enum for consistency

```csharp
public class GoodsReceiptAddItemRequest
{
    [Required]
    public Guid Id { get; set; }

    [Required]
    [StringLength(50)]
    public required string ItemCode { get; set; }

    [Required]
    [StringLength(254)]
    public required string BarCode { get; set; }

    [Required]
    public UnitType Unit { get; set; } = UnitType.Pack;

    // Package-related properties
    public Guid? PackageId { get; set; }
    public bool StartNewPackage { get; set; }
}

public class GoodsReceiptAddItemResponse : ResponseBase
{
    // Existing properties from goods receipt
    // ... (existing goods receipt response properties)
    
    // Package-related properties
    public Guid? PackageId { get; set; }
    public string? PackageBarcode { get; set; }
}
```

## Service Architecture Updates ✅ COMPLETED

**Status**: Major refactoring completed for better maintainability and separation of concerns

**Key Architectural Changes**:

### Specialized Services Created:
1. **IPackageContentService** - Item management and transaction logging
2. **IPackageValidationService** - Barcode generation and consistency validation  
3. **IPackageLocationService** - Movement tracking and location history
4. **IPackageService** - Core package lifecycle management (reduced from 526 to 225 lines)

### Controller Architecture:
- **PackageController** now directly injects specialized services
- Content operations → `IPackageContentService`
- Location operations → `IPackageLocationService` 
- Validation operations → `IPackageValidationService`
- Core package operations → `IPackageService`

### Current Package System Implementation Status:

```csharp
// Package entities are implemented with:
public class Package : BaseEntity 
{
    public required string Barcode { get; set; }
    public PackageStatus Status { get; set; } = PackageStatus.Init;
    public required string WhsCode { get; set; }
    public int? BinEntry { get; set; }
    public required ObjectType SourceOperationType { get; set; }
    public Guid? SourceOperationId { get; set; }
    public string? CustomAttributes { get; set; }
    
    // Navigation properties
    public virtual ICollection<PackageContent> Contents { get; set; }
    public virtual ICollection<PackageTransaction> Transactions { get; set; }
    public virtual ICollection<PackageLocationHistory> LocationHistory { get; set; }
}

// External adapter integration for bin codes
// Package extensions with async ToDto methods
// Complete package lifecycle management (Init → Active → Closed/Cancelled)
```

## 3.2 Counting Integration ⏳ NOT YET IMPLEMENTED

### 3.2.1 Enhanced CountingController

```csharp
[HttpPost("{id}/items")]
public async Task<ActionResult<InventoryCountingLineDto>> AddItem(int id, [FromBody] AddCountingItemRequest request)
{
    try
    {
        // Check if user scanned a package barcode
        var scannedPackage = await _packageService.GetPackageByBarcodeAsync(request.ScannedBarcode);
        
        if (scannedPackage != null)
        {
            // User scanned a package - set current package context
            request.CurrentPackageId = scannedPackage.Id;
            
            return Ok(new AddItemResponse
            {
                IsPackageScan = true,
                PackageId = scannedPackage.Id,
                PackageBarcode = scannedPackage.Barcode,
                PackageContents = await GetPackageContentsForCounting(scannedPackage.Id),
                Message = $"Package {scannedPackage.Barcode} selected. Now scan items within this package."
            });
        }
        
        // Standard item scanning logic
        var itemCheck = await _itemRepository.ScanItemBarCodeAsync(request.ScannedBarcode, request.WhsCode);
        if (itemCheck == null)
        {
            return BadRequest(new { error = "Item not found" });
        }
        
        // Handle package creation or item addition
        if (request.StartNewPackage && IsPackageFeatureEnabled())
        {
            var package = await _packageService.CreatePackageAsync(new CreatePackageRequest
            {
                WhsCode = request.WhsCode,
                BinEntry = request.BinEntry,
                BinCode = request.BinCode,
                UserId = EmployeeID,
                SourceOperationType = "Counting",
                SourceOperationId = Guid.Parse(id.ToString())
            });
            
            request.CurrentPackageId = package.Id;
        }
        
        // Create counting line
        var line = await Data.Counting.AddItemAsync(new AddCountingItemRequest
        {
            CountingId = id,
            ItemCode = itemCheck.ItemCode,
            CountedQuantity = request.CountedQuantity,
            WhsCode = request.WhsCode,
            BinEntry = request.BinEntry,
            BinCode = request.BinCode,
            PackageId = request.CurrentPackageId,
            UserId = EmployeeID
        });
        
        // Add to package if package context is active
        if (request.CurrentPackageId.HasValue)
        {
            await _packageService.AddItemToPackageAsync(new AddItemToPackageRequest
            {
                PackageId = request.CurrentPackageId.Value,
                ItemCode = itemCheck.ItemCode,
                Quantity = request.CountedQuantity,
                UnitCode = itemCheck.UnitCode,
                WhsCode = request.WhsCode,
                BinEntry = request.BinEntry,
                BinCode = request.BinCode,
                UserId = EmployeeID,
                SourceOperationType = "Counting",
                SourceOperationId = Guid.Parse(id.ToString()),
                SourceOperationLineId = line.Id
            });
        }
        
        var response = line.ToDto();
        response.PackageId = request.CurrentPackageId;
        
        return Ok(response);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error adding item to counting {Id}", id);
        return BadRequest(new { error = ex.Message });
    }
}

[HttpPost("{id}/complete")]
public async Task<ActionResult> CompleteCounting(int id)
{
    using var transaction = await _context.Database.BeginTransactionAsync();
    try
    {
        // Complete counting operation
        await Data.Counting.CompleteAsync(id, EmployeeID);
        
        // Update package contents based on counting results
        var countingLines = await _context.InventoryCountingLines
            .Where(l => l.CountingId == id && l.PackageId.HasValue)
            .ToListAsync();
        
        foreach (var line in countingLines)
        {
            await UpdatePackageContentFromCounting(line);
        }
        
        await _context.SaveChangesAsync();
        await transaction.CommitAsync();
        
        _logger.LogInformation("Counting {Id} completed with package updates", id);
        
        return Ok();
    }
    catch (Exception ex)
    {
        await transaction.RollbackAsync();
        _logger.LogError(ex, "Error completing counting {Id}", id);
        return BadRequest(new { error = ex.Message });
    }
}

private async Task<List<PackageContentSummaryDto>> GetPackageContentsForCounting(Guid packageId)
{
    var contents = await _packageService.GetPackageContentsAsync(packageId);
    return contents.Select(c => new PackageContentSummaryDto
    {
        ItemCode = c.ItemCode,
        Quantity = c.Quantity,
        UnitCode = c.UnitCode,
        BatchNo = c.BatchNo,
        SerialNo = c.SerialNo
    }).ToList();
}

private async Task UpdatePackageContentFromCounting(InventoryCountingLine countingLine)
{
    var existingContent = await _context.PackageContents
        .FirstOrDefaultAsync(c => c.PackageId == countingLine.PackageId && 
                                 c.ItemCode == countingLine.ItemCode &&
                                 c.BatchNo == countingLine.BatchNo);
    
    if (existingContent != null)
    {
        // Update existing content quantity
        existingContent.Quantity = countingLine.CountedQuantity;
        
        // Remove if quantity is zero
        if (existingContent.Quantity == 0)
        {
            _context.PackageContents.Remove(existingContent);
        }
    }
    else if (countingLine.CountedQuantity > 0)
    {
        // Add new content if counted quantity is positive
        var newContent = new PackageContent
        {
            Id = Guid.NewGuid(),
            PackageId = countingLine.PackageId.Value,
            ItemCode = countingLine.ItemCode,
            Quantity = countingLine.CountedQuantity,
            UnitCode = countingLine.UnitCode,
            BatchNo = countingLine.BatchNo,
            SerialNo = countingLine.SerialNo,
            WhsCode = countingLine.WhsCode,
            BinEntry = countingLine.BinEntry,
            BinCode = countingLine.BinCode,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = countingLine.UserId
        };
        
        _context.PackageContents.Add(newContent);
    }
}
```

### 3.2.2 Enhanced AddCountingItemRequest

```csharp
public class AddCountingItemRequest
{
    public int CountingId { get; set; }
    public string ScannedBarcode { get; set; }
    public string ItemCode { get; set; }
    public decimal CountedQuantity { get; set; }
    public string UnitCode { get; set; }
    public string WhsCode { get; set; }
    public int? BinEntry { get; set; }
    public string BinCode { get; set; }
    public string BatchNo { get; set; }
    public string SerialNo { get; set; }
    public string UserId { get; set; }
    
    // Package-related properties
    public bool StartNewPackage { get; set; }
    public Guid? CurrentPackageId { get; set; }
}

public class AddItemResponse
{
    public bool IsPackageScan { get; set; }
    public Guid? PackageId { get; set; }
    public string PackageBarcode { get; set; }
    public List<PackageContentSummaryDto> PackageContents { get; set; }
    public string Message { get; set; }
}
```

## 3.3 Transfer Integration ⏳ NOT YET IMPLEMENTED

### 3.3.1 Enhanced TransferController with Package Rules

```csharp
[HttpPost("{id}/items")]
public async Task<ActionResult<TransferLineDto>> AddItem(int id, [FromBody] AddTransferItemRequest request)
{
    try
    {
        // Validate transfer rules for packages
        if (request.SourcePackageId.HasValue || request.TargetPackageId.HasValue)
        {
            await ValidatePackageTransferRulesAsync(request);
        }
        
        // Handle different transfer scenarios
        if (request.IsFullPackageTransfer)
        {
            return await HandleFullPackageTransferAsync(id, request);
        }
        else if (request.IsPackageToPackageTransfer)
        {
            return await HandlePackageToPackageTransferAsync(id, request);
        }
        else
        {
            return await HandleStandardTransferAsync(id, request);
        }
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error processing transfer item for transfer {Id}", id);
        return BadRequest(new { error = ex.Message });
    }
}

private async Task ValidatePackageTransferRulesAsync(AddTransferItemRequest request)
{
    // Rule: Same bin location allows partial transfers
    // Rule: Different bin/warehouse requires full package transfer
    
    if (request.SourcePackageId.HasValue)
    {
        var sourcePackage = await _packageService.GetPackageAsync(request.SourcePackageId.Value);
        
        // Check if locations are different
        bool isDifferentLocation = sourcePackage.WhsCode != request.TargetWhsCode ||
                                  sourcePackage.BinEntry != request.TargetBinEntry;
        
        if (isDifferentLocation && !request.IsFullPackageTransfer)
        {
            throw new InvalidOperationException(
                "Partial package transfers are only allowed within the same bin location. " +
                "Use full package transfer for different locations.");
        }
        
        // Validate package is not locked
        if (sourcePackage.Status == PackageStatus.Locked)
        {
            throw new InvalidOperationException($"Package {sourcePackage.Barcode} is locked");
        }
    }
}

private async Task<ActionResult<TransferLineDto>> HandleFullPackageTransferAsync(int transferId, AddTransferItemRequest request)
{
    using var transaction = await _context.Database.BeginTransactionAsync();
    try
    {
        var sourcePackage = await _packageService.GetPackageAsync(request.SourcePackageId.Value);
        var packageContents = await _packageService.GetPackageContentsAsync(request.SourcePackageId.Value);
        
        var transferLines = new List<TransferLine>();
        
        // Create transfer lines for all package contents
        foreach (var content in packageContents)
        {
            var line = await Data.Transfer.AddItemAsync(new AddTransferItemRequest
            {
                TransferId = transferId,
                ItemCode = content.ItemCode,
                Quantity = content.Quantity,
                UnitCode = content.UnitCode,
                SourceWhsCode = content.WhsCode,
                SourceBinEntry = content.BinEntry,
                TargetWhsCode = request.TargetWhsCode,
                TargetBinEntry = request.TargetBinEntry,
                TargetBinCode = request.TargetBinCode,
                BatchNo = content.BatchNo,
                SerialNo = content.SerialNo,
                UserId = EmployeeID
            });
            
            transferLines.Add(line);
        }
        
        // Move package to new location
        await _packageService.MovePackageAsync(new MovePackageRequest
        {
            PackageId = request.SourcePackageId.Value,
            ToWhsCode = request.TargetWhsCode,
            ToBinEntry = request.TargetBinEntry,
            ToBinCode = request.TargetBinCode,
            UserId = EmployeeID,
            SourceOperationType = "Transfer",
            SourceOperationId = Guid.Parse(transferId.ToString())
        });
        
        await transaction.CommitAsync();
        
        return Ok(new TransferPackageResponse
        {
            PackageId = sourcePackage.Id,
            PackageBarcode = sourcePackage.Barcode,
            TransferredLines = transferLines.Select(l => l.ToDto()).ToList(),
            Message = $"Full package {sourcePackage.Barcode} transferred successfully"
        });
    }
    catch (Exception ex)
    {
        await transaction.RollbackAsync();
        throw;
    }
}

private async Task<ActionResult<TransferLineDto>> HandlePackageToPackageTransferAsync(int transferId, AddTransferItemRequest request)
{
    using var transaction = await _context.Database.BeginTransactionAsync();
    try
    {
        // Validate both packages are in same location
        var sourcePackage = await _packageService.GetPackageAsync(request.SourcePackageId.Value);
        var targetPackage = await _packageService.GetPackageAsync(request.TargetPackageId.Value);
        
        if (sourcePackage.WhsCode != targetPackage.WhsCode || 
            sourcePackage.BinEntry != targetPackage.BinEntry)
        {
            throw new InvalidOperationException("Package-to-package transfers must be within the same location");
        }
        
        // Remove item from source package
        await _packageService.RemoveItemFromPackageAsync(new RemoveItemFromPackageRequest
        {
            PackageId = request.SourcePackageId.Value,
            ItemCode = request.ItemCode,
            Quantity = request.Quantity,
            BatchNo = request.BatchNo,
            SerialNo = request.SerialNo,
            UserId = EmployeeID,
            SourceOperationType = "Transfer",
            SourceOperationId = Guid.Parse(transferId.ToString())
        });
        
        // Add item to target package
        await _packageService.AddItemToPackageAsync(new AddItemToPackageRequest
        {
            PackageId = request.TargetPackageId.Value,
            ItemCode = request.ItemCode,
            Quantity = request.Quantity,
            UnitCode = request.UnitCode,
            BatchNo = request.BatchNo,
            SerialNo = request.SerialNo,
            WhsCode = targetPackage.WhsCode,
            BinEntry = targetPackage.BinEntry,
            BinCode = targetPackage.BinCode,
            UserId = EmployeeID,
            SourceOperationType = "Transfer",
            SourceOperationId = Guid.Parse(transferId.ToString())
        });
        
        // Check if source package is empty and close it
        var sourceContents = await _packageService.GetPackageContentsAsync(request.SourcePackageId.Value);
        if (!sourceContents.Any() || sourceContents.All(c => c.Quantity == 0))
        {
            await _packageService.ClosePackageAsync(request.SourcePackageId.Value, EmployeeID);
        }
        
        await transaction.CommitAsync();
        
        return Ok(new TransferLineDto
        {
            ItemCode = request.ItemCode,
            Quantity = request.Quantity,
            SourcePackageId = request.SourcePackageId,
            TargetPackageId = request.TargetPackageId,
            Message = "Item transferred between packages successfully"
        });
    }
    catch (Exception ex)
    {
        await transaction.RollbackAsync();
        throw;
    }
}
```

### 3.3.2 Enhanced AddTransferItemRequest

```csharp
public class AddTransferItemRequest
{
    public int TransferId { get; set; }
    public string ItemCode { get; set; }
    public decimal Quantity { get; set; }
    public string UnitCode { get; set; }
    public string SourceWhsCode { get; set; }
    public int? SourceBinEntry { get; set; }
    public string SourceBinCode { get; set; }
    public string TargetWhsCode { get; set; }
    public int? TargetBinEntry { get; set; }
    public string TargetBinCode { get; set; }
    public string BatchNo { get; set; }
    public string SerialNo { get; set; }
    public string UserId { get; set; }
    
    // Package-related properties
    public Guid? SourcePackageId { get; set; }
    public Guid? TargetPackageId { get; set; }
    public bool IsFullPackageTransfer { get; set; }
    public bool IsPackageToPackageTransfer { get; set; }
}

public class MovePackageRequest
{
    public Guid PackageId { get; set; }
    public string ToWhsCode { get; set; }
    public int? ToBinEntry { get; set; }
    public string ToBinCode { get; set; }
    public string UserId { get; set; }
    public string SourceOperationType { get; set; }
    public Guid? SourceOperationId { get; set; }
}
```

## 3.4 Picking Integration ⏳ NOT YET IMPLEMENTED

### 3.4.1 Enhanced PickingController with Package Logic

```csharp
[HttpPost("{id}/items")]
public async Task<ActionResult<PickingLineDto>> AddItem(int id, [FromBody] AddPickingItemRequest request)
{
    try
    {
        // Check if user scanned a package barcode
        var scannedPackage = await _packageService.GetPackageByBarcodeAsync(request.ScannedBarcode);
        
        if (scannedPackage != null)
        {
            // Validate package is in expected location
            if (scannedPackage.WhsCode != request.ExpectedWhsCode || 
                scannedPackage.BinEntry != request.ExpectedBinEntry)
            {
                return BadRequest(new { error = "Package is not in expected location" });
            }
            
            return Ok(new AddItemResponse
            {
                IsPackageScan = true,
                PackageId = scannedPackage.Id,
                PackageBarcode = scannedPackage.Barcode,
                AvailableItems = await GetPackageAvailableItemsAsync(scannedPackage.Id, request.RequiredItems),
                Message = "Package scanned. Now scan items to pick from this package."
            });
        }
        
        // Standard item picking logic with package context
        var itemCheck = await _itemRepository.ScanItemBarCodeAsync(request.ScannedBarcode, request.WhsCode);
        if (itemCheck == null)
        {
            return BadRequest(new { error = "Item not found" });
        }
        
        // Validate item can be picked from current package (if package context is active)
        if (request.CurrentPackageId.HasValue)
        {
            var availableQty = await _packageService.GetItemQuantityInPackageAsync(
                request.CurrentPackageId.Value, itemCheck.ItemCode);
            
            if (availableQty < request.PickedQuantity)
            {
                return BadRequest(new { error = $"Only {availableQty} available in package" });
            }
        }
        
        // Create picking line
        var line = await Data.Picking.AddItemAsync(new AddPickingItemRequest
        {
            PickingId = id,
            ItemCode = itemCheck.ItemCode,
            PickedQuantity = request.PickedQuantity,
            UnitCode = itemCheck.UnitCode,
            WhsCode = request.WhsCode,
            BinEntry = request.BinEntry,
            BatchNo = request.BatchNo,
            SerialNo = request.SerialNo,
            UserId = EmployeeID,
            PackageId = request.CurrentPackageId
        });
        
        // Remove item from package
        if (request.CurrentPackageId.HasValue)
        {
            await _packageService.RemoveItemFromPackageAsync(new RemoveItemFromPackageRequest
            {
                PackageId = request.CurrentPackageId.Value,
                ItemCode = itemCheck.ItemCode,
                Quantity = request.PickedQuantity,
                BatchNo = request.BatchNo,
                SerialNo = request.SerialNo,
                UserId = EmployeeID,
                SourceOperationType = "Picking",
                SourceOperationId = Guid.Parse(id.ToString())
            });
            
            // Check if package is now empty and close it
            var remainingContents = await _packageService.GetPackageContentsAsync(request.CurrentPackageId.Value);
            if (!remainingContents.Any() || remainingContents.All(c => c.Quantity == 0))
            {
                await _packageService.ClosePackageAsync(request.CurrentPackageId.Value, EmployeeID);
            }
        }
        
        return Ok(line.ToDto());
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error adding item to picking {Id}", id);
        return BadRequest(new { error = ex.Message });
    }
}

// Enhanced picking suggestion logic
[HttpGet("{id}/suggestions")]
public async Task<ActionResult<PickingSuggestionsDto>> GetPickingSuggestions(int id, [FromQuery] string itemCode, [FromQuery] string whsCode, [FromQuery] int? binEntry)
{
    try
    {
        var suggestions = await Data.Picking.GetPickingSuggestionsAsync(itemCode, whsCode, binEntry);
        
        // Include package information in suggestions
        foreach (var suggestion in suggestions.BinSuggestions)
        {
            suggestion.Packages = await GetPackagesInBinAsync(suggestion.WhsCode, suggestion.BinEntry, itemCode);
            
            // Check if packages are required for picking (insufficient loose stock)
            var looseStock = suggestion.AvailableQuantity - suggestion.Packages.Sum(p => p.ItemQuantity);
            suggestion.RequiresPackagePicking = looseStock < suggestion.RequiredQuantity;
            
            if (suggestion.RequiresPackagePicking)
            {
                suggestion.Message = "Insufficient loose stock. Package picking required.";
            }
        }
        
        return Ok(suggestions);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error getting picking suggestions for picking {Id}", id);
        return BadRequest(new { error = ex.Message });
    }
}

private async Task<List<PackageItemInfo>> GetPackagesInBinAsync(string whsCode, int? binEntry, string itemCode)
{
    return await _context.Packages
        .Where(p => p.Status == PackageStatus.Active && 
                   p.WhsCode == whsCode && 
                   p.BinEntry == binEntry)
        .Join(_context.PackageContents.Where(pc => pc.ItemCode == itemCode),
              p => p.Id, pc => pc.PackageId,
              (p, pc) => new PackageItemInfo
              {
                  PackageId = p.Id,
                  PackageBarcode = p.Barcode,
                  ItemCode = pc.ItemCode,
                  ItemQuantity = pc.Quantity,
                  UnitCode = pc.UnitCode,
                  BatchNo = pc.BatchNo,
                  SerialNo = pc.SerialNo
              })
        .ToListAsync();
}

private async Task<List<AvailableItemDto>> GetPackageAvailableItemsAsync(Guid packageId, List<RequiredItemDto> requiredItems)
{
    var packageContents = await _packageService.GetPackageContentsAsync(packageId);
    
    return packageContents.Select(c => new AvailableItemDto
    {
        ItemCode = c.ItemCode,
        AvailableQuantity = c.Quantity,
        UnitCode = c.UnitCode,
        BatchNo = c.BatchNo,
        SerialNo = c.SerialNo,
        IsRequired = requiredItems?.Any(r => r.ItemCode == c.ItemCode) ?? false,
        RequiredQuantity = requiredItems?.FirstOrDefault(r => r.ItemCode == c.ItemCode)?.Quantity ?? 0
    }).ToList();
}
```

### 3.4.2 Enhanced Picking Models

```csharp
public class AddPickingItemRequest
{
    public int PickingId { get; set; }
    public string ScannedBarcode { get; set; }
    public string ItemCode { get; set; }
    public decimal PickedQuantity { get; set; }
    public string UnitCode { get; set; }
    public string WhsCode { get; set; }
    public int? BinEntry { get; set; }
    public string BinCode { get; set; }
    public string BatchNo { get; set; }
    public string SerialNo { get; set; }
    public string UserId { get; set; }
    
    // Package-related properties
    public Guid? CurrentPackageId { get; set; }
    public string ExpectedWhsCode { get; set; }
    public int? ExpectedBinEntry { get; set; }
    public List<RequiredItemDto> RequiredItems { get; set; }
}

public class PackageItemInfo
{
    public Guid PackageId { get; set; }
    public string PackageBarcode { get; set; }
    public string ItemCode { get; set; }
    public decimal ItemQuantity { get; set; }
    public string UnitCode { get; set; }
    public string BatchNo { get; set; }
    public string SerialNo { get; set; }
}

public class BinSuggestionDto
{
    public string WhsCode { get; set; }
    public int? BinEntry { get; set; }
    public string BinCode { get; set; }
    public decimal AvailableQuantity { get; set; }
    public decimal RequiredQuantity { get; set; }
    public bool RequiresPackagePicking { get; set; }
    public string Message { get; set; }
    public List<PackageItemInfo> Packages { get; set; } = new();
}
```

## Implementation Status Summary

### ✅ Completed (Phase 3.1.1)
- **Goods Receipt Integration**: Package creation, item addition, and activation on completion
- **Service Architecture Refactoring**: Specialized services for better separation of concerns
- **Database Schema**: Complete package entities with relationships and validation
- **External Adapter Integration**: Bin code resolution via adapter pattern
- **Package Lifecycle Management**: Init → Active → Closed/Cancelled workflow

### ⏳ Pending Implementation
- **3.2 Counting Integration**: Package scanning and content validation during counting
- **3.3 Transfer Integration**: Package movement rules and cross-location transfers  
- **3.4 Picking Integration**: Package-aware picking with forced scanning logic

### Key Features Implemented
- **Package toggle functionality**: StartNewPackage flag in goods receipt operations
- **Package state management**: Automatic activation when goods receipt completes
- **Location consistency**: Package and content location synchronization
- **Service layer architecture**: Direct injection of specialized services in controllers
- **External system integration**: Bin code resolution through adapter pattern
- **Comprehensive validation**: Package status and constraint validation

### Architecture Benefits Achieved
- **50%+ code reduction**: PackageService reduced from 526 to 225 lines
- **Better separation of concerns**: Specialized services for content, location, validation
- **Improved testability**: Focused services easier to unit test
- **Enhanced maintainability**: Clear responsibility boundaries
- **Performance optimization**: Eliminated unnecessary service wrapper indirection

### Next Steps
- Complete remaining Phase 3 integrations (3.2, 3.3, 3.4)
- Phase 4: Advanced validation and consistency management
- Phase 5: Enhanced business rules and workflow automation  
- Phase 6: Configuration system and final integration