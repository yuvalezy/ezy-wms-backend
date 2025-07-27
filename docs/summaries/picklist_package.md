# Pick List Package Architecture and Workflow

## Overview

The Pick List Package system integrates warehouse package management with SAP Business One picking operations. It enables picking entire packages or individual items from packages while maintaining full traceability and inventory accuracy.

## Core Architecture

### Entity Relationships

```
PickList (Core.Entities.PickList)
├── AbsEntry: Pick operation identifier (SAP B1)
├── PickEntry: Pick line identifier
├── ItemCode: Item being picked
├── Quantity: Quantity picked
├── BinEntry: Location where item was picked
└── Status: Open/Processing/Closed

PickListPackage (Core.Entities.PickListPackage)
├── AbsEntry: Pick operation identifier
├── PickEntry: Pick line identifier  
├── PackageId: Source package reference
├── Type: Source/Target (always Source for pick lists)
├── BinEntry: Package location
├── ProcessedAt: Closure processing timestamp
└── Navigation to Package entity

Package (referenced entity)
├── Contents: List of PackageContent
├── Commitments: List of PackageCommitment
├── Status: Active/Locked/Closed
└── BinEntry: Current location

PackageCommitment (tracks package reservations)
├── PackageId: Package being committed
├── ItemCode: Item committed
├── Quantity: Quantity committed
├── SourceOperationType: Picking
└── SourceOperationId: PickList.Id reference
```

### Service Layer Architecture

```
PickingController
├── AddItem() → PickListLineService.AddItem()
├── AddPackage() → PickListPackageService.AddPackageAsync()
├── Process() → PickListProcessService.ProcessPickList()
└── Cancel() → PickListCancelService.CancelPickListAsync()

Service Dependencies:
PickListLineService
├── IExternalSystemAdapter (SAP validation)
├── IPackageContentService (package updates)
└── SystemDbContext (data persistence)

PickListPackageService
├── PickListPackageEligibilityService (validation)
├── IPackageContentService (package operations)
└── IExternalSystemAdapter (SAP integration)

PickListCancelService
├── ITransferService (cancellation transfers)
├── IPackageLocationService (package movements)
└── IPackageContentService (package updates)
```

## Workflow Processes

### 0. Create packge for picking 
###
	Package
		SourceOperationType = PickingPackage
		Status = Picking
		SourceOperationId = PickListPackge.Id (update after creation of package)
	PickListPackage
		All fields
		Type = Target
		BinEntry = StagingBinEntry
###

### 1. Adding Individual Items from Packages

**API Endpoint:** `POST /api/picking/addItem`

**Process Flow:**
1. **Package Validation** (PickListLineService.cs:33-86)
   - Load package with contents
   - Validate package status (Active, not Locked)
   - Verify bin location match
   - Check item exists in package
   - Validate available quantity vs committed quantity

2. **SAP Validation** (PickListLineService.cs:88-102)
   - Call adapter.ValidatePickingAddItem()
   - Verify item exists in SAP pick list
   - Check open quantities

3. **Inventory Validation** (PickListLineService.cs:104-141)
   - Calculate existing commitments
   - Verify bin on-hand availability
   - Ensure quantity doesn't exceed limits

4. **Database Operations** (PickListLineService.cs:143-200)
   - Create PickList record
   - Update PackageContent.CommittedQuantity
   - Create PackageCommitment record
   - Create/update PickListPackage record
   
###
	If request.PickingPackageId is not null
	Add to Package Content
		BinEntry => SourceBinEntry
###

### 2. Adding Entire Packages

**API Endpoint:** `POST /api/picking/addPackage`

**Process Flow:**
1. **Package Validation** (PickListPackageService.cs:24-54)
   - Load package with all contents
   - Validate package status and location
   - Check not already added to pick list

2. **Pick List Validation** (PickListPackageService.cs:56-87)
   - Get pick list details from SAP
   - Calculate open quantities accounting for existing picks
   - Build item open quantities dictionary

3. **Eligibility Check** (PickListPackageService.cs:89-95)
   - Use PickListPackageEligibilityService
   - Verify all package contents can be fully picked
   - Validate against available open quantities

4. **Batch Item Processing** (PickListPackageService.cs:97-190)
   - For each package content item:
     - Validate with SAP adapter
     - Check inventory availability
     - Create PickList record
     - Update committed quantities
     - Create package commitments

