---
title: Transfer Report
description: Review what a transfer moved, item by item, with source and target bins.
---

The Transfer Report is the supervisor's view of what a transfer actually moved. It shows one row per item: what was taken from the source bins, where it was placed in the target bins, and what is still pending between them.

Use it to review a transfer at any point — including after it has been finished, when the scanning screen no longer shows anything.

## Accessing the Page

Route: `/transferSummaryReport/:id`

Navigation: Dashboard → Transfer Supervisor → three-dots menu on a transfer → **Transfer Report**

On mobile, the **Transfer Report** button appears at the bottom of each transfer card.

Requires the **Transfer Supervisor** role.

## Report Header

| Field | Description |
|-------|-------------|
| Identification | Internal name, if the transfer has one |
| Date | Creation date |
| Status | Open, In Progress, Waiting for Approval, Finished, Rejected, Cancelled |
| Source Warehouse | Warehouse the stock left |
| Target Warehouse | Destination warehouse, for cross-warehouse transfers |
| Created By | Employee who created the transfer |
| SAP Doc # | The SAP stock transfer document, once posted |
| Comment | The transfer's comment, if any |

## Item Rows

| Column | Description |
|--------|-------------|
| Code | Item code — click to open item details |
| Description | Item name |
| Source Bins | Bins the item was taken from, each with its quantity |
| Target Bins | Bins the item was placed into, each with its quantity |
| Quantity | Total taken from the source |
| Placed | Total placed into the target |
| Pending | Quantity still to be placed |

**Pending** is highlighted whenever it is not zero. On a transfer still in progress this is normal — it is what remains to be put away. On a finished transfer it is worth investigating.

A negative **Pending** means more was placed than was taken, and indicates the two sides disagree.

Quantities follow your unit settings for the Transfer module, so they read as boxes, dozens, and units the same way as the scanning screens.

## Exporting to Excel

Click the green spreadsheet icon in the page header to download the report as an `.xlsx` file, named `transfer_data_<number>.xlsx`. The export contains the item rows — code, description, source bins, target bins, and quantity — with quantity columns matching your unit settings.

## Finding Finished Transfers

The Transfer Supervisor list shows open, in-progress, and waiting-for-approval transfers by default. To report on a transfer that has already been finished, open the filter (funnel icon) and tick **Finished**.

## Notes

- **SAP Doc #** is recorded when a transfer is posted to SAP. Transfers posted before this feature was added show a dash.
- Cancelled lines are excluded from the report.
- The report restates quantities from the current SAP conversion factors before totalling, so it always agrees with the scanning screens.

## Related Pages

- [Transfer Supervisor](/docs/transfer/supervisor)
- [Transfer Process](/docs/transfer/process)
