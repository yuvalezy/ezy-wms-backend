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
/// Drives the reporting feature end to end against the real SAP and WMS databases: discover, create, run,
/// sort, filter, page. Resolves the same services the controllers do, so everything below the HTTP/auth layer
/// is the production path.
/// </summary>
/// <remarks>
/// This exists because the unit suite cannot see these bugs. It stayed green through a duplicated ORDER BY
/// column, a SqlParameter reused across two commands, and a Test/Columns deadlock — all of which appear only
/// when a real SqlCommand meets a real SAP schema. Reach for this, not the unit suite, when changing the
/// engine.
/// <para>
/// Idempotent: deletes the report by slug first, so it can be run repeatedly. It leaves the report behind on
/// purpose — a passing run means a usable report exists in the UI.
/// </para>
/// </remarks>
[TestFixture]
public class ReportEndToEndTest : BaseExternalTest {
    private const string Slug = "stock-on-hand";

    /// <summary>
    /// The tiebreaker after <c>{{orderBy}}</c> is a hidden, non-sortable row key. It has to be non-sortable:
    /// if <c>{{orderBy}}</c> could expand to <c>RowKey</c>, the ORDER BY would name it twice and SQL Server
    /// rejects the statement. It has to exist at all because OITW's key is (ItemCode, WhsCode) — both of them
    /// sortable display columns — so there is no natural tiebreaker, and without one, paging over a column
    /// with ties (OnHand has many) silently repeats and drops rows between pages.
    /// </summary>
    private const string Sql = """
        SELECT
            T0."ItemCode", T1."ItemName", T0."WhsCode", T2."WhsName",
            T0."OnHand", T0."IsCommited" AS "Committed", T0."OnOrder",
            (T0."OnHand" - T0."IsCommited") AS "Available", T1."InvntryUom" AS "UoM",
            T0."ItemCode" + '|' + T0."WhsCode" AS "RowKey"
        FROM OITW T0
        INNER JOIN OITM T1 ON T1."ItemCode" = T0."ItemCode"
        INNER JOIN OWHS T2 ON T2."WhsCode" = T0."WhsCode"
        WHERE (@WhsCode IS NULL OR T0."WhsCode" = @WhsCode)
          AND (@ItemCode IS NULL OR T0."ItemCode" LIKE '%' + @ItemCode + '%')
          AND (@OnlyInStock IS NULL OR @OnlyInStock = 'N' OR T0."OnHand" > 0)
        ORDER BY {{orderBy}}, "RowKey"
        {{paging}}
        """;

    private static readonly string[] SortableKeys =
        ["ItemCode", "ItemName", "WhsCode", "WhsName", "OnHand", "Committed", "OnOrder", "Available", "UoM"];

    private IServiceScope       scope       = null!;
    private IReportService      reports     = null!;
    private IReportExecutionService executor = null!;
    private Guid                userId;

    [SetUp]
    public void SetUp() {
        scope    = factory.Services.CreateScope();
        reports  = scope.ServiceProvider.GetRequiredService<IReportService>();
        executor = scope.ServiceProvider.GetRequiredService<IReportExecutionService>();

        // A real user: CreatedByUserId is a FK to Users, so Guid.Empty fails the insert. Prefer a superuser —
        // access is checked live against the DB, so the run tests need someone who can actually see the report.
        var db = scope.ServiceProvider.GetRequiredService<SystemDbContext>();
        User? user = db.Users.FirstOrDefault(u => u.SuperUser && !u.Deleted) ?? db.Users.FirstOrDefault(u => !u.Deleted);
        Assert.That(user, Is.Not.Null, "no user in the WMS database to own the report");
        userId = user!.Id;
    }

    [TearDown]
    public void TearDown() => scope.Dispose();

