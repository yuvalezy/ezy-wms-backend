---
title: Receipt Confirmation Process
description: Scan and confirm received merchandise.
---

The Receipt Confirmation Process page works identically to the Goods Receipt Process, but is used to validate already-received merchandise.

## Accessing the Page

Route: `/goodsReceiptConfirmation/:scanCode`

Navigation: Dashboard → Receipt Confirmation → Select Entry

## How to Use

1. From the Confirmation list, click the entry number.
2. Scan the **barcode** of each item to confirm.
3. Confirm or modify the **quantity**.
4. Select the **unit type**.
5. Click **Accept**.

## Features

All features from Goods Receipt Process apply:

- Package management (scan package mode, scan item mode)
- Destination options (warehouse, delivery, showroom, supervisor)
- Comments on lines
- Quantity editing

## Error Messages

| Message | Cause |
|---------|-------|
| Entry not valid | The confirmation number does not exist |
| Entry is closed | The confirmation has already been completed |
| Item not found in documents | The item is not in the source document |

## Related Pages

- [Receipt Confirmation Supervisor](/docs/receipt-confirmation/supervisor)
- [Goods Receipt Process](/docs/goods-receipt/process)
