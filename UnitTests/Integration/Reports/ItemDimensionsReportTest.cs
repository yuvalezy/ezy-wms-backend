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
/// Creates and exercises the <c>item-dimensions</c> report: each item's length, width, height, volume
/// and weight shown both as SAP holds them today and as they would read if the unit were re-based
/// through <c>PurPackUn</c> — the working sheet for deciding whether a dimension is recorded per piece
/// or per box.
/// </summary>
/// <remarks>
/// Idempotent (deletes by slug first) and leaves the report behind on purpose — a passing run means a
/// usable report exists in the UI.
/// </remarks>
[TestFixture]
public class ItemDimensionsReportTest : BaseExternalTest {
    private const string Slug = "item-dimensions";

    /// <summary>
    /// Two deliberate departures from the query as handed over:
    /// <list type="number">
    /// <item>
    /// <b>The width block's aliases were the length block's</b> (<c>BWidth1 AS [LargoActualPieza]</c>, and
    /// so on for all four) — a copy-paste. Output column names must be unique: they key the row
    /// dictionary, they are what <c>ORDER BY</c> resolves against, and a duplicate makes the count
    /// shape's derived table fail with Msg 8156. Renamed to <c>Ancho*</c>.
    /// </item>
    /// <item>
    /// <b><c>Format(x, 'N6')</c> is gone.</b> It returns <c>nvarchar</c>, which would sort
    /// lexicographically — putting "10" before "9" in a numeric column — and export to Excel as text
    /// that will not sum. The value stays a real number and the column declares
    /// <c>Format = Number, Decimals = 6</c> instead, so the client renders exactly the same digits
    /// while sorting and exporting honestly.
    /// </item>
    /// </list>
    /// <para>
    /// <c>NULLIF(PurPackUn, 0)</c> guards the division. The WHERE clause already implies
    /// <c>PurPackUn &gt; 0</c> (it must be, for <c>BVolume * PurPackUn &gt; 1</c> to hold), so this
    /// changes nothing today — but a divide-by-zero is a hard SQL error that fails the whole report,
    /// and this makes it a NULL cell (an em dash) if the filter is ever loosened.
    /// </para>
    /// <para>
    /// <c>RowKey</c> is the mandatory hidden tiebreaker. <c>ItemCode</c> is already unique, but it is a
    /// sortable display column here — so if <c>{{orderBy}}</c> expanded to it, the ORDER BY would name
    /// it twice and SQL Server would reject the statement (Msg 169). A separate non-sortable copy is
    /// what keeps paging deterministic over the many ties these dimension columns have.
    /// </para>
    /// </remarks>
    private const string Sql = """
        SELECT
            T0."ItemCode",
            -- largo
            T0."BLength1" AS "LargoActualPieza",
            T0."BLength1" * T0."PurPackUn" AS "LargoActualCaja",
            T0."BLength1" / NULLIF(T0."PurPackUn", 0) AS "LargoNuevoPieza",
            T0."BLength1" AS "LargoNuevoCaja",
            -- ancho
            T0."BWidth1" AS "AnchoActualPieza",
            T0."BWidth1" * T0."PurPackUn" AS "AnchoActualCaja",
            T0."BWidth1" / NULLIF(T0."PurPackUn", 0) AS "AnchoNuevoPieza",
            T0."BWidth1" AS "AnchoNuevoCaja",
            -- alto
            T0."BHeight1" AS "AltoActualPieza",
            T0."BHeight1" * T0."PurPackUn" AS "AltoActualCaja",
            T0."BHeight1" / NULLIF(T0."PurPackUn", 0) AS "AltoNuevoPieza",
            T0."BHeight1" AS "AltoNuevoCaja",
            -- volumen
            T0."BVolume" AS "VolumenActualPieza",
            T0."BVolume" * T0."PurPackUn" AS "VolumenActualCaja",
            T0."BVolume" / NULLIF(T0."PurPackUn", 0) AS "VolumenNuevoPieza",
            T0."BVolume" AS "VolumenNuevoCaja",
            -- peso
            T0."BWeight1" AS "PesoActualPieza",
            T0."BWeight1" * T0."PurPackUn" AS "PesoActualCaja",
            T0."BWeight1" / NULLIF(T0."PurPackUn", 0) AS "PesoNuevoPieza",
            T0."BWeight1" AS "PesoNuevoCaja",
            T0."ItemCode" AS "RowKey"
        FROM OITM T0
        WHERE T0."BVolume" * T0."PurPackUn" > 1
          AND T0."BLength1" IS NOT NULL
          AND T0."OnHand" > 0
        ORDER BY {{orderBy}}, "RowKey"
        {{paging}}
        """;