    private static CreateReportDefinitionRequest BuildRequest(bool rowKeySortable = false) => new() {
        Name        = "Stock on Hand by Warehouse",
        Slug        = Slug,
        Description = "Item stock per warehouse, straight from SAP.",
        Sql         = Sql,
        Enabled     = true,
        DefaultSortColumnKey = "OnHand",
        DefaultSortDirection = ReportSortDirection.Desc,
        DefaultPageSize      = 50,
        TimeoutSeconds       = 60,
        Columns = [
            Column("ItemCode",  "Código",       ReportColumnFormat.Text,   0),
            Column("ItemName",  "Descripción",  ReportColumnFormat.Text,   1),
            Column("WhsCode",   "Almacén",      ReportColumnFormat.Text,   2),
            Column("WhsName",   "Nombre",       ReportColumnFormat.Text,   3),
            Column("OnHand",    "En Stock",     ReportColumnFormat.Number, 4),
            Column("Committed", "Comprometido", ReportColumnFormat.Number, 5),
            Column("OnOrder",   "En Pedido",    ReportColumnFormat.Number, 6),
            Column("Available", "Disponible",   ReportColumnFormat.Number, 7),
            Column("UoM",       "UdM",          ReportColumnFormat.Text,   8),
            // Plumbing, not data: hidden from the grid and never sortable.
            new ReportColumnRequest {
                Key = "RowKey", Label = "RowKey", Format = ReportColumnFormat.Text,
                Visible = false, Sortable = rowKeySortable, Order = 9
            }
        ],
        Variables = [
            new ReportVariableRequest {
                Name = "WhsCode", Label = "Almacén", Type = ReportVariableType.SqlLookup, Order = 0,
                LookupSql = """SELECT "WhsCode" AS "Value", "WhsName" AS "Label" FROM OWHS ORDER BY "WhsCode" """
            },
            new ReportVariableRequest { Name = "ItemCode", Label = "Código", Type = ReportVariableType.Text, Order = 1 },
            // YesNo, not Boolean: SAP stores these as 'Y'/'N' in char(1), which a Bit would never match.
            new ReportVariableRequest { Name = "OnlyInStock", Label = "Solo con stock", Type = ReportVariableType.YesNo, Order = 2 }
        ]
    };

    private static ReportColumnRequest Column(string key, string label, ReportColumnFormat format, int order) =>
        new() { Key = key, Label = label, Format = format, Order = order, Decimals = format == ReportColumnFormat.Number ? 2 : 0 };

    [Test, Order(1)]
    public async Task Test_01_Discover_ReturnsColumnsEvenWhenASortShapeFails() {
        // The deadlock guard. RowKey left sortable is invalid, but discovery must still hand back the columns —
        // the Columns editor is the only place to clear that flag, and it is Test that fills the editor.
        var request = BuildRequest(rowKeySortable: true);
        request.Columns.Clear();   // the Add screen has no columns yet; this is the real Test-path shape

        ReportValidationResult result = await reports.TestAsync(request);

        Assert.That(result.Columns.Select(c => c.Key), Is.SupersetOf(SortableKeys),
            "discovery must return every output column");
        Assert.That(result.Valid, Is.True,
            $"Test must not judge sortable flags the author never set: {string.Join(" | ", result.Errors)}");
    }

    [Test, Order(2)]
    public async Task Test_02_Save_RejectsASortableTiebreaker_AndNamesIt() {
        // Save does hold the author to their own flags, and the message has to say which key broke.
        ReportValidationResult result = await reports.TestAsync(BuildRequest(rowKeySortable: true));

        Assert.That(result.Valid, Is.False, "a sortable tiebreaker collides with {{orderBy}} and must be rejected");
        Assert.That(result.Errors.Any(e => e.Contains("RowKey")), Is.True,
            $"the error must name the offending sort key, not just echo SQL Server: {string.Join(" | ", result.Errors)}");
    }

    [Test, Order(3)]
    public async Task Test_03_Save_CompilesEverySortableColumn() {
        ReportValidationResult result = await reports.TestAsync(BuildRequest());

        Assert.That(result.Valid, Is.True, $"valid definition rejected: {string.Join(" | ", result.Errors)}");
        Assert.That(result.Columns, Is.Not.Empty);
    }

    [Test, Order(4)]
    public async Task Test_04_Create_PersistsTheReport() {
        foreach (ReportDefinitionSummary existing in (await reports.GetAllAsync()).Where(r => r.Slug == Slug))
            await reports.DeleteAsync(existing.Id);

        ReportDefinitionDetail created = await reports.CreateAsync(BuildRequest(), userId);

        Assert.Multiple(() => {
            Assert.That(created.Slug, Is.EqualTo(Slug));
            Assert.That(created.Columns, Has.Count.EqualTo(10));
            Assert.That(created.Variables, Has.Count.EqualTo(3));
        });
    }

    [Test, Order(5)]
    public async Task Test_05_Run_ReturnsRowsAndHonoursTheDefaultSort() {
        ReportDefinition definition = await LoadAsync();
        ReportPageResult page = await executor.ExecutePageAsync(definition, new ReportExecutionRequest { Limit = 10 });

        Assert.That(page.Rows, Is.Not.Empty, "the report returned no rows");
        var onHand = page.Rows.Select(r => Convert.ToDecimal(r["OnHand"])).ToList();
        Assert.That(onHand, Is.Ordered.Descending, "default sort is OnHand desc");
    }

