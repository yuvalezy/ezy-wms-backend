---
title: Device Registration
description: How to register a new device and activate it for WMS access.
---

EZY WMS uses per-device licensing. Every phone, tablet, or browser that accesses the system must be registered once and activated by a Super User before it can be used for warehouse operations.

## How Registration Works

The first time any user logs in from a new device, the system detects an unrecognised device UUID and prompts for a device name before allowing access.

![New device detected — device name prompt](/docs/wms/login/login-device-name.png)

1. Open the WMS URL in the browser.
2. Enter your **Code** and tap **Enter**.
3. The system shows: *"New device detected. Please provide a name for this device to continue."*
4. Enter a descriptive **Device Name** (e.g. `Warehouse A — Scanner 3`, max 100 characters).
5. Tap **Enter**.

The device is now registered with status **Inactive**.

:::caution
A newly registered device is **Inactive** by default. Regular users are blocked from logging in again until a Super User activates the device. Only the initial registration login goes through — subsequent attempts are rejected until activation.
:::

## Activating the Device

Only a **Super User** can activate devices. After a new device is registered:

1. Log in to WMS with a Super User account — the system redirects automatically to **Settings → Devices**.
2. Find the new device in the list (it will show **Inactive** status).
3. Open the device and click **Activate Device**.
4. Optionally enter a reason for the audit trail.
5. Click **Activate**.

The device status changes to **Active** and the user can now log in normally.

## Device Statuses

| Status | Meaning |
|--------|---------|
| **Active** | Device is fully operational; users can log in |
| **Inactive** | Registered but not yet activated; login is blocked for non-Super Users |
| **Disabled** | Manually disabled by an administrator; login is blocked |

## Restricted Access on Inactive Devices

When a Super User logs in on an Inactive device, the main operational menu (Goods Receipt, Picking, Counting, Transfer, etc.) is hidden. Only **Settings** is accessible, allowing the Super User to activate the device directly from the same session.

A yellow status banner also appears on the login page and inside the app when a device is Inactive or Disabled.

## Managing Devices

All device management is under **Settings → Devices** and requires Super User access:

- View device UUID, registration date, and last active date
- Change device status (Activate / Disable) with a reason
- Rename a device
- View the full audit history of status changes

## Related Pages

- [Login](/docs/overview/login)
- [Settings — Devices](/docs/settings/devices)
