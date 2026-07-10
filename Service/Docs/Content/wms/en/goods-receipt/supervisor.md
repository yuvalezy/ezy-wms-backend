---
title: Goods Receipt Supervisor
description: Create, monitor, and close goods receipt entries.
---

The Goods Receipt Supervisor page is used to create new receipt entries, monitor progress, review reports, and close or cancel entries.

## Accessing the Page

Route: `/goodsReceiptSupervisor`

Navigation: Dashboard → Goods Receipt Supervisor

## Creating a New Entry

1. Click **Create** or the add button.
2. Fill in the entry details:
   - **Identification** — free-text name for internal use (e.g., "Receipt April 23").
   - **Document Type** — select the source document type:
     - Purchase Order
     - Purchase Invoice
     - Goods Receipt PO
   - **Document Number** — enter the SAP document number.
3. Click **Add** to link the document.
4. Repeat steps 2–3 to add multiple documents to the same entry.
5. Click **Create** to generate the entry.

## Entry List

The supervisor view shows all entries with:

| Column | Description |
|--------|-------------|
| Number | Entry ID |
| Identification | Internal name |
| Document Type | Source document type |
| Status | Open, In Progress, Finished, Cancelled |
| Created By | Employee who created the entry |
| Date | Creation date |

## Row Actions

Click the three-dots menu next to any entry to access:

| Action | Description |
|--------|-------------|
| Report | View scanned quantities and details |
| vs Exit Report | Compare receipt quantities against exit documents |
| Difference Report | View discrepancies between scanned and document quantities |
| All Items Report | Full detail of all items |
| Finish | Close the entry and finalize |
| Cancel | Delete the entry and all scanned data |

## Finishing an Entry

Before finishing:

1. Review the **Report** to ensure all items are scanned correctly.
2. Check the **Difference Report** for any discrepancies.
3. Click **Finish** and confirm.
4. The entry is closed and data is sent to SAP.

## Cancelling an Entry

To cancel an entry:

1. Click the three-dots menu.
2. Select **Cancel**.
3. Confirm the action.

⚠️ **Warning:** Cancellation cannot be undone. All scanned data is lost.

## Filtering

Use filters to find specific entries:

| Filter | Options |
|--------|---------|
| Status | All, Open, In Progress, Finished, Cancelled |
| Date Range | From / To date |
| Document Type | Purchase Order, Purchase Invoice, etc. |

## Related Pages

- [Goods Receipt Process](/docs/goods-receipt/process)
- [Reports](/docs/goods-receipt/reports)
