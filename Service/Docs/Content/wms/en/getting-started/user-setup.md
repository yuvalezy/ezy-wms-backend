---
title: User Setup
description: Create and configure WMS users.
---

Once devices are registered, you must create the users who will operate the system.

## Creating a New User

1. Log in with the super-administrator account.
2. Go to **Settings → Users**.
3. Click **Create New User**.
4. Enter the user details:
   - **Full Name** — required, 2–100 characters.
   - **Email** — optional, must be valid format.
   - **Position** — optional job title.
   - **Password** — required, minimum 6 characters.
   - **Authorization Group** — assigns role permissions.
   - **Warehouses** — select at least one warehouse.
   - **Super User** — grants access to all system functions.
5. Click **Save**.

## Authorization Groups

Users are assigned to authorization groups that define their permissions:

| Role | Access |
|------|--------|
| Goods Receipt | Process incoming goods |
| Goods Receipt Supervisor | Validate, report, and close receipts |
| Goods Receipt Confirmation | Confirm received goods |
| Picking | Pick items for orders |
| Picking Supervisor | Supervise picking operations |
| Counting | Perform physical inventory counts |
| Counting Supervisor | Supervise counting and resolve discrepancies |
| Transfer | Move inventory between locations |
| Transfer Supervisor | Supervise transfers and approve movements |
| Transfer Confirmation | Confirm completed transfers |
| Direct Transfer | Quick single-item transfers |
| Item Management | Manage item data and barcodes |
| Package Management | Manage packages |

## Importing from External User

You can also create users by importing from an external user directory:

1. In the user form, select **Import from External User**.
2. Search and select the external user.
3. The system prefills name and email.
4. Assign authorization group and warehouses.
5. Save.

## Disabling Users

To disable a user:

1. Go to **Settings → Users**.
2. Find the user and click **Disable**.
3. Confirm the action.

Disabled users cannot log in but their history is preserved.

## Related Pages

- [Device Registration](/docs/getting-started/device-registration)
- [Settings - Users](/docs/settings/users)
- [Settings - Authorization Groups](/docs/settings/authorization-groups)
