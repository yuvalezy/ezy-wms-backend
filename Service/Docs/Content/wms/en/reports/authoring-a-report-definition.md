---
title: Authoring a Report Definition
description: Create, test, and edit the SQL and layout of a report definition. Super User only.
---

A report definition is the saved query and layout behind every report. As a Super User you write the SQL, test it against your live SAP database, declare its columns and filters, and then share it with your teams through their Authorization Groups. End users never see the SQL — they only run the finished report.

## Accessing the Page

Route: `/settings/reports` (list), `/settings/reports/add` (new), `/settings/reports/:id` (edit)

Navigation: Side menu → **Report Definitions**

Only **Super Users** can open the report definitions pages.

## The Definitions List

The list shows every report definition — enabled or disabled — with its name, description, and column and filter counts. Each row has actions: **Run** (open the report), **Export** (download this definition), **Edit**, and **Delete**. The top of the page has **Import** and **Export all** buttons (see [Exporting and Importing Reports](/docs/reports/exporting-and-importing)).

Click **+** (or **Add report**) to create a new definition. Click **Edit** on any row to open the editor.

## How Authoring Works

Only your SAP database can truly validate a report's SQL, so the editor does not let you hand-type columns. Instead you write the SQL, click **Test query**, and the system compiles the query inside a transaction that always rolls back — it discovers the exact columns your SQL returns and merges them into the **Columns** card. You then label and format them.

Save is blocked until the query has been tested and has discovered at least one column.

## General

| Field | Required | Notes |
|-------|----------|-------|
| **Name** | Yes | Up to 100 characters. Shown in the menu and as the report's title. |
| **Slug** | Yes | The URL identifier — lowercase letters, numbers, and hyphens only (`stock-on-hand`). Auto-filled from the name when creating, but never rewritten when editing because it is a live URL. |
| **Description** | No | Up to 500 characters. Shown under the report title and on the index cards. |
| **Enabled** | No | Switch. A disabled report stays in the definitions list but is hidden from end users and cannot be run. |
| **Add OPTION (RECOMPILE)** | No | Switch. Recommended for the `(@Var IS NULL OR col = @Var)` pattern. Never write `OPTION (RECOMPILE)` by hand — use this switch. |

## SQL

The **SQL** card holds the query body. Two tokens are required and must each appear exactly once:

- `{{orderBy}}` — replaced with the column the user is sorting on.
- `{{paging}}` — replaced with the `OFFSET … FETCH NEXT` paging clause.

Write filters as parameters with the `(@Name IS NULL OR col = @Name)` pattern so an empty filter returns all rows. Multi-value dropdowns arrive as a single CSV parameter — split them with `STRING_SPLIT(@Name, ',')`.

> **Tip:** Always add a unique tiebreaker after `{{orderBy}}` so paging is deterministic — for example `ORDER BY {{orderBy}}, T0."DocEntry"`. Declare that tiebreaker as a hidden, non-sortable column (see **Columns**).

Rules the query must follow:

- Must start with `SELECT` or `WITH`.
- Must contain `{{orderBy}}` and `{{paging}}` exactly once each (lowercase — `{{OrderBy}}` is rejected).
- Must be a single statement with no trailing semicolon (`;`).

Click **Test query** to validate the SQL and discover its columns. The result shows either "Query is valid" with the number of discovered columns, or "Query failed" with the database's verbatim error message and any warnings.

## Paging and Sorting

| Field | Notes |
|-------|-------|
| **Rows per page** | 1–200, default 50. |
| **Timeout (seconds)** | 5–300, default 60. A run that exceeds this is cancelled. |
| **Default sort column** | Any sortable column, or "No default sort". |
| **Default direction** | Ascending or Descending. |
| **Total row count** | **None (faster)** or **Exact (on request)**. Paging never needs a total. Choose **Exact** only if users must see the total row count, which they then request on demand with **Show total**. |
| **Custom count query** | Appears only for **Exact** mode. An optional cheaper hand-written `COUNT` query used by the total-count button. |

