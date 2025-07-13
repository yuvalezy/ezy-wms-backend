# Pick List Package Support Enhancement Plan

## Overview

This document outlines the implementation plan for adding comprehensive package support to the pick list system, enabling both full and partial package picking workflows with proper commitment tracking and ERP integration.

## Current State

- Pick lists are represented by individual `PickList` entities with `AbsEntry + PickEntry` composite keys
- Package system exists with commitment tracking for transfers
- No current integration between picking and package management
- ERP sync service monitors pick list status changes

## Goals

1. **Source Package Integration** - Assign existing packages to pick operations with commitment tracking
2. **Auto-Pick Intelligence** - Automatically fulfill pick requirements when package contents match
3. **Partial Picking Support** - Pick specific quantities from packages with proper commitment management
4. **Target Package Creation** - Create new packages during picking operations
5. **ERP Lifecycle Management** - Activate packages when delivery notes are created in ERP

## Phase 1: Data Model Extensions ✅

### 1.1 New Entity: `PickListPackage` ✅

```csharp
public sealed class PickListPackage : BaseEntity {
    [Required]
    public int AbsEntry { get; set; }           // Pick operation identifier
    
    [Required] 
    public int PickEntry { get; set; }          // Pick line identifier
    
    [Required]
    public Guid PackageId { get; set; }         // Source or target package
    
    [Required]
    public SourceTarget Type { get; set; }      // Source or Target
    
    public int? BinEntry { get; set; }          // Location information
    
    [Required]
    public DateTime AddedAt { get; set; } = DateTime.UtcNow;
    
    [Required]
    public Guid AddedByUserId { get; set; }
    
    // Navigation properties
    public Package Package { get; set; } = null!;
}
```

**Composite Key**: `(AbsEntry, PickEntry, PackageId, Type)`

### 1.2 Database Configuration ✅

```csharp
public class PickListPackageConfiguration : IEntityTypeConfiguration<PickListPackage> {
    public void Configure(EntityTypeBuilder<PickListPackage> builder) {
        builder.HasKey(p => new { p.AbsEntry, p.PickEntry, p.PackageId, p.Type });
        
        builder.HasIndex(p => new { p.AbsEntry, p.PickEntry })
            .HasDatabaseName("IX_PickListPackage_Operation");
            
        builder.HasIndex(p => p.PackageId)
            .HasDatabaseName("IX_PickListPackage_Package");
    }
}
```

### 1.3 Enhanced DTOs ✅

```csharp
// New requests
public class PickListAddSourcePackageRequest {
    public int AbsEntry { get; set; }
    public int PickEntry { get; set; }
    public Guid PackageId { get; set; }
    public int? BinEntry { get; set; }
}

public class PickListAutoPickRequest {
    public int AbsEntry { get; set; }
    public int PickEntry { get; set; }
    public Guid SourcePackageId { get; set; }
    public Guid? TargetPackageId { get; set; }  // Optional - create new if not provided
}

// Enhanced existing request
public class PickListAddItemRequest {
    // ... existing fields ...
    public Guid? SourcePackageId { get; set; }  // NEW: For partial picking from packages
}
```

## Phase 2: Service Layer Implementation ✅

### 2.1 New Service: `IPickListPackageService` ✅

```csharp
public interface IPickListPackageService {
    Task<PickListPackageResponse> HandleSourcePackageScanAsync(
        PickListAddSourcePackageRequest request, SessionInfo sessionInfo);
        
    Task<PickListPackageResponse> HandleAutoPickPackageAsync(
        PickListAutoPickRequest request, SessionInfo sessionInfo);
        
    Task ClearPickListCommitmentsAsync(
        int absEntry, int pickEntry, SessionInfo sessionInfo);
        
    Task ClearAllPickListCommitmentsAsync(
        int absEntry, SessionInfo sessionInfo);  // For operation cancellation
}
```

### 2.2 Auto-Pick Algorithm (Core Logic) ✅

```csharp
public async Task<bool> CanAutoPickPackageAsync(int absEntry, int pickEntry, Guid packageId) {
    // 1. Get all pending pick list items for this operation
    var pendingItems = await db.PickList
        .Where(pl => pl.AbsEntry == absEntry && 
                     pl.PickEntry == pickEntry && 
                     pl.Status == ObjectStatus.Open)
        .GroupBy(pl => pl.ItemCode)
        .Select(g => new { ItemCode = g.Key, RequiredQuantity = g.Sum(x => x.Quantity) })
        .ToListAsync();
    
    // 2. Get package contents
    var packageContents = await db.PackageContents
        .Where(pc => pc.PackageId == packageId)
        .ToListAsync();
    
    // 3. Check if package can fulfill ALL requirements exactly
    var canFulfill = pendingItems.All(required => {
        var packageItem = packageContents.FirstOrDefault(pc => pc.ItemCode == required.ItemCode);
        return packageItem != null && 
               (packageItem.Quantity - packageItem.CommittedQuantity) >= required.RequiredQuantity;
    });
    
    return canFulfill && !packageContents.Any(pc => pc.CommittedQuantity > 0);
}
```

