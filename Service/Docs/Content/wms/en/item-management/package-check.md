---
title: Package Check
description: Query package contents, location, and metadata.
---

Package Check allows you to view the contents, current location, and metadata of any package in the warehouse.

## Accessing the Page

Route: `/packageCheck`

Navigation: Dashboard → Package Check

You can also access directly with parameters: `/packageCheck/:id/:barcode`

## How to Use

1. Scan or enter the **package barcode**.
2. The system searches for the package and displays:
   - Package status (Initialized, Active, Closed, Cancelled, Locked)
   - Current bin location and warehouse
   - List of all items inside the package
   - Total unique items and total quantity
   - Metadata values (if configured)

## Package Statuses

| Status | Description |
|--------|-------------|
| Initialized | Package created but empty |
| Active | Package has items and is in use |
| Closed | Package is sealed and complete |
| Cancelled | Package was cancelled |
| Locked | Package is temporarily locked |

## Package Contents

The contents table shows:

| Field | Description |
|-------|-------------|
| Item Code | Product code |
| Item Name | Product description |
| Quantity | Amount in package |
| Unit Type | Unit of measure |
| Bin Code | Storage location |

## Metadata

If the package has configured metadata fields, they appear in a dedicated section. Click **Edit Metadata** to modify values.

## Error Messages

| Message | Cause |
|---------|-------|
| Package not found | The barcode does not match any package |
| No package content found | The package exists but is empty |

## Related Pages

- [Item Check](/docs/item-management/item-check)
- [Bin Check](/docs/item-management/bin-check)
