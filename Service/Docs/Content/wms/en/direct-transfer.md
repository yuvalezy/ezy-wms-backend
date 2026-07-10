---
title: Direct Transfer
description: Quick single-item transfers between bin locations.
---

The Direct Transfer module allows operators to quickly transfer a single item from one bin location to another without creating a formal transfer entry.

## Accessing the Page

Route: `/directTransfer`

Navigation: Dashboard → Direct Transfer

## How to Use

1. Scan the **source bin location**.
2. Scan the **item barcode** to transfer.
3. Enter the **quantity**.
4. Select the **unit type**.
5. Scan the **target bin location**.
6. Click **Accept** to execute the transfer.

## Transfer All Stock

You can transfer all available stock of an item from the source bin to the target bin by selecting the **Transfer All Stock** option.

## Confirmation

After scanning the target bin, the system prompts for confirmation before executing the transfer.

## Success

Once confirmed:

- The transfer is executed immediately.
- Inventory is updated in both SAP and WMS.
- A success message confirms the operation.

## Error Messages

| Message | Cause |
|---------|-------|
| Source bin not found | The scanned source bin does not exist |
| Target bin not found | The scanned target bin does not exist |
| Item not found | The scanned item is not in the source bin |
| Quantity more than available | The quantity exceeds available stock |
| Direct transfer executing | The system is processing the transfer |

## Related Pages

- [Transfer](/docs/transfer/index)
- [Transfer Process](/docs/transfer/process)
