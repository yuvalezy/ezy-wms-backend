# Frontend/backend contract

This document records the integration assumptions shared by the React SPA and the
ASP.NET Core service. Last verified against both repositories: 2026-07-10.

## Transport and deployment

The frontend creates an Axios client with a base URL of `${ServerUrl}/api/`. In
development, `ServerUrl` comes from `window.__env.VITE_APP_SERVER_URL` or
`VITE_APP_SERVER_URL`; in production it is the browser origin. A separate
production API therefore needs same-origin hosting or a reverse proxy.
The backend serves the built SPA from `Service/wwwroot` and falls back to
`index.html`, so same-origin production hosting is supported.

The frontend sends `withCredentials: true`, attaches `Authorization: Bearer ...`
from `sessionStorage` when present, and sends `X-Device-UUID`. The backend also
accepts the HTTP-only session cookie. CORS and cookie settings must therefore be
configured together for split-origin deployments.

## Startup and session flow

1. The SPA calls company/system bootstrap endpoints before a normal business route.
2. If the backend is not SAP-ready, lockdown responses (`503`) are expected and
   `SystemGate` keeps the user in the setup path.
3. The user logs in with a password and optional warehouse/device name.
4. The backend returns session/user capabilities; the SPA stores the bearer token
   in `sessionStorage`, registers the device UUID, and loads company/user data.
5. The SPA filters menus and protects routes using roles, super-user status, device
   status, account validity, and feature settings.
6. A `401` causes the Axios response interceptor to navigate to
   `/login?reason=session-expired`; a `403` navigates to `/unauthorized`.

The backend's authorization attributes and service checks remain authoritative even
when the SPA has hidden a route.

## Role and route mapping

The canonical role values are in `ezy-wms-backend/Core/Enums/RoleType.cs`; the SPA
mirror is `ezy-wms-frontend/src/features/authorization-groups/data/authorization-group.ts`.
Do not rename or renumber existing enum values casually.

| Workflow | Main SPA route(s) | Roles used by SPA | Backend controller family |
| --- | --- | --- | --- |
| Goods receipt | `/goodsReceipt*` | `GoodsReceipt`, `GoodsReceiptSupervisor` | `GoodsReceiptController`, `GoodsReceiptReportController` |
| Goods receipt confirmation | `/goodsReceiptConfirmation*` | `GoodsReceiptConfirmation`, `GoodsReceiptConfirmationSupervisor` | `GoodsReceiptController`, `GoodsReceiptReportController` |
| Picking | `/pick*` | `Picking`, `PickingSupervisor` | `PickingController` |
| Counting | `/counting*` | `Counting`, `CountingSupervisor` | `CountingController` |
| Transfer | `/transfer*` | `Transfer`, `TransferSupervisor`, `TransferRequest` | `TransferController` |
| Transfer confirmation | `/transferConfirmation*` | `TransferConfirmation`, `TransferConfirmationSupervisor` | `GoodsReceiptController`, `GoodsReceiptReportController` |
| Direct transfer | `/directTransfer` | `DirectTransfer` | `DirectTransferController` |
| Checks | `/itemCheck`, `/binCheck` | supervisor roles | `ItemsController` |
| Administration | `/settings/*` | `superUser` | user/device/configuration/license/alert controllers |

The SPA also gates transfer and receipt routes by settings such as
`EnableWarehouseTransfer`, `EnableTransferConfirm`, `EnableTransferRequest`, and
`GoodsReceiptType`, and gates bin operations on the user `binLocations` capability.

### Known discrepancy found during documentation validation

Transfer-confirmation authorization is not consistent across the backend endpoints:
`GoodsReceiptController.GetGoodsReceipts` currently checks `Transfer`/
`TransferSupervisor` for the transfer-confirmation list, while the create/detail/
process paths and `GoodsReceiptReportController` use
`TransferConfirmation`/`TransferConfirmationSupervisor`. The SPA route uses the
confirmation roles, while its supervisor menu currently uses `TransferSupervisor`.

This can produce a real deep-link/list failure for a user assigned only the
confirmation roles. Treat it as a backend/frontend contract defect to resolve before
changing either role model; the exact source locations are
`Service/Controllers/GoodsReceiptController.cs`,
`Service/Controllers/GoodsReceiptReportController.cs`, `src/App.tsx`, and
`src/hooks/useMenus.tsx`.

## API module conventions

Frontend data services live under `src/features/*/data/` and call controller routes
under `/api`. The backend DTOs in `Core/DTOs` are the contract; TypeScript models
should be updated when DTO shape or enum values change.

Important compatibility details:

- Quantities are decimal values. Do not narrow them to integer types in TypeScript
  or format them with lossy arithmetic.
- Dates from the backend are normalized by the Axios response interceptor; preserve
  UTC semantics when adding endpoints.
- Error payloads are normalized/displayed through the frontend error handling, while
  backend exception middleware owns the HTTP error envelope.
- `X-Device-UUID` is part of device/session behavior; do not omit it from new API
  calls by bypassing `axiosInstance`.
- SignalR uses `/hubs/notifications` and authenticated user identity. Any change to
  the hub route or token/cookie transport must update `NotificationContext`.

## Configuration contract

The settings returned to the SPA are a projection of the backend configuration
system, not a second frontend-owned configuration store. Backend configuration is
stored as section JSON with PascalCase section/leaf semantics and secret masking;
the frontend configuration editor serializes the edited JSON through
`ConfigurationController`.

When adding a configuration-driven feature:

1. Add/extend the backend settings model and catalog/validation rules.
2. Decide whether the section is live-reloadable or restart-required.
3. Expose the setting in `UserInfo`/configuration responses as appropriate.
4. Add frontend types, editor controls, route/menu feature gates, and both English
   and Spanish translation keys.
5. Test the disabled, enabled, invalid, and stale-version cases.

## Cross-repo change checklist

- Controller route and HTTP verb added/changed.
- Request/response DTO and enum values updated in both languages/runtimes where
  applicable.
- Backend role/permission and lockdown/license behavior reviewed.
- Frontend data service uses `axiosInstance` and preserves device/auth headers.
- Page route, `ProtectedRoute`, menu, feature gate, and translations updated.
- Loading, `401`, `403`, `503`, offline, and SAP failure states exercised.
- Swagger, unit tests, and a production frontend build checked.
