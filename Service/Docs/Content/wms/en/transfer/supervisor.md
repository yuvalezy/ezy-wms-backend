---
title: Transfer Supervisor
description: Create, monitor, and finish inventory transfers.
---

The Transfer Supervisor page is used to create new transfer entries, monitor their progress, and finish or cancel them.

## Accessing the Page

Route: `/transferSupervisor`

Navigation: Dashboard → Transfer Supervisor

## Creating a New Transfer

1. Click **Create**.
2. Fill in the transfer details:
   - **Identification** — internal name (e.g., "Reorganization Zone A").
   - **Comment** — optional additional information.
   - **Target Warehouse** — for cross-warehouse transfers only.
3. Click **Create**.
4. The transfer is now available for operators.

## Transfer List

The supervisor view shows all transfers with:

| Column | Description |
|--------|-------------|
| Number | Transfer ID |
| Identification | Internal name |
| Status | Open, In Progress, Finished, Cancelled, Waiting for Approval |
| Created By | Employee who created the transfer |
| Date | Creation date |

## Row Actions

Click the three-dots menu to access:

| Action | Description |
|--------|-------------|
| Transfer Report | Review what the transfer moved, item by item |
| Finish | Close the transfer and update inventory — only once the transfer is 100% complete |
| Cancel | Delete the transfer and all scanned data |

To open a transfer for scanning, click its **Number**.

## Finishing a Transfer

1. Verify all items have been moved.
2. Click **Finish** and confirm.
3. The system confirms merchandise has been moved and updates locations in SAP and WMS.

## Cancelling a Transfer

1. Click the three-dots menu.
2. Select **Cancel**.
3. Confirm the action.

⚠️ **Warning:** Cancellation cannot be undone.

## Filtering

The list shows **Open**, **In Progress**, and **Waiting for Approval** transfers by default.

Click the funnel icon in the page header to filter by status. Tick **Finished**, **Rejected**, or **Cancelled** to bring closed transfers into the list — this is how you reach the [Transfer Report](/docs/transfer/summary-report) for a transfer that has already been finished. **Clear** returns the list to its default statuses.

## Related Pages

- [Transfer Report](/docs/transfer/summary-report)
- [Transfer Process](/docs/transfer/process)
- [Transfer Approval](/docs/transfer/approval)
