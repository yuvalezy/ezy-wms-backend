---
title: Counting Summary Report
description: Review and finalize inventory count data.
---

The Counting Summary Report provides a comprehensive view of all locations and items scanned during a count, allowing supervisors to validate data before finishing.

## Accessing the Page

Route: `/countingSummaryReport/:scanCode`

Navigation: Counting Supervisor → Three-dots menu → Summary Report

## Report Contents

The summary report displays:

| Column | Description |
|--------|-------------|
| Bin Location | Scanned bin code |
| Item Code | Product code |
| Item Name | Product description |
| Counted Quantity | Physically counted amount |
| Unit | Unit of measure |
| Document Quantity | Expected quantity from SAP |
| Difference | Variance |

## Show Only Differences

Enable the **Show only differences** filter to display only lines where the counted quantity differs from the document quantity.

## Finishing in SAP

After finishing the count in WMS, you must post it in SAP:

1. In SAP, navigate to **Inventory → Inventory Operations → Inventory Counting**.
2. Click the green back icon to view recent counts.
3. Verify the count status is **Open**.
4. Review details and adjust quantities if necessary.
5. Click **Copy to Inventory Posting**.
6. Accept and approve the process.

Once approved, the system officially updates inventory in both SAP and WMS.

## Open Counts Check

Before starting new counts, supervisors should review open counts in SAP:

1. Filter by status "Open" in the SAP counting module.
2. Finish or cancel any pending counts.
3. This prevents confusion, inventory errors, or duplicates.

## Related Pages

- [Counting Supervisor](/docs/counting/supervisor)
- [Counting Process](/docs/counting/process)