## Columns

Columns arrive from **Test query** — you cannot add them by hand. Each discovered column shows the exact name the SQL returns (read-only) and lets you configure how it displays.

| Field | Notes |
|-------|-------|
| **Label** | Header text shown to the user. |
| **Format** | Text, Number, Currency, Percent, Date, Date and time, Time, Boolean, or Binary. |
| **Alignment** | Automatic (derived from format), Left, Center, or Right. |
| **Decimals** | 0–10, for numeric formats. |
| **Visible** | Switch. Turn off for helper columns the SQL needs but the user should not see (for example, a paging tiebreaker or a hidden value used only by a link). |
| **Sortable** | Switch. Locked off with a note when the column's SQL type cannot be sorted on. Every sortable column is validated at save time, not just the default. |

Use the up and down buttons to set the display order.

### Column Links

Each column can optionally render as a hyperlink. Set **Link** to one of:

- **No link** — plain text (default).
- **Internal page** — a WMS route navigated without reloading. The template must start with a single `/`, for example `/itemCheck/{ItemCode}`.
- **External site** — an absolute `http`/`https` URL that opens in a new tab, for example `https://www.google.com/search?q={WhsName}`.

The **Link target** is a template with `{ColumnKey}` placeholders filled from the current row. To drill into another report, link to `/reports/its-slug?VariableName={ColumnKey}` — for example, `/reports/bin-stock?ItemCode={ItemCode}&WhsCode={WhsCode}`. The target report's filter names are the contract; renaming a filter on the target breaks the link silently.

> **Note:** If a link's placeholder resolves to an empty value, the link is hidden and the cell shows plain text. A leftover template on a **No link** column is dropped on save, so you do not have to clear the box before toggling a link off.

## Variables

Variables are the filters end users fill in. Click **Add variable** to declare one. Each is a card:

| Field | Notes |
|-------|-------|
| **Name** | The parameter name without `@` — for example `FromDate`. Letters, numbers, and underscores; must not start with `__` (reserved for paging). Unique, case-insensitive. |
| **Label** | The text shown beside the filter. |
| **Type** | See the table below. |
| **Decimals** | For the Decimal type only. |
| **Required** | Switch. A required empty filter blocks the run. |
| **Allow multiple** | Switch, for the two dropdown types only. Renders a multi-select; values bind as one CSV parameter. |
| **Default value** | Used when the user does not set the filter and on the automatic first run. For a **Date range** this is disabled. |

### Variable Types

| Type | Bound as | Notes |
|------|----------|-------|
| Text | Text | General string filter. |
| Whole number | Integer | Keeps an integer column on its index. |
| Decimal number | Decimal | Uses the variable's **Decimals**. |
| Date | Date | ISO date. |
| Date and time | DateTime | ISO date and time. |
| Date range | Two dates | Emits `@{Name}From` and `@{Name}To`. From must be on or before To. |
| Yes/No (bit) | Bit | Genuine bit columns only. |
| Yes/No (Y/N) | Char(1) | SAP Business One `Y`/`N` flag columns. |
| Dropdown (fixed list) | The selected value | A short list you define with **Options** (each has a **Value** sent to SQL and a **Label** shown to the user). |
| Dropdown (SQL query) | The selected value | A **Lookup query** that must return exactly two columns named `Value` and `Label`. All the report's variables are available as parameters, so a lookup can depend on another filter (bins for the selected warehouse, for example). Limited to 1,000 rows. |

## Saving

Click **Save** to create or update the definition. Save is blocked if there are no columns yet ("Run **Test query** first") or if any variable has a validation error.

Once saved, share the report with your teams from the [Authorization Groups](/docs/settings/authorization-groups) screen — a report is invisible to end users until it is granted to a group.

## Related Pages

- [Running a Report](/docs/reports/running-a-report)
- [Exporting and Importing Reports](/docs/reports/exporting-and-importing)
- [Authorization Groups](/docs/settings/authorization-groups)