### 2.3 Source Package Handling ✅

```csharp
public async Task<PickListPackageResponse> HandleSourcePackageScanAsync(
    PickListAddSourcePackageRequest request, SessionInfo sessionInfo) {
    
    await using var transaction = await db.Database.BeginTransactionAsync();
    try {
        // 1. Validate package exists and is available
        var package = await packageService.GetPackageAsync(request.PackageId);
        if (package == null) throw new ValidationException("Package not found");
        
        // 2. Check if already assigned
        var existing = await db.PickListPackages
            .AnyAsync(plp => plp.AbsEntry == request.AbsEntry && 
                           plp.PickEntry == request.PickEntry && 
                           plp.PackageId == request.PackageId && 
                           plp.Type == SourceTarget.Source);
        if (existing) throw new ValidationException("Package already assigned");
        
        // 3. Create PickListPackage record
        var pickListPackage = new PickListPackage {
            AbsEntry = request.AbsEntry,
            PickEntry = request.PickEntry,
            PackageId = request.PackageId,
            Type = SourceTarget.Source,
            BinEntry = request.BinEntry,
            AddedAt = DateTime.UtcNow,
            AddedByUserId = sessionInfo.Guid,
            CreatedAt = DateTime.UtcNow,
            CreatedByUserId = sessionInfo.Guid
        };
        
        db.PickListPackages.Add(pickListPackage);
        
        // 4. Create commitments for ALL package contents
        var packageContents = await db.PackageContents
            .Where(pc => pc.PackageId == request.PackageId)
            .ToListAsync();
            
        foreach (var content in packageContents) {
            content.CommittedQuantity += content.Quantity;
            db.PackageContents.Update(content);
            
            var commitment = new PackageCommitment {
                PackageId = request.PackageId,
                ItemCode = content.ItemCode,
                Quantity = content.Quantity,
                SourceOperationType = ObjectType.Picking,
                SourceOperationId = new Guid(request.AbsEntry.ToString()), // Convert to Guid
                // Note: May need to store PickEntry separately or create composite identifier
                CommittedAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow,
                CreatedByUserId = sessionInfo.Guid
            };
            
            db.PackageCommitments.Add(commitment);
        }
        
        await db.SaveChangesAsync();
        await transaction.CommitAsync();
        
        return new PickListPackageResponse { 
            Success = true,
            PackageId = package.Id,
            PackageContents = await Task.WhenAll(packageContents.Select(async c => await c.ToDto(adapter)))
        };
    }
    catch {
        await transaction.RollbackAsync();
        throw;
    }
}
```

### 2.4 Additional Service Methods Implemented ✅

Beyond the planned interface, the full `PickListPackageService` implementation includes:

```csharp
public class PickListPackageService : IPickListPackageService {
    // Core service methods implemented:
    
    public async Task<bool> CanAutoPickPackageAsync(int absEntry, int pickEntry, Guid packageId);
    // - Validates package contents against pending pick requirements
    // - Ensures no existing commitments or extra items
    // - Returns true only for perfect match scenarios
    
    public async Task<PickListPackageResponse> HandleAutoPickPackageAsync(
        PickListAutoPickRequest request, SessionInfo sessionInfo);
    // - Complete auto-pick workflow implementation
    // - Creates target packages automatically if not provided
    // - Processes all pending pick list items in single transaction
    // - Handles both source commitments and target package creation
    
    public async Task<PickListAddItemResponse> HandlePartialPickAsync(
        PickListAddItemRequest request, SessionInfo sessionInfo);
    // - Supports partial picking from source packages
    // - Creates commitments for picked quantities only
    // - Integrates with existing package content management
    // - Validates item availability before commitment
    
    public async Task ClearPickListCommitmentsAsync(int absEntry, int pickEntry, SessionInfo sessionInfo);
    // - Clears commitments for specific pick operation
    // - Removes PickListPackage records
    // - Restores package content availability
    
    public async Task ClearAllPickListCommitmentsAsync(int absEntry, SessionInfo sessionInfo);
    // - Clears all commitments for entire pick operation
    // - Supports operation-level cancellation
    // - Handles multiple pick entries atomically
}
```

