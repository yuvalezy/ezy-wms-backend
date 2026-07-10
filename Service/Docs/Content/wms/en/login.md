---
title: Login
description: How to access EZY WMS and authenticate on a mobile device.
---

The login page is the entry point to EZY WMS. Open your device's browser and navigate to the WMS URL provided by your administrator. The page loads the company branding and a single **Code** field for authentication.

![EZY WMS login screen](/docs/wms/login/login-empty.png)

## Login Fields

| Field | Description |
|-------|-------------|
| **Code** | Your personal access code (assigned by your administrator) |
| **Language** | Interface language — English or Español |
| **Warehouse** | Shown only when your account has access to multiple warehouses |
| **Device Name** | Shown only on first use from a new device |

## Logging In

1. Enter your **Code**.
2. Select your preferred **Language**.
3. If prompted, select your **Warehouse**.
4. If this is a new device, enter a **Device Name** (see below).
5. Tap **Enter**.

![Login form with code entered](/docs/wms/login/login-filled.png)

## Language Selection

Tap the **Language** dropdown to switch between English and Español before logging in. The interface language updates immediately.

![Language selector open showing English and Español](/docs/wms/login/login-language.png)

## First-Time Device Setup

EZY WMS uses per-device licensing. The first time you log in from a new phone, tablet, or browser, the system detects an unregistered device and asks you to name it before granting access.

![New device prompt with Device Name field](/docs/wms/login/login-device-name.png)

1. Enter a descriptive name (e.g. `Warehouse A — Scanner 3`).
2. Tap **Enter**.

The device is registered and your session starts. An administrator can manage registered devices under **Settings → Devices**.

## Session Management

- Sessions expire automatically after a period of inactivity (configurable by your administrator).
- When a session expires you are redirected back to the login page.
- To end your session manually, use the logout option from the sidebar menu.

## Connection Errors

If the device cannot reach the WMS server, a connection error overlay appears with a **Retry** button. Verify that your device is on the correct network and tap **Retry**.

## Account Status Warnings

Depending on the license state, a status banner may appear below the login form:

| Banner | Meaning |
|--------|---------|
| Payment Due | Subscription payment is overdue |
| Demo | Running on a time-limited demo license |
| Demo Expired | Demo period has ended — contact support |
| Disabled | Account has been disabled by an administrator |
