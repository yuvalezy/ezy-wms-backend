using Core.DTOs.Reports;
using Core.Entities;
using Core.Enums;
using Core.Interfaces;
using Infrastructure.DbContexts;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using UnitTests.Integration.ExternalSystems;

namespace UnitTests.Integration.Reports;

/// <summary>
/// Creates and exercises the <c>bin-stock</c> report — which bins hold an item in a warehouse — against
/// the real SAP database. This is the drill-down target of <see cref="ReportEndToEndTest"/>'s
/// <c>stock-on-hand</c>: its <c>En Stock</c> column links here, passing the row's item and warehouse
/// as query parameters.
/// </summary>
/// <remarks>
/// Idempotent (deletes by slug first) and leaves the report behind on purpose — a passing run means a
/// usable report exists in the UI.
/// <para>
/// The columns are exactly what SAP actually stores per bin. <c>OIBQ</c> carries only
/// <c>OnHandQty</c>; committed and on-order live on <c>OITW</c> at warehouse level, so there is no
/// honest per-bin "available" to show without deriving it from open pick lists.
/// </para>
/// </remarks>
[TestFixture]
public class BinStockReportTest : BaseExternalTest {
    private const string Slug = "bin-stock";

    /// <summary>
    /// Mirrors <c>SboItemRepository.ItemBinStockAsync</c>, the query already behind the /itemCheck
    /// screen — same tables, same join key (<c>OBIN."AbsEntry" = OIBQ."BinAbs"</c>).
    /// <para>
    /// <c>&lt;&gt; 0</c> rather than <c>&gt; 0</c>: a negative bin is a real data problem, and a stock
    /// report that hides it is the report lying. This matches <c>BinCheckAsync</c>'s choice.
    /// </para>
    /// <para>
    /// <c>RowKey</c> is the mandatory tiebreaker: item+warehouse+bin is the only unique key here, and
    /// paging over a tied column without one silently repeats and drops rows. It must stay
    /// non-sortable — if <c>{{orderBy}}</c> could expand to it, ORDER BY would name it twice and SQL
    /// Server rejects the statement outright.
    /// </para>
    /// </summary>
    private const string Sql = """
        SELECT
            T0."ItemCode", T2."ItemName", T0."WhsCode", T3."WhsName",
            T1."BinCode", T0."OnHandQty", T2."InvntryUom" AS "UoM",
            T0."ItemCode" + '|' + T0."WhsCode" + '|' + T1."BinCode" AS "RowKey"
        FROM OIBQ T0
        INNER JOIN OBIN T1 ON T1."AbsEntry" = T0."BinAbs"
        INNER JOIN OITM T2 ON T2."ItemCode" = T0."ItemCode"
        INNER JOIN OWHS T3 ON T3."WhsCode" = T0."WhsCode"
        WHERE (@ItemCode IS NULL OR T0."ItemCode" = @ItemCode)
          AND (@WhsCode IS NULL OR T0."WhsCode" = @WhsCode)
          AND (@OnlyInStock IS NULL OR @OnlyInStock = 'N' OR T0."OnHandQty" <> 0)
        ORDER BY {{orderBy}}, "RowKey"
        {{paging}}
        """;

    private static readonly string[] SortableKeys =
        ["ItemCode", "ItemName", "WhsCode", "WhsName", "BinCode", "OnHandQty", "UoM"];

    private IServiceScope           scope    = null!;
    private IReportService          reports  = null!;
    private IReportExecutionService executor = null!;
    private Guid                    userId;

    [SetUp]
    public void SetUp() {
        scope    = factory.Services.CreateScope();
        reports  = scope.ServiceProvider.GetRequiredService<IReportService>();
        executor = scope.ServiceProvider.GetRequiredService<IReportExecutionService>();

        var db = scope.ServiceProvider.GetRequiredService<SystemDbContext>();
        User? user = db.Users.FirstOrDefault(u => u.SuperUser && !u.Deleted) ?? db.Users.FirstOrDefault(u => !u.Deleted);
        Assert.That(user, Is.Not.Null, "no user in the WMS database to own the report");
        userId = user!.Id;
    }

    [TearDown]
    public void TearDown() => scope.Dispose();

    private static CreateReportDefinitionRequest BuildRequest() => new() {
        Name        = "Stock por Ubicación",
        Slug        = Slug,
        Description = "Ubicaciones que contienen un artículo en un almacén.",
        Sql         = Sql,
        Enabled     = true,
        DefaultSortColumnKey = "BinCode",
        DefaultSortDirection = ReportSortDirection.Asc,
        DefaultPageSize      = 50,
        TimeoutSeconds       = 60,
        Columns = [
            // Links back out to the item screen, the same way stock-on-hand's code column does.
            new ReportColumnRequest {
                Key = "ItemCode", Label = "Código", Format = ReportColumnFormat.Text, Order = 0,
                LinkType = ReportColumnLinkType.Internal, LinkTemplate = "/itemCheck/{ItemCode}"
            },
            Column("ItemName",  "Descripción", ReportColumnFormat.Text,   1),
            Column("WhsCode",   "Almacén",     ReportColumnFormat.Text,   2),
            Column("WhsName",   "Nombre",      ReportColumnFormat.Text,   3),
            Column("BinCode",   "Ubicación",   ReportColumnFormat.Text,   4),
            Column("OnHandQty", "En Stock",    ReportColumnFormat.Number, 5),
            Column("UoM",       "UdM",         ReportColumnFormat.Text,   6),
            new ReportColumnRequest {
                Key = "RowKey", Label = "RowKey", Format = ReportColumnFormat.Text,
                Visible = false, Sortable = false, Order = 7
            }
        ],
        // These names are the drill-down contract: stock-on-hand's link writes ?ItemCode=..&WhsCode=..,
        // and the runner matches query params to variable names. Renaming one breaks the link silently.
        Variables = [
            new ReportVariableRequest { Name = "ItemCode", Label = "Código", Type = ReportVariableType.Text, Order = 0 },
            new ReportVariableRequest {
                Name = "WhsCode", Label = "Almacén", Type = ReportVariableType.SqlLookup, Order = 1,
                LookupSql = """SELECT "WhsCode" AS "Value", "WhsName" AS "Label" FROM OWHS WHERE "BinActivat" = 'Y' ORDER BY "WhsCode" """
            },
            new ReportVariableRequest { Name = "OnlyInStock", Label = "Solo con stock", Type = ReportVariableType.YesNo, Order = 2 }
        ]
    };

