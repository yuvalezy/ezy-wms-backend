using Core.DTOs.Reports;
using Core.Entities;
using Core.Interfaces;
using Infrastructure.DbContexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using UnitTests.Integration.ExternalSystems;

namespace UnitTests.Integration.Reports;

/// <summary>
/// Export and import of report definitions — how a report authored here reaches a customer install
/// without its SQL being retyped.
/// </summary>
/// <remarks>
/// Runs against the real databases because the interesting part is not the JSON: importing compiles
/// every report's SQL against <b>this</b> install's SAP, which is exactly what tells an operator that a
/// report needs a column their SAP does not have.
/// <para>
/// Depends on the reports the other fixtures create. It restores whatever it disturbs.
/// </para>
/// </remarks>
[TestFixture]
public class ReportPortabilityTest : BaseExternalTest {
    private const string Slug = "stock-on-hand";

    private IServiceScope  scope   = null!;
    private IReportService reports = null!;
    private Guid           userId;

    /// <summary>Resolved per use rather than held: the scope owns its lifetime and disposes it in TearDown.</summary>
    private SystemDbContext Db => scope.ServiceProvider.GetRequiredService<SystemDbContext>();

    [SetUp]
    public void SetUp() {
        scope   = factory.Services.CreateScope();
        reports = scope.ServiceProvider.GetRequiredService<IReportService>();

        User? user = Db.Users.FirstOrDefault(u => u.SuperUser && !u.Deleted) ?? Db.Users.FirstOrDefault(u => !u.Deleted);
        Assert.That(user, Is.Not.Null, "no user in the WMS database to own the report");
        userId = user!.Id;
    }

    [TearDown]
    public void TearDown() => scope.Dispose();

    private async Task<CreateReportDefinitionRequest> ExportOneAsync(string slug) {
        ReportExportBundle bundle = await reports.ExportAsync(null);
        CreateReportDefinitionRequest? report = bundle.Reports.FirstOrDefault(r => r.Slug == slug);
        Assert.That(report, Is.Not.Null, $"'{slug}' is not in the export — run the other report fixtures first");
        return report!;
    }

    [Test]
    public async Task Export_CarriesTheAuthoredFieldsAndNothingInstallSpecific() {
        ReportExportBundle bundle = await reports.ExportAsync(null);

        Assert.That(bundle.FormatVersion, Is.EqualTo(ReportPortability.FormatVersion));
        Assert.That(bundle.ExportedAtUtc, Is.Not.EqualTo(default(DateTime)));
        Assert.That(bundle.Reports, Is.Not.Empty);

        CreateReportDefinitionRequest report = await ExportOneAsync(Slug);
        Assert.Multiple(() => {
            Assert.That(report.Sql, Does.Contain("{{orderBy}}"), "the SQL must travel, or the file is useless");
            Assert.That(report.Columns, Is.Not.Empty);
            Assert.That(report.Variables, Is.Not.Empty);
            // The export type IS the import type, so there is no id/createdAt to carry by construction —
            // this asserts the property that matters instead: a link config survives the trip.
            Assert.That(report.Columns.Any(c => c.LinkTemplate != null), Is.True, "column links did not export");
        });
    }

    [Test]
    public async Task Export_SingleReport_ReturnsOnlyThatOne() {
        IReadOnlyList<ReportDefinitionSummary> all = await reports.GetAllAsync();
        ReportDefinitionSummary target = all.First(r => r.Slug == Slug);

        ReportExportBundle bundle = await reports.ExportAsync(target.Id);

        Assert.That(bundle.Reports, Has.Count.EqualTo(1));
        Assert.That(bundle.Reports[0].Slug, Is.EqualTo(Slug));
    }

    [Test]
    public async Task Import_DryRun_ReportsOverwriteForAnExistingSlugAndWritesNothing() {
        CreateReportDefinitionRequest report = await ExportOneAsync(Slug);
        string originalName = report.Name;
        report.Name = "Renamed by a dry run";

        ReportImportResult result = await reports.ImportAsync(
            new ReportImportRequest { Reports = [report] }, dryRun: true, userId);

        Assert.Multiple(() => {
            Assert.That(result.Success, Is.True);
            Assert.That(result.DryRun, Is.True);
            Assert.That(result.Reports, Has.Count.EqualTo(1));
            Assert.That(result.Reports[0].Action, Is.EqualTo(ReportImportAction.Overwrite));
            Assert.That(result.Reports[0].Valid, Is.True);
            Assert.That(result.Reports[0].Applied, Is.False, "a dry run must never apply");
        });

        ReportDefinitionDetail? live = await reports.GetBySlugAsync(Slug, userId);
        Assert.That(live!.Name, Is.EqualTo(originalName), "the dry run wrote to the database");
    }

