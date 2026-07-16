---
title: Transfer
description: Overview of the inventory transfer module.
---

The Transfer module is used to move merchandise from one bin location to another within the warehouse. It is a key tool for reorganizing inventory and optimizing warehouse distribution.

## Workflow

1. **Supervisor** creates a new transfer entry with an internal name and optional comment.
2. **Operators** access the transfer and scan source bin locations and items.
3. Operators select destination bins and scan items again to confirm placement.
4. Multiple source and destination bins can be used within the same transfer.
5. **Supervisor** finishes the transfer to update inventory in SAP and WMS.

## Pages

| Page | Route | Description |
|------|-------|-------------|
| [Transfer List](/docs/transfer/process) | `/transfer` | List of pending transfers |
| [Transfer Process](/docs/transfer/process) | `/transfer/:scanCode/*` | Execute the transfer |
| [Supervisor](/docs/transfer/supervisor) | `/transferSupervisor` | Create and monitor transfers |
| [Transfer Report](/docs/transfer/summary-report) | `/transferSummaryReport/:id` | Review what a transfer moved |
| [Approval](/docs/transfer/approval) | `/transfer/approve` | Approve cross-warehouse transfers |
| [Transfer Request](/docs/transfer/transfer-request) | `/transferRequest` | Create transfer requests |

## Transfer Process Steps

The transfer process has three sub-steps:

1. **Source** — scan the pickup bin and items to move.
2. **Target Bins** — select destination bins.
3. **Target Items** — scan items again at the destination to confirm.

## Cross-Warehouse Transfers

Transfers between different warehouses require supervisor approval:

1. Select the **target warehouse**.
2. The system marks the transfer as "Waiting for Approval".
3. A supervisor with approval rights must approve or reject the transfer.

## Roles

| Role | Permissions |
|------|-------------|
| Transfer | Execute transfers |
| Transfer Supervisor | Create transfers, finish, view reports |
| Transfer Request | Create transfer requests |
| Transfer Approval | Approve or reject cross-warehouse transfers |

## Related Pages

- [Transfer Process](/docs/transfer/process)
- [Transfer Supervisor](/docs/transfer/supervisor)
- [Transfer Report](/docs/transfer/summary-report)