    /// <summary>Every measure, in the order the report shows them. Drives both the columns and the sort test.</summary>
    private static readonly (string Prefix, string Label)[] Measures = [
        ("Largo",   "Largo"),
        ("Ancho",   "Ancho"),
        ("Alto",    "Alto"),
        ("Volumen", "Volumen"),
        ("Peso",    "Peso")
    ];

    private static readonly (string Suffix, string Label)[] Variants = [
        ("ActualPieza", "Actual Pieza"),
        ("ActualCaja",  "Actual Caja"),
        ("NuevoPieza",  "Nuevo Pieza"),
        ("NuevoCaja",   "Nuevo Caja")
    ];

    /// <summary>The 20 measure columns plus the item code — everything a user may sort by.</summary>
    private static IEnumerable<string> SortableKeys =>
        ["ItemCode", .. Measures.SelectMany(m => Variants.Select(v => $"{m.Prefix}{v.Suffix}"))];

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

    private static CreateReportDefinitionRequest BuildRequest() {
        var columns = new List<ReportColumnRequest> {
            // Same link as the other reports: the code opens the item's own screen.
            new() {
                Key = "ItemCode", Label = "Código", Format = ReportColumnFormat.Text, Order = 0,
                LinkType = ReportColumnLinkType.Internal, LinkTemplate = "/itemCheck/{ItemCode}"
            }
        };

        var order = 1;
        foreach ((string prefix, string measureLabel) in Measures) {
            foreach ((string suffix, string variantLabel) in Variants) {
                columns.Add(new ReportColumnRequest {
                    Key    = $"{prefix}{suffix}",
                    Label  = $"{measureLabel} {variantLabel}",
                    Format = ReportColumnFormat.Number,
                    // 6, matching the Format(x,'N6') this replaces — the digits are identical, but the
                    // value stays numeric, so it sorts and exports as a number.
                    Decimals = 6,
                    Order    = order++
                });
            }
        }

        columns.Add(new ReportColumnRequest {
            Key = "RowKey", Label = "RowKey", Format = ReportColumnFormat.Text,
            Visible = false, Sortable = false, Order = order
        });

        return new CreateReportDefinitionRequest {
            Name        = "Dimensiones por Pieza y Caja",
            Slug        = Slug,
            Description = "Largo, ancho, alto, volumen y peso de cada artículo, como están hoy y como quedarían al "
                        + "reconvertir la unidad por PurPackUn. Solo artículos con stock y volumen de caja mayor a 1.",
            Sql         = Sql,
            Enabled     = true,
            DefaultSortColumnKey = "ItemCode",
            DefaultSortDirection = ReportSortDirection.Asc,
            DefaultPageSize      = 50,
            TimeoutSeconds       = 60,
            Columns              = columns,
            Variables            = []
        };
    }

    [Test, Order(1)]
    public async Task Test_01_Save_ValidatesAndDiscoversEveryColumn() {
        ReportValidationResult result = await reports.TestAsync(BuildRequest());

        Assert.That(result.Errors, Is.Empty);
        Assert.That(result.Valid, Is.True);
        // The duplicate-alias trap: had the width block kept the length block's names, discovery would
        // report fewer columns than declared rather than failing loudly here.
        Assert.That(result.Columns.Select(c => c.Key), Is.SupersetOf(SortableKeys));
    }

    [Test, Order(2)]
    public async Task Test_02_Create_PersistsTheReport() {
        foreach (ReportDefinitionSummary existing in (await reports.GetAllAsync()).Where(r => r.Slug == Slug))
            await reports.DeleteAsync(existing.Id);

        ReportDefinitionDetail created = await reports.CreateAsync(BuildRequest(), userId);

        Assert.Multiple(() => {
            Assert.That(created.Slug, Is.EqualTo(Slug));
            // 1 code + 5 measures x 4 variants + RowKey.
            Assert.That(created.Columns, Has.Count.EqualTo(22));
            Assert.That(created.Variables, Is.Empty, "this report takes no filters");
        });
    }

    [Test, Order(3)]
    public async Task Test_03_Run_ReturnsRows() {
        ReportDefinition definition = await LoadAsync();
        ReportPageResult page = await executor.ExecutePageAsync(definition, new ReportExecutionRequest { Limit = 10 });

        Assert.That(page.Rows, Is.Not.Empty,
            "no item matches BVolume * PurPackUn > 1 with stock — cannot prove the report works");
        Assert.That(page.Rows[0]["ItemCode"], Is.Not.Null);
    }

