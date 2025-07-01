# Phase 3: Operation Integration

## 3.1 Goods Receipt Integration

### 3.1.1 Enhanced GoodsReceiptController

```csharp
[HttpPost("{id}/items")]
public async Task<ActionResult<GoodsReceiptLineDto>> AddItem(int id, [FromBody] AddGoodsReceiptItemRequest request)
{
    try
    {
        // Enhanced request to support package operations
        if (request.StartNewPackage && IsPackageFeatureEnabled())
        {
            // Create new package for this operation
            var package = await _packageService.CreatePackageAsync(new CreatePackageRequest
            {
                WhsCode = request.WhsCode,
                BinEntry = request.BinEntry,
                BinCode = request.BinCode,
                UserId = EmployeeID,
                SourceOperationType = "GoodsReceipt",
                SourceOperationId = Guid.Parse(id.ToString())
            });
            
            request.PackageId = package.Id;
        }
        
        // Standard goods receipt line creation
        var line = await Data.GoodsReceipt.AddItemAsync(request);
        
        // Add item to package if package operation is active
        if (request.PackageId.HasValue)
        {
            await _packageService.AddItemToPackageAsync(new AddItemToPackageRequest
            {
                PackageId = request.PackageId.Value,
                ItemCode = request.ItemCode,
                Quantity = request.Quantity,
                UnitCode = request.UnitCode,
                BatchNo = request.BatchNo,
                SerialNo = request.SerialNo,
                WhsCode = request.WhsCode,
                BinEntry = request.BinEntry,
                BinCode = request.BinCode,
                UserId = EmployeeID,
                SourceOperationType = "GoodsReceipt",
                SourceOperationId = Guid.Parse(id.ToString()),
                SourceOperationLineId = line.Id
            });
        }
        
        // Return enhanced response with package info
        var response = line.ToDto();
        if (request.PackageId.HasValue)
        {
            var package = await _packageService.GetPackageAsync(request.PackageId.Value);
            response.PackageId = package.Id;
            response.PackageBarcode = package.Barcode;
        }
        
        return Ok(response);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error adding item to goods receipt {Id}", id);
        return BadRequest(new { error = ex.Message });
    }
}

[HttpPost("{id}/complete")]
public async Task<ActionResult> CompleteGoodsReceipt(int id)
{
    using var transaction = await _context.Database.BeginTransactionAsync();
    try
    {
        // Complete goods receipt
        await Data.GoodsReceipt.CompleteAsync(id, EmployeeID);
        
        // Activate any packages created during this operation
        var packages = await _context.Packages
            .Where(p => p.Status == PackageStatus.Init && 
                       p.CustomAttributes.Contains($"\"SourceOperationId\":\"{id}\""))
            .ToListAsync();
        
        foreach (var package in packages)
        {
            package.Status = PackageStatus.Active;
            package.UpdatedAt = DateTime.UtcNow;
            package.UpdatedBy = EmployeeID;
        }
        
        await _context.SaveChangesAsync();
        await transaction.CommitAsync();
        
        _logger.LogInformation("Goods receipt {Id} completed with {PackageCount} packages activated", 
            id, packages.Count);
        
        return Ok(new { activatedPackages = packages.Count });
    }
    catch (Exception ex)
    {
        await transaction.RollbackAsync();
        _logger.LogError(ex, "Error completing goods receipt {Id}", id);
        return BadRequest(new { error = ex.Message });
    }
}

private bool IsPackageFeatureEnabled()
{
    return _configuration.GetValue<bool>("Options:enablePackages", false);
}
```

### 3.1.2 Enhanced AddGoodsReceiptItemRequest

```csharp
public class AddGoodsReceiptItemRequest
{
    // Existing properties
    public string ItemCode { get; set; }
    public decimal Quantity { get; set; }
    public string UnitCode { get; set; }
    public string WhsCode { get; set; }
    public int? BinEntry { get; set; }
    public string BinCode { get; set; }
    public string BatchNo { get; set; }
    public string SerialNo { get; set; }
    public DateTime? ExpiryDate { get; set; }
    
    // Package-related properties
    public bool StartNewPackage { get; set; }
    public Guid? PackageId { get; set; }
}

public class GoodsReceiptLineDto
{
    // Existing properties
    public Guid Id { get; set; }
    public string ItemCode { get; set; }
    public string ItemName { get; set; }
    public decimal Quantity { get; set; }
    public string UnitCode { get; set; }
    public string WhsCode { get; set; }
    public string BinCode { get; set; }
    public string BatchNo { get; set; }
    public string SerialNo { get; set; }
    
    // Package-related properties
    public Guid? PackageId { get; set; }
    public string PackageBarcode { get; set; }
}
```

## 3.2 Counting Integration

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

## 3.3 Transfer Integration

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

## 3.4 Picking Integration

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

## Implementation Notes

### Timeline: Week 5-7
- Goods Receipt integration with package toggle functionality
- Counting integration with package scanning capability
- Transfer integration with package movement rules
- Picking integration with forced package scanning logic

### Key Features
- **Per-operation package toggle**: Users can start package mode for each operation
- **Package state management**: Init → Active workflow for goods receipt
- **Location consistency**: All operations enforce package/content location synchronization
- **Business rules enforcement**: Transfer rules (same-bin partial, cross-location full)
- **Forced package scanning**: Picking requires package scan when insufficient loose stock
- **Comprehensive validation**: All operations validate package status and constraints

### Integration Points
- Enhanced existing controller methods with package-aware logic
- Extended request/response models to include package information
- Integrated with existing barcode scanning infrastructure
- Maintained backward compatibility with non-package operations

### Next Steps
- Phase 4: Advanced validation and consistency management
- SAP integration patterns for package operations
- Enhanced error handling and business rule validation