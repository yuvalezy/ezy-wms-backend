# Backend functionality

Last verified against source: 2026-07-10. This is a workflow map, not a substitute
for the generated OpenAPI schema. The controller names below are the source of the
`/api/[controller]` route family unless a custom route is shown.

## Warehouse workflows

| Capability | Backend surface | What it does | Frontend surface |
| --- | --- | --- | --- |
| Goods receipt | `GoodsReceiptController`, `GoodsReceiptReportController` | Creates, lists, edits, cancels, processes, and reports goods receipts; supports transactional and confirmation process types and line/document validation | `src/Pages/GoodsReceipt/`, `src/features/goods-receipt/`; routes `/goodsReceipt*` |
| Picking | `PickingController` | Lists and opens pick lists, adds/updates picking detail, processes/cancels picks, manages package labels, check sessions, and repack sessions | `src/Pages/Picking/`, `src/features/picking/`; routes `/pick*` |
| Inventory counting | `CountingController` | Creates, lists, edits, cancels, processes, retries batches, and serves summary/detail reports | `src/Pages/Counting/`, `src/features/counting/`; routes `/counting*` |
| Warehouse transfer | `TransferController` | Creates and edits transfers, manages source/target content and bins, processes/cancels, supports requests and supervisor approval | `src/Pages/Transfer/`, `src/features/transfer/`; routes `/transfer*` and `/transferRequest` |
| Transfer confirmation | `GoodsReceiptController` with `ProcessType.TransferConfirmation` plus report endpoints | Uses the goods-receipt workflow components for transfer-confirmation processing/reporting; role enforcement is currently inconsistent between list and other endpoints | `GoodsReceipt*` components with `ProcessType.TransferConfirmation`; routes `/transferConfirmation*` |
| Direct transfer | `DirectTransferController` | Handles the direct bin/item movement operation exposed separately from the transfer document workflow | `src/Pages/DirectTransfer/`, `src/features/direct-transfer/`; route `/directTransfer` |
| Item/bin checks | `ItemsController` | Resolves scanned bins/items, checks item/bin content and stock, reads/updates item metadata and barcodes | `src/Pages/items/`, `src/features/items/`; routes `/itemCheck` and `/binCheck` |

The exact request and response DTOs are in `Core/DTOs/<module>/`; the browser data
services in `ezy-wms-frontend/src/features/*/data/` are the consumer-side contract.

## Administration and system operations

| Capability | Backend surface | Frontend surface |
| --- | --- | --- |
| Authentication and company bootstrap | `AuthenticationController` | `src/features/login/`, `/login`, `SystemGate` |
| Users | `UserController` | `src/Pages/settings/users-list.tsx`, `src/features/users/` |
| Authorization groups | `AuthorizationGroupController` | `src/Pages/settings/authorization-groups-list.tsx`, `src/features/authorization-groups/` |
| Devices and audit | `DeviceController`, `GeneralController` | `src/Pages/settings/devices-list.tsx`, `src/features/devices/` |
| Cancellation reasons | `CancellationReasonController` | `src/features/cancellation-reasons/`; reads require authentication, while create/update/delete are super-user-only |
| External system alerts | `ExternalSystemAlertController` | `src/Pages/settings/external-alerts-list.tsx`, `src/features/external-alerts/` |
| In-app alerts | `WmsAlertController`, `NotificationHub` | `NotificationContext`, notification bell, `/hubs/notifications` |
| Runtime configuration | `ConfigurationController`, `SmtpController` | `src/Pages/settings/configuration-list.tsx`, `src/features/configuration/` |
| Licensing/account status | `LicenseController`, authentication license-status endpoint | `src/Pages/settings/license.tsx`, `src/features/license/` |
| Health/readiness | `HealthController`, `SystemController` | `SystemGate`, offline/status UI |

## HTTP route families

The public route families currently include:

```text
/api/authentication
/api/authorizationgroup
/api/cancellationreason
/api/configuration
/api/docs
/api/counting
/api/device
/api/directtransfer
/api/externalSystemAlert
/api/general
/api/goodsreceipt
/api/items
/api/license
/api/picking
/api/smtp
/api/system
/api/transfer
/api/user
/api/wmsalert
/health
/hubs/notifications
```

Controller route casing is normalized by ASP.NET routing configuration, but clients
should use the paths already used by the frontend data services. Some legacy action
names intentionally use casing such as `ItemByBarCode`, `HomeInfo`, `readAll`, and
`updateAll`; changing them is an API compatibility change.

## Workflow state and external effects

Most business workflows have a local WMS document/session lifecycle before they
are processed into SAP. Processing can create or update SAP documents through the
Service Layer and may trigger follow-up work, reports, alerts, approvals, or
background synchronization. The endpoint response is the authoritative result for
that request; the frontend should refresh the returned object/state rather than
assuming a local button click completed the SAP operation.

For picking, optional check, package-label, repack, and post-processing behavior is
configuration-driven. For counting, a process can create batches and expose retry
operations. For transfers, feature flags determine whether requests, cross-warehouse
movement, confirmation, target items, and supervisor approval are available.

## Feature/configuration dependencies

The frontend receives user settings and capabilities through the authenticated user
and general-information responses. The most visible gates are:

- `GoodsReceiptType`: transactional, confirmation, or both;
- `EnableTransferConfirm`, `EnableTransferRequest`, and `EnableWarehouseTransfer`;
- `BinLocation` capability for bin checks/direct transfer;
- supervisor-required settings for goods receipt and transfer creation;
- unit/scanner/display-vendor settings;
- picking check, package-label, path-routing, and repack settings.

The backend remains the authorization and validation boundary. Hiding a menu item or
route in the SPA is a usability feature, not a security control.

## Embedded documentation API

The authenticated `DocsController` serves the WMS guide corpus used by the frontend:

- `GET /api/docs?locale=en|es` returns only articles visible to the current session.
- `GET /api/docs/search?q=...&locale=en|es&limit=20` searches visible article
  summaries; the service clamps the limit to 1–50.
- `GET /api/docs/{module}/{slug}?locale=en|es` returns one visible Markdown article
  or `404` when it is missing or gated.

Visibility is enforced on the backend by super-user status, role, bin-location
capability, and the relevant workflow feature settings. The frontend index is not a
security boundary and must not be treated as a complete list of what every user can
read.

The transfer-confirmation role mismatch is documented in
[Frontend/backend contract](frontend-backend-contract.md) and should be resolved
before using this table as a permission contract.