    [Test]
    public async Task Import_RoundTripsWithoutLosingAnything() {
        CreateReportDefinitionRequest before = await ExportOneAsync(Slug);

        ReportImportResult result = await reports.ImportAsync(
            new ReportImportRequest { Reports = [before] }, dryRun: false, userId);
        Assert.That(result.Success, Is.True, string.Join("; ", result.Reports.SelectMany(r => r.Errors)));
        Assert.That(result.Reports[0].Applied, Is.True);

        CreateReportDefinitionRequest after = await ExportOneAsync(Slug);

        Assert.Multiple(() => {
            Assert.That(after.Name, Is.EqualTo(before.Name));
            Assert.That(after.Sql, Is.EqualTo(before.Sql));
            Assert.That(after.DefaultSortColumnKey, Is.EqualTo(before.DefaultSortColumnKey));
            Assert.That(after.TimeoutSeconds, Is.EqualTo(before.TimeoutSeconds));
            Assert.That(after.Columns.Select(c => c.Key), Is.EqualTo(before.Columns.Select(c => c.Key)));
            Assert.That(after.Columns.Select(c => c.LinkTemplate), Is.EqualTo(before.Columns.Select(c => c.LinkTemplate)));
            Assert.That(after.Columns.Select(c => c.Sortable), Is.EqualTo(before.Columns.Select(c => c.Sortable)));
            Assert.That(after.Variables.Select(v => v.Name), Is.EqualTo(before.Variables.Select(v => v.Name)));
            Assert.That(after.Variables.Select(v => v.Type), Is.EqualTo(before.Variables.Select(v => v.Type)));
            Assert.That(after.Variables.Select(v => v.LookupSql), Is.EqualTo(before.Variables.Select(v => v.LookupSql)));
        });
    }

    /// <summary>
    /// The reason Overwrite replaces in place instead of delete-then-create: a re-import must not silently
    /// revoke every group's access to the report.
    /// </summary>
    [Test]
    public async Task Import_Overwrite_KeepsTheIdSoGrantsSurvive() {
        ReportDefinitionSummary before = (await reports.GetAllAsync()).First(r => r.Slug == Slug);
        CreateReportDefinitionRequest report = await ExportOneAsync(Slug);

        ReportImportResult result = await reports.ImportAsync(
            new ReportImportRequest { Reports = [report] }, dryRun: false, userId);
        Assert.That(result.Success, Is.True);

        ReportDefinitionSummary after = (await reports.GetAllAsync()).First(r => r.Slug == Slug);
        Assert.That(after.Id, Is.EqualTo(before.Id),
            "the id changed — every AuthorizationGroupReport row for this report was just orphaned");
    }

    [Test]
    public async Task Import_Overwrite_ReplacesColumnsWholesaleRatherThanAccumulating() {
        CreateReportDefinitionRequest report = await ExportOneAsync(Slug);
        int columnCount = report.Columns.Count;

        await reports.ImportAsync(new ReportImportRequest { Reports = [report] }, dryRun: false, userId);
        await reports.ImportAsync(new ReportImportRequest { Reports = [report] }, dryRun: false, userId);

        CreateReportDefinitionRequest after = await ExportOneAsync(Slug);
        Assert.That(after.Columns, Has.Count.EqualTo(columnCount), "importing twice duplicated the columns");
    }

