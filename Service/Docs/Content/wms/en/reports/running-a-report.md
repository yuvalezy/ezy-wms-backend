---
title: Running a Report
description: Filter, sort, page, export, and drill into the results of a report.
---

The report runner is where you work with a report's results. You set filters, run the query, and read the rows that SAP returns. Sort by any column, page through the results, export to Excel, or follow a cell's link into another page or a second report.

## Accessing the Page

Route: `/reports/:slug`

Navigation: Side menu → the report's name

Any authenticated user can open a report shared with their Authorization Group. If a report has not been shared with you, it does not appear in your menu.

## Running the Report

1. Open the report from the side menu.
2. Set the filters you need (see **Filter Bar** below).
3. Click **Run**.

The page runs automatically on load when every required filter already has a value — from its default, or from filters carried in a drill-down link. If a required filter is empty, the page waits for you to fill it in and click **Run**.

While a run is in progress the button reads **Running...**. When it finishes, the elapsed time shows at the bottom ("Ran in N ms").

## Filter Bar

The filter bar shows one control for each filter the report's author declared. Each filter is labelled, and required filters are marked with `*`.

| Filter type | Control |
|-------------|---------|
| Text / whole number / decimal | A text or number input |
| Date | A calendar date picker |
| Date and time | A date-and-time input |
| Date range | Two date pickers — **From** and **To** |
| Yes/No (bit) or Yes/No (Y/N) | A Yes/No/**Any** dropdown |
| Dropdown (fixed list) | A searchable dropdown, or a multi-select checkbox popover when **Allow multiple** is on |
| Dropdown (SQL query) | A searchable dropdown whose options come from a live query — for example, the list of warehouses. These refresh whenever another filter changes, so one filter can narrow the next (warehouse → bins). |

Click **Reset filters** to return every filter to the values the page opened with. Reset does not clear a drill-down link's values back to the report's defaults — it restores exactly what the link asked for.

> **Note:** Required filters that are empty, and date ranges where **From** is later than **To**, block the run with an error message.

## Results Table

Rows appear in a sortable table. Click any column header marked sortable to re-run from the first page in that order — the sort arrow only moves once the server confirms the ordering.

| Behaviour | Detail |
|-----------|--------|
| Cell formatting | Follows the column's format — numbers, currency, percentages, dates, and times render accordingly. Empty (NULL) cells show an em dash (—). |
| Date and time values | Are shown in UTC, matching how SAP stores them. |
| Column links | A cell may be a hyperlink. See **Drill-down Links** below. |

## Paging

There is no total row count by default — the report fetches one page at a time and tells you whether more rows follow. Use **Previous** and **Next** to move between pages. A label shows the row range for the current page ("Rows X–Y").

If the report's author enabled **Exact** total counting, a **Show total** button appears that computes the full count on demand and then displays "N rows in total".

## Exporting to Excel

Click **Export to Excel** in the page header to download every matching row as an `.xlsx` file named after the report.

- Rows are pulled in the background in chunks, honouring your current filters and sort.
- Numeric columns are written as real numbers so Excel can sum and sort them.
- Exports are capped at 50,000 rows. If the cap is reached, a warning tells you the export was truncated.

## Drill-down Links

A column can be a link. The author decides where each link goes:

- An **internal page** opens another WMS screen without reloading — for example, an item code that opens **Item Check**.
- An **external site** opens in a new tab — for example, a web search using a column's value.
- A link to **another report** carries the current row's values as filters, so the target report opens already filtered. For example, an *On Hand* quantity can link to a *Stock by Bin* report for the same item and warehouse.

If the row value behind a link is empty, the link is hidden and the cell shows plain text.

> **Warning:** When a drill-down link asks for filters the target report cannot apply — an unknown parameter, an invalid value, or a duplicate — an amber banner lists the problems and the report does **not** run automatically. This stops a link from silently showing a wider result set than it promised.

## Related Pages

- [Reports Overview](/docs/reports)
- [Authoring a Report Definition](/docs/reports/authoring-a-report-definition)
- [Authorization Groups](/docs/settings/authorization-groups)
