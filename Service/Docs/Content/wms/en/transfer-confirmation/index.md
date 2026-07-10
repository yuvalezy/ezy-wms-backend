---
title: Transfer Confirmation
description: Overview of the transfer confirmation module.
---

The Transfer Confirmation module is used to confirm and validate completed inventory transfers, particularly for cross-warehouse movements. It reuses the same components as Goods Receipt Confirmation but with a transfer focus.

## Workflow

1. **Supervisor** creates a transfer confirmation entry.
2. **Operators** scan and confirm transferred items at the destination warehouse.
3. **Supervisor** reviews reports and closes the confirmation.

## Pages

| Page | Route | Description |
|------|-------|-------------|
| Confirmation List | `/transferConfirmation` | List of open confirmations |
| Confirmation Process | `/transferConfirmation/:scanCode` | Scan and confirm items |
| Confirmation Supervisor | `/transferConfirmationSupervisor` | Create and monitor confirmations |
| Confirmation Report | `/transferConfirmationReport` | View confirmed quantities |
| Difference Report | `/transferConfirmationProcessDifferenceReport/:scanCode` | View discrepancies |
| All Items Report | `/transferConfirmationReportAll/:scanCode` | Full detail report |

## Roles

| Role | Permissions |
|------|-------------|
| Transfer Confirmation | Scan and confirm transferred items |
| Transfer Confirmation Supervisor | Create entries, view reports, close entries |

## Related Pages

- [Transfer](/docs/transfer/index)
- [Receipt Confirmation](/docs/receipt-confirmation/index)
