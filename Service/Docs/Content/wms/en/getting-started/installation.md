---
title: EZY WMS Installation
description: Installing and configuring EZY WMS.
---

Follow these steps to install and configure EZY WMS in your environment.

## Prerequisites

- Compatible web browser (Chrome, Edge, Safari)
- Network access to the WMS server
- SAP Business One integration (if applicable)
- Barcode scanners or mobile devices (PDTs) for warehouse operations

## Setup Steps

1. **Access the WMS** from your browser using the URL provided by your administrator.
2. **Log in** with your super-administrator credentials.
3. **Register devices** — each device (PDT, computer, tablet) must be registered once. See [Device Registration](/docs/getting-started/device-registration).
4. **Create users** and assign authorization groups. See [User Setup](/docs/getting-started/user-setup).
5. **Configure warehouses** in the system settings.
6. **Verify SAP connectivity** (if integrated).

## First Login

On first access from any device:

1. Enter the WMS URL in your browser.
2. Log in with your user credentials.
3. If prompted, enter a device name (e.g., "PDT Aisle 1").
4. Click **Enter** to access the system.

## Mobile Device Setup

For PDTs and handheld scanners:

- Ensure the device has Wi-Fi connectivity.
- Open the browser and navigate to the WMS URL.
- The interface is optimized for touch and barcode scanning.
- Barcode scanning automatically fills input fields.

## Troubleshooting

| Issue | Solution |
|-------|----------|
| Cannot connect | Check network and server URL |
| Device not recognized | Register the device first |
| Session expired | Log in again; sessions expire after inactivity |
| Offline mode | The system queues actions and syncs when online |
