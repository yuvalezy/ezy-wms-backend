---
title: Picking Detail
description: Pick a specific line from a picking order.
---

The Picking Detail page allows operators to focus on a single line item within a picking, showing all available bins and quantities for that item.

## Accessing the Page

Route: `/pick/:idParam/:typeParam/:entryParam`

Navigation: Picking Process → Click a specific item line

## Page Sections

### Available Bins

Shows all bin locations where the item is stored, with:

| Field | Description |
|-------|-------------|
| Bin Location | Warehouse bin code |
| Available Quantity | Quantity available to pick |
| Unit | Unit of measure |

### Item Details

Displays the item code, name, and total open quantity for the picking line.

### Bins

Shows the suggested bin and alternative bins where the item can be picked from.

## How to Use

1. From the Picking Process page, click an item line.
2. The detail page shows all available bins.
3. Scan or select the **bin location** you are picking from.
4. Scan the **item barcode**.
5. Enter the **quantity**.
6. Select the **unit**.
7. Click **Accept**.

## Related Pages

- [Picking Process](/docs/picking/process)
- [Picking Supervisor](/docs/picking/supervisor)