    [Test, Order(6)]
    public async Task Test_06_Run_SortsByEverySortableColumn() {
        ReportDefinition definition = await LoadAsync();

        // The regression that shipped: each of these compiled only when someone clicked its header.
        foreach (string key in SortableKeys) {
            ReportPageResult page = await executor.ExecutePageAsync(definition,
                new ReportExecutionRequest { SortColumnKey = key, SortDirection = ReportSortDirection.Asc, Limit = 5 });
            Assert.That(page.Rows, Is.Not.Empty, $"sorting by '{key}' returned nothing");
        }
    }

    [Test, Order(7)]
    public async Task Test_07_Run_FiltersByLookupAndYesNo() {
        ReportDefinition definition = await LoadAsync();

        ReportPageResult page = await executor.ExecutePageAsync(definition, new ReportExecutionRequest {
            Variables = {
                ["WhsCode"]     = new ReportVariableValue { Value = "01" },
                ["OnlyInStock"] = new ReportVariableValue { Value = "true" }
            },
            Limit = 25
        });

        Assert.That(page.Rows, Is.Not.Empty);
        Assert.Multiple(() => {
            Assert.That(page.Rows.Select(r => (string?)r["WhsCode"]), Is.All.EqualTo("01"), "warehouse filter ignored");
            Assert.That(page.Rows.Select(r => Convert.ToDecimal(r["OnHand"])), Is.All.GreaterThan(0m),
                "YesNo filter ignored — this is what a Bit binding would silently do against SAP's 'Y'/'N'");
        });
    }

    [Test, Order(8)]
    public async Task Test_08_Run_PagesWithoutRepeatingOrDroppingRows() {
        ReportDefinition definition = await LoadAsync();

        ReportPageResult first  = await executor.ExecutePageAsync(definition, new ReportExecutionRequest { Offset = 0, Limit = 10 });
        ReportPageResult second = await executor.ExecutePageAsync(definition, new ReportExecutionRequest { Offset = 10, Limit = 10 });

        Assert.That(first.HasMore, Is.True, "limit+1 probing should see a second page");
        var firstKeys  = first.Rows.Select(r => (string)r["RowKey"]!).ToList();
        var secondKeys = second.Rows.Select(r => (string)r["RowKey"]!).ToList();
        Assert.That(firstKeys, Has.Count.EqualTo(10), "the probe row must be trimmed off");
        Assert.That(firstKeys.Intersect(secondKeys), Is.Empty,
            "pages overlap — the tiebreaker is not making the sort deterministic");
    }

    [Test, Order(9)]
    public async Task Test_09_Run_ReturnsNullNotZeroForNullColumns() {
        // 212 of 218 demo items have a NULL UoM. A null must arrive as null so the client can render a dash;
        // coercing it to 0 or "" here would make the report state something SAP never said.
        ReportDefinition definition = await LoadAsync();
        ReportPageResult page = await executor.ExecutePageAsync(definition, new ReportExecutionRequest { Limit = 50 });

        Assert.That(page.Rows.Any(r => r["UoM"] is null), Is.True, "expected at least one NULL UoM cell");
        Assert.That(page.Rows.Where(r => r["UoM"] is not null).Select(r => r["UoM"]), Is.All.InstanceOf<string>());
    }

    [Test, Order(10)]
    public async Task Test_10_Lookup_ReturnsWarehouseOptions() {
        ReportDefinition definition = await LoadAsync();
        IReadOnlyList<ReportLookupOption> options =
            await executor.ExecuteLookupAsync(definition, "WhsCode", new ReportExecutionRequest());

        Assert.That(options, Is.Not.Empty);
        Assert.That(options.Any(o => o.Value == "01"), Is.True, "warehouse 01 should be offered");
        Assert.That(options.All(o => !string.IsNullOrWhiteSpace(o.Label)), Is.True, "every option needs a label");
    }

    [Test, Order(11)]
    public async Task Test_11_Available_ListsTheReportForItsOwner() {
        // What the sidebar is built from: one entry per accessible report, name-ordered. A user with no
        // grants gets an empty list and no Reports group at all.
        IReadOnlyList<ReportDefinitionSummary> available = await reports.GetAvailableAsync(userId);

        Assert.That(available.Select(r => r.Slug), Does.Contain(Slug));
        Assert.That(available.Select(r => r.Name), Is.Ordered, "the menu shows these in order");
        Assert.That(available.All(r => !string.IsNullOrWhiteSpace(r.Name)), Is.True,
            "a blank name would render an unclickable-looking menu entry");
    }

    private async Task<ReportDefinition> LoadAsync() {
        ReportDefinition? definition = await reports.GetForExecutionAsync(Slug, userId);
        Assert.That(definition, Is.Not.Null, $"report '{Slug}' not found — run Test_04 first");
        return definition!;
    }
}
