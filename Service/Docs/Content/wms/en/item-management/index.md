---
title: Item Management
description: Overview of item management operations.
---

The Item Management module provides tools to query and verify inventory data in real time. It includes three check operations: Item Check, Bin Check, and Package Check.

## Operations

| Operation | Description | Path |
|-----------|-------------|------|
| [Item Check](/docs/item-management/item-check) | Query stock information by item code or barcode | `/itemCheck` |
| [Bin Check](/docs/item-management/bin-check) | Query all items in a specific bin location | `/binCheck` |
| [Package Check](/docs/item-management/package-check) | Query package contents and location | `/packageCheck` |

## Authorization

Access to Item Management requires one of the following roles:

- Goods Receipt Supervisor
- Picking Supervisor
- Counting Supervisor
- Transfer Supervisor
- Package Management
- Package Management Supervisor

## Common Features

All check operations support:

- **Barcode scanning** — scan barcodes to auto-fill input fields.
- **Real-time stock data** — view current quantities across bins.
- **Unit switching** — display quantities in units, dozens, or boxes.
- **Metadata display** — view and edit custom metadata fields (if configured).

## Related Pages

- [Item Check](/docs/item-management/item-check)
- [Bin Check](/docs/item-management/bin-check)
- [Package Check](/docs/item-management/package-check)
