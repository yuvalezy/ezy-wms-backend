using Adapters.Common.SBO.Services;
using NUnit.Framework;

namespace UnitTests.Unit.Reports;

/// <summary>
/// The reporting connection falls back to <c>ExternalAdapterConnection</c> so reports work on an
/// install that never configures a read-only principal. The blank cases are the ones that matter:
/// the shipped template carries <c>"ReportingConnection": ""</c>, and <c>GetConnectionString</c>
/// returns <c>""</c> — not null — for a key that is present but empty, so a <c>??</c> fallback would
/// silently hand an empty connection string to SqlClient instead of falling back.
/// </summary>
[TestFixture]
public class ReportingConnectionResolutionTests {
    private const string Fallback  = "Server=sap;Database=SBODemoPA;User Id=sa;";
    private const string ReadOnly  = "Server=sap;Database=SBODemoPA;User Id=wms_reports_ro;";

    [Test]
    public void ResolveReportingConnectionString_Configured_UsesReadOnlyPrincipal() {
        Assert.That(SboDatabaseService.ResolveReportingConnectionString(ReadOnly, Fallback), Is.EqualTo(ReadOnly));
    }

    [Test]
    public void ResolveReportingConnectionString_Null_FallsBack() {
        Assert.That(SboDatabaseService.ResolveReportingConnectionString(null, Fallback), Is.EqualTo(Fallback));
    }

    /// <summary>The state a customer lands in after copying appsettings.example.json and not filling it in.</summary>
    [Test]
    public void ResolveReportingConnectionString_Empty_FallsBack() {
        Assert.That(SboDatabaseService.ResolveReportingConnectionString(string.Empty, Fallback), Is.EqualTo(Fallback));
    }

    [Test]
    public void ResolveReportingConnectionString_Whitespace_FallsBack() {
        Assert.That(SboDatabaseService.ResolveReportingConnectionString("   ", Fallback), Is.EqualTo(Fallback));
    }
}
