---
title: Transfer Approval
description: Approve or reject cross-warehouse transfers.
---

The Transfer Approval page is used by supervisors to review and approve or reject cross-warehouse transfers that require authorization.

## Accessing the Page

Route: `/transfer/approve`

Navigation: Dashboard → Transfer Approval

## Approval List

The page displays all transfers awaiting approval with:

| Column | Description |
|--------|-------------|
| Number | Transfer ID |
| Source Warehouse | Origin warehouse |
| Target Warehouse | Destination warehouse |
| Requested By | Employee who created the transfer |
| Requested At | Date and time of request |
| Status | Pending Approval |

## Actions

For each transfer, supervisors can:

| Action | Description |
|--------|-------------|
| **Approve** | Authorize the transfer to proceed |
| **Reject** | Deny the transfer |

### Approving a Transfer

1. Click **Approve** next to the transfer.
2. Confirm the action.
3. The transfer is approved and the operator can proceed.

### Rejecting a Transfer

1. Click **Reject** next to the transfer.
2. Enter a **rejection reason** (required).
3. Confirm the action.
4. The transfer is rejected and cannot proceed.

## Approval Detail

Route: `/transfer/approve/:id`

Click a transfer number to view full details before deciding:

- Source and target warehouses
- Items being transferred
- Quantities and units
- Requester information

## Messages

| Message | Meaning |
|---------|---------|
| Transfer approved successfully | The transfer has been authorized |
| Transfer rejected successfully | The transfer has been denied |
| No transfers awaiting approval | There are no pending approvals |
| This cross-warehouse transfer requires supervisor approval | The transfer was submitted for approval |

## Related Pages

- [Transfer Process](/docs/transfer/process)
- [Transfer Supervisor](/docs/transfer/supervisor)