    /// <summary>
    /// The reason <c>Format(x,'N6')</c> was dropped: these must arrive as numbers. A string column would
    /// sort "10" before "9" and export to Excel as text that will not sum.
    /// </summary>
    [Test, Order(4)]
    public async Task Test_04_Run_ReturnsMeasuresAsNumbersNotFormattedStrings() {
        ReportDefinition definition = await LoadAsync();
        ReportPageResult page = await executor.ExecutePageAsync(definition, new ReportExecutionRequest { Limit = 5 });

        Assert.That(page.Rows, Is.Not.Empty);
        foreach (string key in SortableKeys.Where(k => k != "ItemCode")) {
            object? value = page.Rows[0][key];
            Assert.That(value, Is.Not.InstanceOf<string>(), $"'{key}' came back as text, not a number");
        }
    }

    /// <summary>
    /// The derived columns must actually derive: Caja is the piece value scaled up by PurPackUn, and
    /// NuevoPieza scaled down. A column silently returning the raw value would look plausible and be wrong.
    /// </summary>
    [Test, Order(5)]
    public async Task Test_05_Run_DerivedColumnsScaleByPurPackUn() {
        ReportDefinition definition = await LoadAsync();
        ReportPageResult page = await executor.ExecutePageAsync(definition, new ReportExecutionRequest { Limit = 20 });

        Assert.That(page.Rows, Is.Not.Empty);
        foreach (Dictionary<string, object?> row in page.Rows) {
            foreach ((string prefix, _) in Measures) {
                double piece = Convert.ToDouble(row[$"{prefix}ActualPieza"]);
                double box   = Convert.ToDouble(row[$"{prefix}ActualCaja"]);
                if (piece == 0) {
                    continue;
                }
                // The ratio between them IS PurPackUn, whatever it is for this item — so the two derived
                // columns must be reciprocal about the piece value.
                double newPiece = Convert.ToDouble(row[$"{prefix}NuevoPieza"]);
                Assert.That(box * newPiece, Is.EqualTo(piece * piece).Within(0.01).Percent,
                    $"{prefix}: Caja and NuevoPieza are not scaled by the same PurPackUn");
            }
        }
    }

    [Test, Order(6)]
    public async Task Test_06_Run_SortsByEverySortableColumn() {
        ReportDefinition definition = await LoadAsync();

        foreach (string key in SortableKeys) {
            ReportPageResult page = await executor.ExecutePageAsync(definition, new ReportExecutionRequest {
                Limit = 5, SortColumnKey = key, SortDirection = ReportSortDirection.Desc
            });
            Assert.That(page.SortColumnKey, Is.EqualTo(key), $"sorting by '{key}' did not apply");
        }
    }

    [Test, Order(7)]
    public async Task Test_07_Run_PagesWithoutRepeatingRows() {
        ReportDefinition definition = await LoadAsync();

        // Sorted by a column with many ties, which is exactly where a missing tiebreaker shows up.
        ReportPageResult first = await executor.ExecutePageAsync(definition, new ReportExecutionRequest {
            Limit = 5, Offset = 0, SortColumnKey = "VolumenActualPieza"
        });
        ReportPageResult second = await executor.ExecutePageAsync(definition, new ReportExecutionRequest {
            Limit = 5, Offset = 5, SortColumnKey = "VolumenActualPieza"
        });

        var firstKeys  = first.Rows.Select(r => (string)r["RowKey"]!).ToList();
        var secondKeys = second.Rows.Select(r => (string)r["RowKey"]!).ToList();
        Assert.That(firstKeys.Intersect(secondKeys), Is.Empty,
            "pages overlap — the tiebreaker is not making the sort deterministic");
    }

    [Test, Order(8)]
    public async Task Test_08_Available_ListsTheReportForItsOwner() {
        IReadOnlyList<ReportDefinitionSummary> available = await reports.GetAvailableAsync(userId);
        Assert.That(available.Select(r => r.Slug), Does.Contain(Slug));
    }

    private async Task<ReportDefinition> LoadAsync() {
        ReportDefinition? definition = await reports.GetForExecutionAsync(Slug, userId);
        Assert.That(definition, Is.Not.Null, $"report '{Slug}' not found — run Test_02 first");
        return definition!;
    }
}
