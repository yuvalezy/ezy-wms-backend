---
title: Counting Supervisor
description: Create, monitor, and close inventory counts.
---

The Counting Supervisor page is used to create new inventory counts, monitor their progress, review reports, and close or cancel them.

## Accessing the Page

Route: `/countingSupervisor`

Navigation: Dashboard → Counting Supervisor

## Creating a New Count

1. Go to the Counting Supervisor page.
2. In the **Identification** field, enter an internal name (e.g., "Count Aisle 3").
3. Click **Create**.
4. The count is now available for operators to work.

## Count List

The supervisor view shows all counts with:

| Column | Description |
|--------|-------------|
| Number | Count ID |
| Identification | Internal name |
| Status | Open, In Progress, Finished, Cancelled |
| Created By | Employee who created the count |
| Date | Creation date |

## Row Actions

Click the three-dots menu to access:

| Action | Description |
|--------|-------------|
| Summary Report | View all scanned locations and items |
| Finish | Close the count and send data to SAP |
| Cancel | Delete the count and all scanned data |

## Finishing a Count

1. Review the **Summary Report** to verify completeness.
2. Click **Finish** and confirm.
3. The count is closed.

⚠️ **Important:** Finishing the count in WMS does not yet update SAP inventory. An additional step in SAP is required. See [Summary Report](/docs/counting/summary-report).

## Cancelling a Count

To cancel a count:

1. Click the three-dots menu.
2. Select **Cancel**.
3. Confirm the action.

⚠️ **Warning:** Cancellation cannot be undone. All scanned data is lost.

## Filtering

Use filters to find specific counts by status or date range.

## Related Pages

- [Counting Process](/docs/counting/process)
- [Summary Report](/docs/counting/summary-report)
