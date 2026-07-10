---
title: Transfer Request
description: Create requests for inventory transfers.
---

The Transfer Request page allows users to create formal requests for inventory transfers between locations or warehouses.

## Accessing the Page

Route: `/transferRequest`

Navigation: Dashboard → Transfer Request

## Creating a Transfer Request

1. Click **Create Request**.
2. Fill in the request details:
   - **Source Warehouse** — where the inventory is currently located.
   - **Target Warehouse** — where the inventory should be moved.
   - **Items** — add the items to transfer:
     - Scan or enter the item code.
     - Enter the quantity.
     - Select the unit.
3. Click **Request Approval** to submit.

## Request Status

| Status | Description |
|--------|-------------|
| Pending Approval | Waiting for supervisor approval |
| Approved | Request has been authorized |
| Rejected | Request has been denied |
| Cancelled | Request was cancelled by the requester |

## Messages

| Message | Meaning |
|---------|---------|
| Transfer request created | The request was successfully submitted |
| Transfer submitted for approval | The request is pending authorization |

## Related Pages

- [Transfer Approval](/docs/transfer/approval)
- [Transfer Process](/docs/transfer/process)
