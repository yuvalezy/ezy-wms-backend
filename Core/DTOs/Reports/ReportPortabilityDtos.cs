namespace Core.DTOs.Reports;

/// <summary>
/// A portable set of report definitions — what moves a report authored on one install to another,
/// instead of retyping its SQL.
///
/// <para>
/// The shape deliberately mirrors <c>ConfigExportBundle</c> (<see cref="Core.DTOs.Configuration"/>):
/// <c>FormatVersion</c> + <c>ExportedAtUtc</c> + the payload, exchanged as a plain JSON body with the
/// browser doing the file part. There is no <c>IFormFile</c> anywhere in this codebase and no reason
/// for this to be the first.
/// </para>
///
/// <para>
/// Each entry is a <see cref="CreateReportDefinitionRequest"/> — the authored fields and nothing else.
/// <c>Id</c>, <c>CreatedAt</c> and <c>CreatedByUserId</c> are deliberately absent: they are meaningless
/// on the target install (the id is database-generated, and the author's user Guid would not resolve).
/// Authorization-group grants are absent for the same reason — they are keyed by
/// <c>AuthorizationGroupId</c>, which is install-specific, and grants are managed from the
/// authorization group screen.
/// </para>
/// </summary>
public class ReportExportBundle {
    /// <summary>
    /// Bumped only when the payload shape changes incompatibly, so a future import can refuse a file it
    /// cannot honestly read rather than silently dropping fields.
    /// </summary>
    public int FormatVersion { get; set; } = ReportPortability.FormatVersion;

    public DateTime ExportedAtUtc { get; set; }

    /// <summary>Which install produced this, purely so a stray file can be traced. Never read on import.</summary>
    public string? SourceCompany { get; set; }

    public List<CreateReportDefinitionRequest> Reports { get; set; } = [];
}

/// <summary>Constants shared by the export and import paths.</summary>
public static class ReportPortability {
    /// <summary>The only format this build writes, and the only one it reads.</summary>
    public const int FormatVersion = 1;
}

/// <summary>
/// Body of <c>POST /api/reportDefinition/import</c>. Accepts the bundle's <c>Reports</c> array; the
/// client may post the whole bundle or just the array (the frontend tolerates both file shapes).
/// </summary>
public class ReportImportRequest {
    /// <summary>Zero means nothing to do — the import reports that rather than claiming success.</summary>
    public List<CreateReportDefinitionRequest> Reports { get; set; } = [];

    /// <summary>
    /// Echoed from the file when present. A bundle from a newer format is refused outright.
    /// </summary>
    public int FormatVersion { get; set; } = ReportPortability.FormatVersion;
}

/// <summary>What importing one report would do — or did.</summary>
public enum ReportImportAction {
    /// <summary>No report with this slug exists here; it will be inserted.</summary>
    Create = 0,

    /// <summary>
    /// A report with this slug already exists and will be replaced in place, keeping its id — which is
    /// what makes its authorization-group grants survive a re-import.
    /// </summary>
    Overwrite = 1
}

/// <summary>The verdict on one report in the bundle.</summary>
public class ReportImportItemResult {
    public string Slug { get; set; } = "";

    public string Name { get; set; } = "";

    public ReportImportAction Action { get; set; }

    /// <summary>False → <see cref="Errors"/> says why. Nothing in the bundle is applied while any is false.</summary>
    public bool Valid { get; set; }

    /// <summary>True only after a real (non dry-run) apply that committed.</summary>
    public bool Applied { get; set; }

    /// <summary>
    /// Human-readable, and typically SQL Server's own words — "Invalid column name 'BLength1'" is exactly
    /// what tells an operator this report needs a field their SAP does not have.
    /// </summary>
    public List<string> Errors { get; set; } = [];
}

/// <summary>
/// The result of a dry run or an apply. The dry run is the whole point: it compiles every report against
/// <b>this</b> install's SAP before anything is written.
/// </summary>
public class ReportImportResult {
    /// <summary>True when every report is valid and there is at least one. Gates the Apply button.</summary>
    public bool Success { get; set; }

    public bool DryRun { get; set; }

    /// <summary>Set when the file itself is unusable (wrong format version), rather than a report being bad.</summary>
    public List<string> Errors { get; set; } = [];

    public List<ReportImportItemResult> Reports { get; set; } = [];
}
