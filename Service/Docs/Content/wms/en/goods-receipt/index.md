---
title: Goods Receipt
description: Overview of the goods receipt module.
---

The Goods Receipt module is used to receive and process incoming merchandise. It supports receiving against purchase orders, purchase invoices, and other document types.

## Workflow

1. **Supervisor** creates a new goods receipt entry and links source documents.
2. **Operators** scan and receive merchandise using the Goods Receipt process page.
3. **Supervisor** reviews reports, adjusts quantities if needed, and closes the entry.

## Pages

| Page | Route | Description |
|------|-------|-------------|
| [Goods Receipt List](/docs/goods-receipt/process) | `/goodsReceipt` | List of open receipts for operators |
| [Goods Receipt Process](/docs/goods-receipt/process) | `/goodsReceipt/:scanCode` | Scan and receive items |
| [Supervisor](/docs/goods-receipt/supervisor) | `/goodsReceiptSupervisor` | Create, monitor, and close receipts |
| [Reports](/docs/goods-receipt/reports) | `/goodsReceiptReport` | View scanned quantities |
| [vs Exit Report](/docs/goods-receipt/reports) | `/goodsReceiptVSExitReport/:scanCode` | Compare receipt vs exit |
| [Difference Report](/docs/goods-receipt/reports) | `/goodsReceiptProcessDifferenceReport/:scanCode` | View discrepancies |
| [All Items Report](/docs/goods-receipt/reports) | `/goodsReceiptReportAll/:scanCode` | Full item detail report |

## Document Types Supported

- Purchase Order
- Purchase Invoice
- Goods Receipt PO
- Inventory Transfer

## Roles

| Role | Permissions |
|------|-------------|
| Goods Receipt | Scan and receive items |
| Goods Receipt Supervisor | Create entries, view reports, close entries |

## Related Pages

- [Receipt Confirmation](/docs/receipt-confirmation/index)
- [Transfer](/docs/transfer/index)
