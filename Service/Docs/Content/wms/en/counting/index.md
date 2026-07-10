---
title: Inventory Counting
description: Overview of the inventory counting module.
---

The Inventory Counting module is used to perform physical inventory counts, whether for the entire warehouse or specific locations such as shelves or racks.

## Workflow

1. **Supervisor** creates a new counting entry with an internal identification name.
2. **Operators** access the counting and scan bin locations and items.
3. Multiple bin locations can be counted within the same counting entry.
4. **Supervisor** reviews the summary report and closes the counting.
5. In SAP, the counting is posted to update inventory.

## Pages

| Page | Route | Description |
|------|-------|-------------|
| [Counting List](/docs/counting/process) | `/counting` | List of open counts |
| [Counting Process](/docs/counting/process) | `/counting/:scanCode` | Scan bins and count items |
| [Supervisor](/docs/counting/supervisor) | `/countingSupervisor` | Create and monitor counts |
| [Summary Report](/docs/counting/summary-report) | `/countingSummaryReport/:scanCode` | Review count summary |

## Roles

| Role | Permissions |
|------|-------------|
| Counting | Scan bins and count items |
| Counting Supervisor | Create counts, view reports, close counts |

## Important Notes

- Items that have been counted cannot be moved without a formal process.
- Use the [Transfer](/docs/transfer/index) module to move products between locations.
- Use the [Picking](/docs/picking/index) module to withdraw merchandise for dispatch.
- Any physical movement not registered can cause inventory discrepancies.

## Related Pages

- [Counting Process](/docs/counting/process)
- [Counting Supervisor](/docs/counting/supervisor)
