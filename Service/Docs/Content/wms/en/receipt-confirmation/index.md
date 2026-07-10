---
title: Receipt Confirmation
description: Overview of the receipt confirmation module.
---

The Receipt Confirmation module is used to confirm and validate goods that have been received. It follows the same workflow as Goods Receipt but with a confirmation focus.

## Workflow

1. **Supervisor** creates a confirmation entry.
2. **Operators** scan and confirm received items.
3. **Supervisor** reviews confirmation reports and closes the entry.

## Pages

| Page | Route | Description |
|------|-------|-------------|
| Confirmation List | `/goodsReceiptConfirmation` | List of open confirmations |
| Confirmation Process | `/goodsReceiptConfirmation/:scanCode` | Scan and confirm items |
| Confirmation Supervisor | `/goodsReceiptConfirmationSupervisor` | Create and monitor confirmations |
| Confirmation Report | `/goodsReceiptConfirmationReport` | View confirmed quantities |
| vs Exit Report | `/goodsReceiptConfirmationVSExitReport/:scanCode` | Compare confirmation vs exit |
| Difference Report | `/goodsReceiptConfirmationProcessDifferenceReport/:scanCode` | View discrepancies |
| All Items Report | `/goodsReceiptConfirmationReportAll/:scanCode` | Full detail report |

## Roles

| Role | Permissions |
|------|-------------|
| Goods Receipt Confirmation | Scan and confirm items |
| Goods Receipt Confirmation Supervisor | Create entries, view reports, close entries |

## Related Pages

- [Goods Receipt](/docs/goods-receipt/index)
- [Transfer Confirmation](/docs/transfer-confirmation/index)
