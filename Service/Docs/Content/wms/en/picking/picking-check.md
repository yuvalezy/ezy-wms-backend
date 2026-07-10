---
title: Picking Check
description: Verify and validate picked items for accuracy.
---

The Picking Check page allows supervisors or designated checkers to verify that picked items match the order requirements before dispatch.

## Accessing the Page

Route: `/pick/:id/check`

Navigation: Picking Supervisor → Select Picking → Start Check

## How to Use

1. From the Picking Supervisor page, find the picking to check.
2. Click **Start Check**.
3. The check page displays all items in the picking.
4. For each item, verify:
   - Item code matches the order.
   - Quantity is correct.
   - Bin location is correct.
   - Package assignments are correct.
5. Mark items as checked or note discrepancies.

## Check Status

| Status | Description |
|--------|-------------|
| Not Checked | Item has not been verified yet |
| Checked | Item has been verified |
| Discrepancy | Item has a quantity or product mismatch |

## Completing the Check

1. Review all items.
2. If discrepancies are found, the system shows a warning with the count.
3. Click **Complete Check** to finish.
4. The system records the check results and updates the picking status.

## Restarting a Check

If needed, you can restart a check:

1. Click **Restart Check**.
2. Confirm the action.
3. All check statuses are reset.

## Error Messages

| Message | Cause |
|---------|-------|
| Check started | The check has already been initiated |
| Picking check complete | The check was successfully completed |
| Discrepancies found | There are quantity or product mismatches |

## Related Pages

- [Picking Process](/docs/picking/process)
- [Picking Supervisor](/docs/picking/supervisor)
