---
title: Goods Receipt Reports
description: Review and analyze goods receipt data.
---

The Goods Receipt module provides several reports to help supervisors validate and close entries accurately.

## Report Types

### Goods Receipt Report

Route: `/goodsReceiptReport`

Shows all scanned items for a selected entry with:

| Column | Description |
|--------|-------------|
| Line Number | Document line |
| Item Code | Product code |
| Item Name | Product description |
| Document Quantity | Expected quantity from SAP |
| Scanned Quantity | Quantity received in WMS |
| Unit | Unit of measure |
| Bin | Destination bin |
| Comment | Operator comments |

### vs Exit Report

Route: `/goodsReceiptVSExitReport/:scanCode`

Compares received quantities against exit documents to detect anomalies.

### Difference Report

Route: `/goodsReceiptProcessDifferenceReport/:scanCode`

Highlights discrepancies between the source document and scanned quantities:

| Status | Meaning |
|--------|---------|
| Complete | Quantity matches exactly |
| Less than ordered | Received less than expected |
| More than ordered | Received more than expected |
| Not Received | Item was not scanned |

### All Items Report

Route: `/goodsReceiptReportAll/:scanCode`

Provides comprehensive detail of all items in the entry, including lines that were not scanned.

## Filtering Reports

All reports support:

- **Date range** — filter by From/To dates.
- **Show only differences** — display only lines with discrepancies.
- **Document type** — filter by source document type.

## Exporting

Reports can be exported to Excel for external analysis.

## Related Pages

- [Goods Receipt Supervisor](/docs/goods-receipt/supervisor)
- [Goods Receipt Process](/docs/goods-receipt/process)
