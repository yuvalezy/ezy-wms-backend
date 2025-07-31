# Picking New Package Process - Complete Workflow

This document provides a comprehensive step-by-step breakdown of the picking new package process, derived from the unit test `PickingNewPackage.cs` and tracing through all the services and their dependencies.

## Overview

The picking new package process allows users to create a new target package during picking operations and consolidate items from multiple sources (loose items, partial packages, and full packages) into this new package. The process involves complex package management, commitment tracking, and transaction auditing.

## Complete Step-by-Step Workflow

### Phase 1: Data Preparation
**Test Method:** `PrepareData()`

1. **Create Test Items**
   - **Class:** `CreateTestItem`
   - **Method:** `Execute()`
   - **Action:** Creates 4 test items in SAP B1 (3 for package-managed items, 1 for non-package item)

2. **Create Goods Receipt for Package Items**
   - **Class:** `CreateGoodsReceipt`
   - **Method:** `Execute()`
   - **Action:** Creates GRPO in SAP B1 with package management enabled
   - **Result:** Generates packages with package contents

3. **Create Goods Receipt for Non-Package Item**
   - **Class:** `CreateGoodsReceipt`
   - **Method:** `Execute()`
   - **Action:** Creates GRPO in SAP B1 without package management

### Phase 2: Sales Order Creation & Pick List Generation
**Test Method:** `CreateSaleOrder_ReleaseToPicking()`

4. **Create Sales Order**
   - **Class:** `CreateSalesOrder`
   - **Method:** `Execute()`
   - **Action:** Creates sales order in SAP B1 for all test items
   - **Result:** Generates DocEntry (salesEntry) and AbsEntry (absEntry) for pick list

### Phase 3: Target Package Creation
**Test Method:** `CreatePicking_NewPackage()`

5. **Create New Target Package**
   - **Class:** `PickListPackageService`
   - **Method:** `CreatePackageAsync(absEntry, sessionInfo)`
   - **Steps:**
     - Validates staging bin configuration
     - Creates `CreatePackageRequest` with staging bin entry
     - Calls `IPackageService.CreatePackageAsync()` to create the physical package
     - Creates `PickListPackage` record with `Type = SourceTarget.Target`
     - Links package to pick list via `SourceOperationId`
   - **Database Changes:**
     - Inserts record in `Packages` table (Status: Init, BinEntry: staging bin)
     - Inserts record in `PickListPackages` table (Type: Target)

### Phase 4: Adding Items to Target Package

#### 4A: Add Non-Package Item
**Test Method:** `AddItemNoContainer_IntoNewPackage()`

6. **Add Loose Item to Target Package**
   - **Class:** `PickListLineService`
   - **Method:** `AddItem(sessionInfo, request)`
   - **Steps:**
     - Validates unit conversion if needed
     - Calls `PickListValidationService.ValidateItemForPicking()`
     - Calculates bin on-hand quantity
     - Validates quantity against pick list requirements
     - Creates `PickList` entry (Status: Open, SyncStatus: Pending)
     - Calls `AddNewPackageContent()` to add content to target package
   - **Sub-Method:** `AddNewPackageContent()`
     - **Class:** `PickListPackageOperationsService`
     - **Method:** `AddOrUpdatePackageContent()`
     - **Action:** Adds or updates content in target package
   - **Database Changes:**
     - Inserts `PickList` record
     - Inserts/Updates `PackageContents` in target package
     - Updates committed quantities

#### 4B: Add Partial Package Item
**Test Method:** `AddPartialFromPackage_IntoNewPackage()`

7. **Add Partial Quantity from Source Package**
   - **Class:** `PickListLineService`
   - **Method:** `AddItem(sessionInfo, request)` (with PackageId specified)
   - **Steps:**
     - Calls `PickListPackageOperationsService.ValidatePackageForItem()`
     - Validates source package status and availability
     - Creates `PickList` entry
     - Calls `AddItemPackage()` for source package handling
     - Calls `AddNewPackageContent()` for target package
   - **Sub-Method:** `AddItemPackage()`
     - Updates source package content committed quantity
     - Calls `CreatePackageCommitment()` to create commitment record
     - Calls `CreatePickListPackageIfNotExists()` for source package tracking
   - **Database Changes:**
     - Inserts `PickList` record
     - Updates `PackageContents.CommittedQuantity` in source package
     - Inserts `PackageCommitment` record (source → target relationship)
     - Inserts `PickListPackage` record for source package (Type: Source)
     - Inserts/Updates `PackageContents` in target package

#### 4C: Add Full Package
**Test Method:** `AddFullPackage_IntoNewPackage()`

8. **Add Entire Source Package**
   - **Class:** `PickListPackageService`
   - **Method:** `AddPackageAsync(request, sessionInfo)`
   - **Steps:**
     - Calls `PickListPackageOperationsService.ValidatePackageForFullPicking()`
     - Gets picking details from external adapter
     - Calculates open quantities
     - Validates package eligibility for full picking
     - Creates `PickList` entries for each package content item
     - Creates package commitments for all items
     - Adds contents to target package if specified
     - Creates `PickListPackage` record for source
   - **Database Changes:**
     - Multiple `PickList` records (one per item in package)
     - Updates all `PackageContents.CommittedQuantity` in source package
     - Multiple `PackageCommitment` records
     - Inserts `PickListPackage` record for source package
     - Multiple content additions to target package