5. **Package Registration** (PickListPackageService.cs:192-206)
   - Create PickListPackage record
   - Set PickEntry to -1 (indicates full package)
   
###
	If request.PickingPackageId is not null
	Add to Package Content
		BinEntry => SourceBinEntry
###

### 4. Pick List Processing and Closure

**Process Flow:**
1. **Background Sync** (BackgroundPickListSyncService.cs:49-81)
   - Periodic sync of pending pick lists
   - Process closed pick lists with packages
   - Handle SAP synchronization

2. **Closure Detection** (PickListDetailService.cs:225-268)
   - Identify closed/synced pick lists
   - Find unprocessed packages
   - Get closure information from SAP

3. **Package Movement Processing** (PickListPackageService.cs:296-455)
   - Map follow-up documents to package commitments
   - Calculate actual quantities moved per package
   - Remove items from source packages
   - Update package status to Closed if empty
   - Clear package commitments 
###
	If request.PickingPackageId is not null
	Add to Package Content
		BinEntry => SourceBinEntry
###

### 5. Pick List Cancellation

**API Endpoint:** `POST /api/picking/cancel`

**Process Flow:**
1. **Preparation** (PickListCancelService.cs:27-58)
   - Process any pending items in SAP
   - Get cancel bin from settings
   - Cancel pick list in SAP
   - Create transfer for cancelled items

2. **Package Handling** (PickListCancelService.cs:61-113)
   - Group packages by PackageId
   - Determine full vs partial commitments
   - Process each package group

3. **Full Package Cancellation** (PickListCancelService.cs:115-151)
   - Clear package commitments first
   - Move entire package to cancel bin
   - Add package to transfer
   - Mark all package items as processed

4. **Partial Package Cancellation** (PickListCancelService.cs:153-193)
   - Clear package commitments
   - Remove committed quantities from package
   - Add individual items to transfer
   - Track processed items

5. **Regular Item Processing** (PickListCancelService.cs:195-248)
   - Handle non-package items
   - Calculate unit breakdowns (packs/dozens/units)
   - Add to transfer with proper units

## Key Design Patterns

### 1. Transaction Management
- All package operations use database transactions
- Rollback on any failure to maintain consistency
- Package commitments updated atomically

### 2. Commitment Pattern
- PackageCommitment tracks reserved quantities
- CommittedQuantity prevents double-allocation
- Cleared during cancellation or closure

### 3. Eligibility Validation
- PickListPackageEligibilityService validates full package picking
- Prevents partial package selection when not all items needed
- Supports package consolidation strategies

### 4. Status Tracking
- PickListPackage.ProcessedAt prevents reprocessing
- Package.Status lifecycle management
- SyncStatus for SAP synchronization tracking

### 5. Bin Location Consistency
- Package movements tracked through IPackageLocationService
- Bin validation during package operations
- Location history maintained

## Integration Points

### SAP Business One Integration
- **ValidatePickingAddItem**: Validates items against pick list
- **GetPickingDetailItems**: Retrieves pick list item details
- **GetPickListClosureInfo**: Gets follow-up document information
- **CancelPickList**: Cancels pick list in SAP

### Package Management Integration
- **IPackageContentService**: Manages package contents
- **IPackageLocationService**: Handles package movements
- **PackageCommitment**: Tracks package reservations

### Transfer Integration
- **ITransferService**: Creates transfers for cancelled items
- **ITransferPackageService**: Handles package transfers
- **ITransferLineService**: Manages individual item transfers

## Error Handling and Recovery

### Validation Failures
- Package status validation prevents invalid operations
- Quantity validation ensures inventory accuracy
- SAP validation maintains data consistency

### Transaction Rollback
- Database transactions ensure atomicity
- Package commitments rolled back on failure
- Partial processing prevented

### Background Recovery
- BackgroundPickListSyncService handles delayed processing
- ProcessClosedPickListsWithPackages recovers unprocessed closures
- Retry mechanisms for transient failures

## Performance Considerations

### Batch Operations
- Package contents processed in single transaction
- Bulk database updates for commitments
- Efficient queries with proper indexing

### Caching Strategy
- Package contents loaded once per operation
- Item validation results cached during batch processing
- Open quantity calculations optimized

### Async Processing
- Background sync prevents blocking operations
- Closure processing handles large package movements
- Parallel processing where safe