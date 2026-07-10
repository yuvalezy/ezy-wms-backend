---
title: Transfer Process
description: Execute inventory transfers between bin locations.
---

The Transfer Process page guides operators through moving merchandise from source bins to destination bins.

## Accessing the Page

Route: `/transfer/:scanCode/*`

Navigation: Dashboard → Transfer → Select Transfer Number

## How to Use

### Step 1: Scan Source Bin

1. Select the transfer number from the list.
2. Scan the **source bin location** barcode.
3. This is the bin you are moving merchandise from.

### Step 2: Scan Source Items

1. Scan the **item barcode** for each product being removed.
2. You can scan multiple items as long as they come from the same scanned source bin.
3. Confirm or modify the **quantity**.
4. Select the **unit type**.
5. Click **Accept**.

### Step 3: Select Destination Bins

1. Once finished collecting from the source bin, click **Choose Destination Bins**.
2. Scan or select the **destination bin location**.
3. This is where the merchandise will be placed.

### Step 4: Scan Items at Destination

1. Scan the **items again** that are being placed in the destination bin.
2. This step verifies that all removed merchandise is being correctly placed.
3. Confirm on screen that the scanned items match what was removed.

### Continuing the Transfer

To continue with a new source or destination bin:

- Click **Transfer** again.
- Scan a new source bin, or
- Select a new destination bin as needed.

You can move merchandise from one source bin to multiple destinations, or from multiple sources to one destination.

## Cross-Warehouse Transfer

If the transfer is between warehouses:

1. Select the **target warehouse**.
2. The system validates that it is different from the source warehouse.
3. Complete the transfer normally.
4. The transfer is submitted for approval.

## Error Messages

| Message | Cause |
|---------|-------|
| Transfer not valid | The transfer number does not exist |
| Transfer is closed | The transfer has already been completed |
| Item not found in source | The item is not in the source documents |
| Quantity more than available | The quantity exceeds available stock |
| Package already added as source | The package is already part of this transfer |

## Related Pages

- [Transfer Supervisor](/docs/transfer/supervisor)
- [Transfer Approval](/docs/transfer/approval)
