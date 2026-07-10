---
title: Picking SAP Integration
description: How pickings are generated and synchronized with SAP.
---

EZY WMS integrates directly with SAP Business One for picking operations. Pickings are generated in SAP and sync automatically to the WMS.

## Generating a Picking from a Sales Order

1. In SAP, open the sales order.
2. Copy the sales order number.
3. Navigate to **Inventory → Pick and Pack → Picking Manager**.
4. Paste the sales order number in both the **From** and **To** fields.
5. Click **OK**.
6. Double-click the boxes to select all lines.
7. Verify sufficient stock is available for each item.
8. Copy the delivery date from the original sales order.
9. Click **Generate**.
10. Continue through the wizard, pasting the delivery date when prompted.
11. Click **Generate** to finalize.

The picking now appears automatically in the WMS as pending.

## Real-time Synchronization

As items are picked in the WMS:

- SAP is updated in real time.
- The Traffic Department sees current picking progress.
- Completed pickings disappear from WMS and are marked complete in SAP.

## Manual Delivery

After the warehouse completes picking:

1. The Traffic Department returns to SAP.
2. From the sales order or picking list, perform a **manual delivery**.
3. This registers the inventory exit.

⚠️ **Note:** This step is mandatory for inventory exit but does not affect invoicing or accounting.

## Related Pages

- [Picking Process](/docs/picking/process)
- [Picking Supervisor](/docs/picking/supervisor)
