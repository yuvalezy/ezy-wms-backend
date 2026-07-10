---
title: Counting Process
description: Scan bins and count items for physical inventory.
---

The Counting Process page is where operators perform the physical inventory count by scanning bin locations and item barcodes.

## Accessing the Page

Route: `/counting/:scanCode`

Navigation: Dashboard → Counting → Select Count Number

## How to Use

### Step 1: Scan the Bin Location

1. Enter the counting number or select from the list.
2. Scan the **bin location barcode** (e.g., rack or shelf).
3. Verify on screen that the scanned location matches the physical location.
4. Confirm before proceeding.

### Step 2: Scan Items

1. Scan the **item barcode**.
2. Verify the item information matches what is physically present.
3. Enter or confirm the **counted quantity**.
4. Click **Accept** to register the item in the count.

### Step 3: Continue Counting

- Continue scanning more items in the same bin location.
- To change to another bin location within the same count:
  1. Click **Counting** to exit the current bin.
  2. Re-enter the same count number.
  3. Scan the new bin location.
  4. Repeat the item scanning process.

## Multiple Locations per Count

You can count several locations within a single counting entry, allowing partial or complete inventories of different warehouse zones in one operation.

## Counted vs Document Quantity

The system shows:

| Field | Description |
|-------|-------------|
| Document Quantity | Expected quantity from SAP |
| Counted Quantity | Quantity physically counted |
| Difference | Variance between expected and counted |

## Error Messages

| Message | Cause |
|---------|-------|
| Counting not valid | The counting number does not exist |
| Counting is closed | The counting has already been completed |
| Bin location not found | The scanned bin does not exist |
| Item not found in warehouse | The item is not enabled in the current warehouse |

## Related Pages

- [Counting Supervisor](/docs/counting/supervisor)
- [Summary Report](/docs/counting/summary-report)
