---
title: Picking
description: Overview of the picking module.
---

The Picking module is used to prepare merchandise for dispatch. Operators collect products from their bin locations according to sales orders, transfer requests, or other outgoing documents.

## Workflow

1. The **Traffic Department** generates pickings in SAP (from sales orders or directly).
2. Pickings sync automatically to the WMS.
3. **Operators** open a picking and scan items from suggested bin locations.
4. As items are picked, the picking updates in real time with SAP.
5. When 100% complete, the picking disappears from WMS and is reflected in SAP.

## Pages

| Page | Route | Description |
|------|-------|-------------|
| [Picking List](/docs/picking/process) | `/pick` | List of pending pickings |
| [Picking Process](/docs/picking/process) | `/pick/:idParam` | Scan and pick items |
| [Picking Detail](/docs/picking/detail) | `/pick/:idParam/:typeParam/:entryParam` | Pick specific line detail |
| [Picking Check](/docs/picking/picking-check) | `/pick/:id/check` | Verify picked items |
| [Supervisor](/docs/picking/supervisor) | `/pickSupervisor` | Monitor and manage pickings |

## Document Types

Pickings can be generated from:

- Sales Orders
- Transfer Requests
- Reserved Invoices
- Invoices

## Roles

| Role | Permissions |
|------|-------------|
| Picking | Scan and pick items |
| Picking Supervisor | Monitor pickings, manage assignments, run checks |

## Related Pages

- [Picking SAP Integration](/docs/picking/sap-integration)
- [Transfer](/docs/transfer/index)