**Key Implementation Features:**

1. **Composite Key Handling** - Proper conversion of `AbsEntry` and `PickEntry` to Guid format using padding strategy
2. **Transaction Safety** - All operations wrapped in database transactions with proper rollback
3. **Commitment Tracking** - Full integration with `PackageCommitment` system for `ObjectType.Picking`
4. **Auto-Pick Intelligence** - Sophisticated validation ensuring exact requirement matching
5. **Package Lifecycle** - Creates target packages with `PackageStatus.Init` for ERP activation
6. **Error Handling** - Comprehensive validation with clear error messages

## Phase 3: Controller Implementation ⏳

### 3.1 New Pick List Package Controller ⏳

```csharp
[ApiController]
[Route("api/picklist/packages")]
[Authorize]
public class PickListPackageController(
    IPickListPackageService pickListPackageService,
    ISettings settings) : ControllerBase {
    
    [HttpPost("addSource")]
    [RequireRolePermission(RoleType.Picking)]
    public async Task<PickListPackageResponse> AddSourcePackage(
        [FromBody] PickListAddSourcePackageRequest request) {
        
        if (!settings.Options.EnablePackages) {
            return new PickListPackageResponse { ErrorMessage = "Package feature not enabled" };
        }
        
        var sessionInfo = HttpContext.GetSession();
        return await pickListPackageService.HandleSourcePackageScanAsync(request, sessionInfo);
    }
    
    [HttpPost("autoPick")]
    [RequireRolePermission(RoleType.Picking)]
    public async Task<PickListPackageResponse> AutoPickPackage(
        [FromBody] PickListAutoPickRequest request) {
        
        if (!settings.Options.EnablePackages) {
            return new PickListPackageResponse { ErrorMessage = "Package feature not enabled" };
        }
        
        var sessionInfo = HttpContext.GetSession();
        return await pickListPackageService.HandleAutoPickPackageAsync(request, sessionInfo);
    }
}
```

### 3.2 Enhanced Pick List AddItem ⏳

Modify existing pick list AddItem endpoint to support `SourcePackageId`:

```csharp
[HttpPost("addItem")]
public async Task<PickListAddItemResponse> AddItem([FromBody] PickListAddItemRequest request) {
    var sessionInfo = HttpContext.GetSession();
    
    if (request.SourcePackageId.HasValue && settings.Options.EnablePackages) {
        // Handle partial picking from package with commitment tracking
        return await pickListPackageService.HandlePartialPickAsync(request, sessionInfo);
    }
    
    // Existing logic for non-package picking
    return await pickListService.AddItem(request, sessionInfo);
}
```

## Phase 4: Background Processing Enhancement

### 4.1 Extended BackgroundPickListSyncService

```csharp
public async Task SyncPendingPickLists() {
    // Existing sync logic...
    
    // NEW: Handle package activation when delivery notes are created
    await SyncPickListPackageActivation();
}

private async Task SyncPickListPackageActivation() {
    // 1. Query ERP for newly created delivery notes
    var newDeliveryNotes = await adapter.GetNewDeliveryNotesAsync();
    
    foreach (var deliveryNote in newDeliveryNotes) {
        // 2. Map delivery note back to AbsEntry + PickEntry
        var pickOperations = await MapDeliveryNoteToPickOperations(deliveryNote);
        
        foreach (var operation in pickOperations) {
            // 3. Activate target packages for this pick operation
            await ActivateTargetPackagesAsync(operation.AbsEntry, operation.PickEntry);
            
            // 4. Clear commitments for this pick operation
            await pickListPackageService.ClearPickListCommitmentsAsync(
                operation.AbsEntry, operation.PickEntry, systemSessionInfo);
        }
    }
}

private async Task ActivateTargetPackagesAsync(int absEntry, int pickEntry) {
    var targetPackages = await db.PickListPackages
        .Where(plp => plp.AbsEntry == absEntry && 
                     plp.PickEntry == pickEntry && 
                     plp.Type == SourceTarget.Target)
        .Include(plp => plp.Package)
        .ToListAsync();
    
    foreach (var targetPackage in targetPackages) {
        if (targetPackage.Package.Status == PackageStatus.Init) {
            targetPackage.Package.Status = PackageStatus.Active;
            targetPackage.Package.UpdatedAt = DateTime.UtcNow;
            db.Packages.Update(targetPackage.Package);
        }
    }
    
    await db.SaveChangesAsync();
}
```