    private static ReportColumnRequest Column(string key, string label, ReportColumnFormat format, int order) =>
        new() { Key = key, Label = label, Format = format, Order = order, Decimals = format == ReportColumnFormat.Number ? 2 : 0 };

    [Test, Order(1)]
    public async Task Test_01_Save_CompilesEverySortableColumn() {
        ReportValidationResult result = await reports.TestAsync(BuildRequest());

        Assert.That(result.Errors, Is.Empty);
        Assert.That(result.Valid, Is.True);
        Assert.That(result.Columns.Select(c => c.Key), Is.SupersetOf(SortableKeys));
    }

    [Test, Order(2)]
    public async Task Test_02_Create_PersistsTheReport() {
        foreach (ReportDefinitionSummary existing in (await reports.GetAllAsync()).Where(r => r.Slug == Slug))
            await reports.DeleteAsync(existing.Id);

        ReportDefinitionDetail created = await reports.CreateAsync(BuildRequest(), userId);

        Assert.Multiple(() => {
            Assert.That(created.Slug, Is.EqualTo(Slug));
            Assert.That(created.Columns, Has.Count.EqualTo(8));
            Assert.That(created.Variables, Has.Count.EqualTo(3));
        });
    }

    [Test, Order(3)]
    public async Task Test_03_Run_ReturnsBinsForRealStock() {
        ReportDefinition definition = await LoadAsync();
        ReportPageResult page = await executor.ExecutePageAsync(definition, new ReportExecutionRequest { Limit = 10 });

        Assert.That(page.Rows, Is.Not.Empty, "no bin stock in SAP at all — cannot prove the report works");
        Assert.That(page.Rows[0].ContainsKey("BinCode"), Is.True);
        Assert.That(page.Rows[0]["BinCode"], Is.Not.Null, "a bin row without a bin code is meaningless");
    }

    /// <summary>
    /// The drill-down itself: the exact filter combination stock-on-hand's link produces must return
    /// only that item's bins in that warehouse. This is the test that would catch a variable rename.
    /// </summary>
    [Test, Order(4)]
    public async Task Test_04_Run_FiltersToOneItemAndWarehouse_AsTheDrillDownLinkDoes() {
        ReportDefinition definition = await LoadAsync();

        ReportPageResult any = await executor.ExecutePageAsync(definition, new ReportExecutionRequest { Limit = 1 });
        Assert.That(any.Rows, Is.Not.Empty, "no bin stock in SAP to drill into");
        var itemCode = (string)any.Rows[0]["ItemCode"]!;
        var whsCode  = (string)any.Rows[0]["WhsCode"]!;

        ReportPageResult drilled = await executor.ExecutePageAsync(definition, new ReportExecutionRequest {
            Limit = 200,
            Variables = new Dictionary<string, ReportVariableValue> {
                ["ItemCode"] = new() { Value = itemCode },
                ["WhsCode"]  = new() { Value = whsCode }
            }
        });

        Assert.That(drilled.Rows, Is.Not.Empty, "the drill-down returned nothing for a row that exists");
        Assert.That(drilled.Rows.All(r => (string)r["ItemCode"]! == itemCode), Is.True,
            "the ItemCode filter leaked — the drill-down would show other items' bins");
        Assert.That(drilled.Rows.All(r => (string)r["WhsCode"]! == whsCode), Is.True,
            "the WhsCode filter leaked — the drill-down would show other warehouses' bins");
    }

    [Test, Order(5)]
    public async Task Test_05_Run_SortsByEverySortableColumn() {
        ReportDefinition definition = await LoadAsync();

        foreach (string key in SortableKeys) {
            ReportPageResult page = await executor.ExecutePageAsync(definition, new ReportExecutionRequest {
                Limit = 5, SortColumnKey = key, SortDirection = ReportSortDirection.Desc
            });
            Assert.That(page.SortColumnKey, Is.EqualTo(key), $"sorting by '{key}' did not apply");
        }
    }

    [Test, Order(6)]
    public async Task Test_06_Available_ListsTheReportForItsOwner() {
        IReadOnlyList<ReportDefinitionSummary> available = await reports.GetAvailableAsync(userId);
        Assert.That(available.Select(r => r.Slug), Does.Contain(Slug));
    }

    private async Task<ReportDefinition> LoadAsync() {
        ReportDefinition? definition = await reports.GetForExecutionAsync(Slug, userId);
        Assert.That(definition, Is.Not.Null, $"report '{Slug}' not found — run Test_02 first");
        return definition!;
    }
}
