# Embedded documentation API

Last verified against source: 2026-07-10.

The service publishes the bilingual WMS guide corpus consumed by the frontend Help
panel and `/docs` pages. The source authoring tree is the WMS section of
`ezy-wiki`; the runtime copy is `Service/Docs/Content/wms` with its generated
`Service/Docs/docs-manifest.json`.

## Endpoints

All endpoints require an authenticated WMS session.

| Method | Route | Behavior |
| --- | --- | --- |
| `GET` | `/api/docs?locale=en|es` | Visible article summaries in manifest order. Unsupported locales fall back to English. |
| `GET` | `/api/docs/search?q=...&locale=en|es&limit=20` | Searches visible titles, descriptions, modules, and slugs. The backend clamps `limit` to 1–50 and returns an empty collection for a blank query. |
| `GET` | `/api/docs/{module}/{slug}?locale=en|es` | Summary plus Markdown for one visible article. Missing, gated, or unpublished content returns `404`. |

The `locale` query parameter supports `en` and `es`; any other value is served as
English. Article content is read from the published `Docs/Content/wms` directory
and cached for the process lifetime.

## Visibility

`WmsDocumentationService` applies visibility checks for every index, search, and
article operation:

- `superUserOnly` requires a super-user session;
- `roles` requires at least one matching `RoleType`, unless the session is a
  super-user;
- feature gates cover bin locations, receipt type, transfer requests, warehouse
  transfers, and transfer confirmation;
- missing files are treated as unavailable and logged server-side.

The frontend may filter the returned index for local search, but it must use the
backend article endpoint for content and must not treat a local manifest as an
authorization source.

## Publishing and maintenance

When WMS source documentation changes:

1. Import both `en/wms` and `es/wms` trees and the WMS screenshot assets into the
   backend Markdown corpus and frontend `public/docs/wms` assets.
2. Regenerate the manifest so article paths, routes, roles, and feature gates match
   `App.tsx`, `useMenus.tsx`, controller authorization, and `ConfigSectionCatalog`.
3. Validate that every manifest file and referenced image exists, and that internal
   `/docs/...` links resolve to a manifest article.
4. Build both repositories; the frontend build must copy `public/docs/wms` into the
   deployed static output when the service hosts the SPA.

The source wiki currently contains 55 English and 55 Spanish WMS articles. That
count is a coverage check, not permission to retain a guide for a route that no
longer exists; stale source-wiki pages must be marked or removed from the embedded
manifest during import.
