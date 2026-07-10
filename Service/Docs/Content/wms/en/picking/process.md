---
title: Picking Process
description: Scan and pick items for outgoing orders.
---

The Picking Process page is where operators scan and collect items from warehouse bin locations.

## Accessing the Page

Route: `/pick/:idParam`

Navigation: Dashboard → Picking → Select Picking Number

## How to Use

1. From the Picking list, click the picking number you want to work.
2. The system shows a list of items with suggested bin locations.
3. For each item:
   - Scan or navigate to the **bin location**.
   - Scan the **item barcode**.
   - Confirm or modify the **quantity**.
   - Select the **unit type** (box, dozen, unit).
   - Click **Accept**.
4. Click **Picking** to return to the list and continue with other items.

## Picking List Columns

| Column | Description |
|--------|-------------|
| Item Code | Product code |
| Item Name | Product description |
| Bin Location | Suggested bin to pick from |
| Open Quantity | Remaining quantity to pick |
| Picked Quantity | Already picked quantity |
| Unit | Unit of measure |

## Package Management

You can pick items into packages:

- **Scan Package** — associate items with an existing package.
- **New Package** — create a new package for the current picking.
- The system tracks which package each item was placed in.

## Completing a Picking

A picking is considered complete when all open quantities reach zero. At that point:

- The picking disappears from the WMS list.
- SAP is updated automatically.
- The Traffic Department can proceed with delivery creation.

## Error Messages

| Message | Cause |
|---------|-------|
| Picking not valid | The picking number does not exist |
| Picking is closed | The picking has already been completed |
| Picking status error | The picking is not in a processable status |
| Item not found in picking | The scanned item is not in the picking list |
| Pick pack only error | The customer accepts only closed packages |

## Related Pages

- [Picking Detail](/docs/picking/detail)
- [Picking Supervisor](/docs/picking/supervisor)
