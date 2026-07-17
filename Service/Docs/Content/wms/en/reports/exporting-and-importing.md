---
title: Exporting and Importing Reports
description: Move report definitions between WMS installs as a portable JSON bundle. Super User only.
---

Report definitions can be exported to a JSON file and imported into another WMS install. This lets you author and test reports against a staging database, then move them to production, or share a set of reports across customer installations.

## Accessing the Page

Route: `/settings/reports`

Navigation: Side menu → **Report Definitions**

Only **Super Users** can export or import report definitions.

## Exporting

From the definitions list:

- **Export all** (top of the page) — downloads every definition as a single file named `ezy-wms-reports.json`.
- **Export** on a row — downloads just that report as a file named after its slug.

An export contains the authored fields only — the name, slug, SQL, columns, variables, and paging settings. It deliberately omits the report's internal ID, timestamps, author, and Authorization Group grants, because those are specific to the install that created them.

## Importing

1. Click **Import** to open the import dialog.
2. Pick a JSON file — either a full bundle or a bare array exported earlier.
3. The system immediately runs a **dry run** that compiles every report's SQL against **this** install's SAP database.

The preview lists each report with two badges:

| Badge | Meaning |
|-------|---------|
| **New** / **Replaces existing** | Whether the report's slug already exists in this install. A replacement keeps the existing report's ID, so its Authorization Group grants survive. |
| **Ready** / **Not valid here** | Whether the SQL compiled and returned columns on this database, with the database's verbatim message when it did not. |

> **Warning:** Import is **all-or-nothing**. A single invalid report blocks the whole bundle — nothing is written until every report is valid on this system. This avoids leaving a half-imported set behind.

4. When every report is **Ready**, click **Import** to apply. The list refreshes with the imported reports.

> **Tip:** The dry run checks SQL against this install's SAP, which may differ from the source (different company database, different item codes). Always read the preview before applying — a report that was valid on staging can be **Not valid here** on a customer whose schema differs.

## Related Pages

- [Authoring a Report Definition](/docs/reports/authoring-a-report-definition)
- [Reports Overview](/docs/reports)