### Phase 5: Content Validation
**Test Method:** `Validate_NewPackageContent()`

9. **Validate Target Package State**
   - **Verification Points:**
     - Target package has correct status (Init) and bin location (staging)
     - Target package contains expected 3 items with correct quantities
     - Source packages have correct committed quantities
     - All package commitments exist with proper relationships
     - PickListPackage records are correctly created (1 target + 2 sources)

### Phase 6: Pick List Processing
**Test Method:** `Process_AssertPackagesMovements()`

10. **Process Pick List**
    - **Class:** `PickListProcessService`
    - **Method:** `ProcessPickList(absEntry, userId)`
    - **Steps:**
      - Loads open pick list items
      - Updates status to Processing and SyncStatus to Processing
      - Calls external adapter to process pick list in SAP B1
      - Updates status to Closed and SyncStatus to Synced on success

11. **Create Delivery Note**
    - **Class:** `CreateDeliveryNote`
    - **Method:** `Execute()`
    - **Action:** Creates delivery note in SAP B1 based on processed pick list

12. **Process Closed Pick Lists with Packages**
    - **Class:** `PickListDetailService`
    - **Method:** `ProcessClosedPickListsWithPackages()`
    - **Steps:**
      - Finds closed and synced pick lists with unprocessed packages
      - Gets closure information from external system
      - Calls package service to process closure

### Phase 7: Package Closure Processing
**Internal to ProcessClosedPickListsWithPackages()**

13. **Process Pick List Closure**
    - **Class:** `PickListPackageService`
    - **Method:** `ProcessPickListClosureAsync(absEntry, closureInfo, userId)`
    - **Delegates to:** `PickListPackageClosureService.ProcessPickListClosureAsync()`

14. **Target Package Movement Processing**
    - **Class:** `PickListPackageClosureService`
    - **Method:** `ProcessTargetPackageMovements(absEntry, userId)`
    - **Steps:**
      - Finds all target packages for the pick list
      - Gets all package commitments for the pick list
      - For each commitment, processes the movement from source to target
      - Creates audit trail transactions (Add to target, Remove from source)
      - Updates package quantities and statuses
      - Removes empty package contents, closes empty packages

15. **Follow-up Document Processing**
    - **Class:** `PickListPackageClosureService`
    - **Method:** `ProcessPackageMovementsFromFollowUpDocuments()`
    - **Action:** Processes final package movements based on delivery documents
    - **Creates:** Final removal transactions from target packages

16. **Commitment Cleanup**
    - **Class:** `PickListPackageClosureService`
    - **Method:** `ClearPickListCommitmentsAsync(absEntry, userId)`
    - **Steps:**
      - Finds all package commitments for the pick list
      - Reduces committed quantities in source packages
      - Removes commitment records
      - Updates PickListPackage records as processed

### Phase 8: Final Validation
**Test Methods:** `Validate_SourcePackages_AfterDeliveryNoteCreated_PickListFinished()` and `Validate_TargetPackage_AfterDeliveryNoteCreated_PickListFinished()`

17. **Source Package Final State Validation**
    - **Partial Source Package:** Status remains Active with reduced quantity, no commitments
    - **Full Source Package:** Status becomes Closed (empty), all contents removed
    - **Audit Trail:** Removal transactions recorded with proper references

18. **Target Package Final State Validation**
    - **Status:** Closed after delivery processing
    - **Contents:** Empty (removed during delivery)
    - **Audit Trail:** Both incoming (from picking) and outgoing (from delivery) transactions
    - **Net Effect:** All transactions balance to zero

## Key Services and Their Roles

### Primary Services
- **PickListPackageService**: Target package creation and full package addition
- **PickListLineService**: Individual item addition to pick lists
- **PickListProcessService**: Pick list processing and SAP B1 synchronization
- **PickListDetailService**: Orchestrates package closure processing

### Supporting Services
- **PickListPackageOperationsService**: Package validation and operations
- **PickListPackageClosureService**: Package movement and closure processing
- **PickListValidationService**: Item and quantity validation
- **IPackageService**: Core package management
- **IPackageContentService**: Package content operations
- **IExternalSystemAdapter**: SAP B1 integration

## Database Entities Involved

### Primary Tables
- **Packages**: Physical package records
- **PackageContents**: Items within packages with quantities
- **PackageCommitments**: Commitments between source and target packages
- **PackageTransactions**: Audit trail of all package movements
- **PickLists**: Individual pick list line items
- **PickListPackages**: Links packages to pick lists with source/target designation

### Transaction Flow
1. **Creation**: Items added to target package with committed quantities in sources
2. **Processing**: Pick list processed in SAP B1, statuses updated
3. **Closure**: Physical movements executed, audit trails created
4. **Cleanup**: Commitments cleared, packages finalized

This workflow demonstrates a sophisticated package management system with full traceability, transaction integrity, and proper integration with external ERP systems.