## Phase 5: Pick List Cancellation Integration

### 5.1 Enhanced Cancellation Logic

When pick lists are cancelled, clear all associated package commitments:

```csharp
public async Task CancelPickList(int absEntry, int pickEntry, SessionInfo sessionInfo) {
    // Existing cancellation logic...
    
    // NEW: Clear package commitments
    if (settings.Options.EnablePackages) {
        await pickListPackageService.ClearPickListCommitmentsAsync(absEntry, pickEntry, sessionInfo);
    }
}

public async Task CancelEntirePickOperation(int absEntry, SessionInfo sessionInfo) {
    // Cancel all pick entries for this AbsEntry
    
    // NEW: Clear all package commitments for this operation
    if (settings.Options.EnablePackages) {
        await pickListPackageService.ClearAllPickListCommitmentsAsync(absEntry, sessionInfo);
    }
}
```

## Phase 6: Package Creation Integration

### 6.1 Leverage Existing Package Infrastructure

Use existing `PackageController.CreatePackage` with picking-specific parameters:

```csharp
// Target package creation during auto-pick
var createPackageRequest = new CreatePackageRequest {
    SourceOperationType = ObjectType.Picking,
    SourceOperationId = absEntry,  // May need to create composite identifier
    WhsCode = sessionInfo.Warehouse,
    BinEntry = targetBinEntry,
    Notes = $"Auto-picked from package {sourcePackage.Barcode}"
};

var targetPackage = await packageService.CreatePackageAsync(sessionInfo, createPackageRequest);
```

## Implementation Benefits

### 1. Complete Package Lifecycle
- **Source packages** properly committed during picking operations
- **Target packages** created with Init status and activated via ERP sync
- **Full audit trail** through PackageCommitment and PackageTransaction records

### 2. Flexible Picking Workflows
- **Auto-pick optimization** when package contents exactly match requirements
- **Partial picking support** with proper commitment tracking
- **Manual package creation** for picked items not from existing packages

### 3. ERP Integration
- **Delivery note detection** triggers package activation
- **Commitment cleanup** when operations complete in ERP
- **Bidirectional sync** ensures consistency between WMS and ERP

### 4. Data Consistency
- **Transaction-based operations** ensure atomicity
- **Proper rollback** on failures
- **Constraint enforcement** prevents double-allocation

## Technical Considerations

### 1. Composite Key Challenges
- `PickListPackage` uses composite primary key `(AbsEntry, PickEntry, PackageId, Type)`
- May need custom identifier strategy for `PackageCommitment.SourceOperationId`
- Consider additional fields for storing `PickEntry` information

### 2. Performance Optimization
- **Efficient indexing** on `(AbsEntry, PickEntry)` combinations
- **Batch operations** for large pick operations
- **Query optimization** for auto-pick algorithm

### 3. Concurrency Management
- **Proper locking** during auto-pick operations to prevent race conditions
- **Optimistic concurrency** for package commitment updates
- **Transaction isolation** for complex multi-table operations

### 4. Error Handling
- **Graceful degradation** when package features are disabled
- **Comprehensive validation** for package availability and pick requirements
- **Clear error messages** for users when operations fail

## Migration Strategy

### 1. Database Changes
- Create `PickListPackage` table with proper constraints
- Add indexes for performance
- Update `PackageCommitment` if needed for pick operation tracking

### 2. Service Registration
- Register new `IPickListPackageService` in dependency injection
- Update existing pick list services to support package integration

### 3. Controller Updates
- Add new package-specific endpoints
- Enhance existing endpoints with optional package parameters
- Maintain backward compatibility

### 4. Background Processing
- Extend existing sync service with package activation logic
- Add ERP mapping for delivery note to pick operation correlation
- Implement proper error handling and retry logic

## Success Metrics

1. **Operational Efficiency**
   - Reduced picking time through auto-pick capabilities
   - Improved inventory accuracy through package tracking
   - Enhanced audit trail for compliance requirements

2. **System Integration**
   - Seamless ERP synchronization for package lifecycle
   - Proper commitment management preventing double-allocation
   - Consistent data between WMS and ERP systems

3. **User Experience**
   - Intuitive package scanning workflows
   - Clear feedback on package availability and pick status
   - Flexible picking options (auto, partial, manual)

## Conclusion

This enhancement plan provides comprehensive package support for pick list operations while maintaining compatibility with existing workflows. The phased approach ensures minimal disruption during implementation while delivering significant operational improvements through intelligent auto-pick capabilities and complete package lifecycle management.