    [Test]
    public async Task Import_InvalidSql_IsRejectedWithSqlServersOwnWords() {
        CreateReportDefinitionRequest report = await ExportOneAsync(Slug);
        report.Slug = "import-broken-sql";
        report.Name = "Broken";
        // Exactly the customer case: a report whose SAP lacks a column the report needs.
        report.Sql = report.Sql.Replace("T0.\"OnHand\"", "T0.\"NoSuchColumnHere\"");

        ReportImportResult result = await reports.ImportAsync(
            new ReportImportRequest { Reports = [report] }, dryRun: true, userId);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Reports[0].Valid, Is.False);
        Assert.That(result.Reports[0].Action, Is.EqualTo(ReportImportAction.Create));
        Assert.That(string.Join(" ", result.Reports[0].Errors), Does.Contain("NoSuchColumnHere"),
            "the error must name the offending column — that is what tells an operator what their SAP lacks");
    }

    /// <summary>All-or-nothing: the valid report in a bundle must not land when another is broken.</summary>
    [Test]
    public async Task Import_OneBadReport_AppliesNothing() {
        CreateReportDefinitionRequest good = await ExportOneAsync(Slug);
        good.Slug = "import-atomic-good";
        good.Name = "Would have been created";

        CreateReportDefinitionRequest bad = await ExportOneAsync(Slug);
        bad.Slug = "import-atomic-bad";
        bad.Name = "Broken";
        bad.Sql  = bad.Sql.Replace("FROM OITW T0", "FROM NoSuchTableAtAll T0");

        ReportImportResult result = await reports.ImportAsync(
            new ReportImportRequest { Reports = [good, bad] }, dryRun: false, userId);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Reports.Single(r => r.Slug == "import-atomic-good").Valid, Is.True);
        Assert.That(result.Reports.Single(r => r.Slug == "import-atomic-bad").Valid, Is.False);
        Assert.That(result.Reports.All(r => !r.Applied), Is.True);

        bool goodExists = await Db.ReportDefinitions.AnyAsync(r => r.Slug == "import-atomic-good");
        Assert.That(goodExists, Is.False, "the valid report was written despite the bundle failing");
    }

    [Test]
    public async Task Import_DuplicateSlugsInOneFile_AreRejected() {
        CreateReportDefinitionRequest first = await ExportOneAsync(Slug);
        CreateReportDefinitionRequest second = await ExportOneAsync(Slug);

        ReportImportResult result = await reports.ImportAsync(
            new ReportImportRequest { Reports = [first, second] }, dryRun: true, userId);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Reports.All(r => r.Errors.Any(e => e.Contains("more than one report"))), Is.True);
    }

    [Test]
    public async Task Import_NewerFormatVersion_IsRefusedRatherThanReadOptimistically() {
        CreateReportDefinitionRequest report = await ExportOneAsync(Slug);

        ReportImportResult result = await reports.ImportAsync(
            new ReportImportRequest { Reports = [report], FormatVersion = ReportPortability.FormatVersion + 1 },
            dryRun: true, userId);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Errors, Is.Not.Empty);
        Assert.That(result.Reports, Is.Empty, "a file this build cannot read must not be reported per-report");
    }

    [Test]
    public async Task Import_EmptyFile_SaysSoRatherThanClaimingSuccess() {
        ReportImportResult result = await reports.ImportAsync(
            new ReportImportRequest { Reports = [] }, dryRun: true, userId);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Errors, Is.Not.Empty);
    }

    [Test]
    public async Task Import_CreatesAReportThatDoesNotExistYet() {
        const string newSlug = "import-created-report";
        foreach (ReportDefinitionSummary existing in (await reports.GetAllAsync()).Where(r => r.Slug == newSlug))
            await reports.DeleteAsync(existing.Id);

        CreateReportDefinitionRequest report = await ExportOneAsync(Slug);
        report.Slug = newSlug;
        report.Name = "Imported Report";

        ReportImportResult result = await reports.ImportAsync(
            new ReportImportRequest { Reports = [report] }, dryRun: false, userId);

        Assert.That(result.Success, Is.True, string.Join("; ", result.Reports.SelectMany(r => r.Errors)));
        Assert.That(result.Reports[0].Action, Is.EqualTo(ReportImportAction.Create));
        Assert.That(result.Reports[0].Applied, Is.True);

        ReportDefinitionDetail? created = await reports.GetBySlugAsync(newSlug, userId);
        Assert.That(created, Is.Not.Null);
        Assert.That(created!.Columns, Has.Count.EqualTo(report.Columns.Count));

        // Imported reports are attributed to whoever imported them — the source install's user Guid would
        // not resolve here, which is exactly why authorship is not in the file.
        ReportDefinition entity = await Db.ReportDefinitions.AsNoTracking().FirstAsync(r => r.Slug == newSlug);
        Assert.That(entity.CreatedByUserId, Is.EqualTo(userId));

        await reports.DeleteAsync(created.Id);
    }
}
