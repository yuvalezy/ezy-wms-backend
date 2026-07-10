---
title: Goods Receipt Process
description: Scan and receive incoming merchandise.
---

The Goods Receipt Process page is where operators scan and record incoming merchandise.

## Accessing the Page

Route: `/goodsReceipt/:scanCode`

Navigation: Dashboard → Goods Receipt → Select Entry

## How to Use

1. From the Goods Receipt list, click the entry number you want to process.
2. Scan the **barcode** of each item being received.
3. The system displays item information and prompts for quantity confirmation.

## Scanning Items

After scanning a barcode:

1. The system shows the item name, code, and open quantity.
2. Confirm or modify the **scanned quantity**.
3. Select the **unit type**:
   - Box
   - Dozen
   - Unit
4. The system defaults to **Box** but you can change it.
5. Click **Accept** to register the item.

## Package Management

During receipt, you can associate items with packages:

- **Scan Package Mode** — scan a package barcode first, then scan items into it.
- **Scan Item Mode** — scan items individually without a package.
- **Create New Package** — generate a new package for the current items.
- **Clear Package** — disassociate the current package.

## Destination Options

When scanning items, the system may prompt for the destination:

| Option | Description |
|--------|-------------|
| Store in Warehouse | Place in regular warehouse storage |
| Separate for Customer Delivery | Set aside for a specific order |
| Send to Showroom | Move to showroom location |
| Separate for Supervisor | Hold for supervisor review |

## Comments

You can add comments to any line:

1. Click **Add Comment**.
2. Enter your observation (e.g., damaged packaging, quantity discrepancy).
3. Save.

## Modifying Quantities

If you need to change a quantity after scanning:

1. Find the item in the list.
2. Click **Edit Quantity**.
3. Enter the corrected quantity.
4. Save.

## Error Messages

| Message | Cause |
|---------|-------|
| Entry not valid | The scanned entry number does not exist |
| Entry is closed | The entry has already been completed |
| Item not found in documents | The scanned item is not in the source document |
| Quantity exceeds available | Scanned quantity is more than the open quantity |
| Barcode not found | The scanned barcode is not registered |

## Related Pages

- [Goods Receipt Supervisor](/docs/goods-receipt/supervisor)
- [Reports](/docs/goods-receipt/reports)
