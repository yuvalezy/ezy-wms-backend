---
title: Reports
description: Overview of the Reports module — run live, read-only SQL reports authored by administrators against your SAP data.
---

The Reports module lets you run live, read-only reports against your SAP Business One data without leaving WMS. Each report is a saved SQL query that an administrator has authored, tested, and shared with your team. You pick a report from the menu, set your filters, and read the results — you never write SQL yourself.

Reports are not part of any single warehouse workflow. Use them for ad-hoc questions such as *how much stock do I have by warehouse?*, *which bins hold this item?*, or *what are the dimensions per piece and per box?*

## Accessing the Reports

Each report you have been granted appears as its own entry in the side menu, under a violet report icon. The menu lists only the reports shared with you — if you do not see any reports, ask a Super User to grant them through your Authorization Group (see [Sharing Reports](/docs/settings/authorization-groups)).

You can also open the **Reports** index page to see every report available to you as a card grid.

Route: `/reports`

Navigation: Side menu → a report's name, or **Reports** for the full list

## How Access Works

Reports use a different permission model from the rest of WMS:

- **Running a report** — there is no dedicated "Reports" role. Access is granted **per report** through your Authorization Group. A grant takes effect immediately, with no need to log in again, and the report appears in your menu straight away.
- **Authoring a report** — only **Super Users** can create, edit, test, import, or delete report definitions.

> **Note:** A report only appears in your menu once an administrator has shared it with one of your Authorization Groups. A freshly installed WMS ships with no reports at all — they exist only once an administrator creates or imports them.

## What You See on the Index Page

The **Reports** index page shows a card for each report shared with you, with its name and description. Click a card to open the [report runner](/docs/reports/running-a-report) and start working with it.

If the grid is empty, no reports have been shared with your group yet.

## Module Pages

| Page | Description | Path |
|------|-------------|------|
| [Running a Report](/docs/reports/running-a-report) | Filter, sort, page, and export a report | `/reports/:slug` |
| [Authoring a Report Definition](/docs/reports/authoring-a-report-definition) | Create and edit report definitions (Super User) | `/settings/reports` |
| [Exporting and Importing Reports](/docs/reports/exporting-and-importing) | Move report definitions between installs (Super User) | `/settings/reports` |

## Related Pages

- [Running a Report](/docs/reports/running-a-report)
- [Authorization Groups](/docs/settings/authorization-groups)
