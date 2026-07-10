---
title: Picking Supervisor
description: Monitor and manage picking operations.
---

The Picking Supervisor page provides an overview of all pickings and tools to monitor progress, run checks, and manage assignments.

## Accessing the Page

Route: `/pickSupervisor`

Navigation: Dashboard → Picking Supervisor

## Picking List

The supervisor view shows all pickings with:

| Column | Description |
|--------|-------------|
| Number | Picking ID |
| Type | Document type (Sales Order, Transfer, etc.) |
| Document Number | Source SAP document |
| Customer / Vendor | Business partner name |
| Status | Open, In Progress, Finished, Cancelled |
| Total Items | Total lines in the picking |
| Open Items | Remaining lines to pick |
| Progress | Percentage complete |

## Row Actions

Click the three-dots menu to access:

| Action | Description |
|--------|-------------|
| View Check | View or start a picking check |
| Restart Check | Reset the check status |
| Details | View full picking details |

## Search and Filter

- **Search** — find pickings by number, order, invoice, transfer, or comments.
- **Status filter** — show only pickings with a specific status.

## Check Management

Supervisors can initiate picking checks to ensure accuracy:

1. Find the picking in the list.
2. Click **Start Check**.
3. Follow the [Picking Check](/docs/picking/picking-check) process.

## Error Messages

| Message | Cause |
|---------|-------|
| No picking data | There are no pending pickings |
| No search results | The search term did not match any picking |

## Related Pages

- [Picking Process](/docs/picking/process)
- [Picking Check](/docs/picking/picking-check)
