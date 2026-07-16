namespace Core.DTOs.Reports;

/// <summary>
/// Outcome of validating a report definition and discovering its columns — what the Test button shows, and
/// what every save runs first.
/// </summary>
/// <remarks>
/// Validation compiles <b>both</b> shapes (page and count) plus every lookup query inside the always-rollback
/// scope, so SQL Server fully parses and binds the real runtime SQL and returns its actual error. Skipping the
/// count shape is the trap: a <c>Recompile</c> report would save clean and blow up on the count request.
/// </remarks>
public class ReportValidationResult {
    /// <summary>
    /// True only when <see cref="Errors"/> is empty. Warnings do not block a save.
    /// </summary>
    public bool Valid { get; set; }

    /// <summary>
    /// Blocking problems — including SQL Server's verbatim message (e.g. "Must declare the scalar variable '@X'"),
    /// which is far more useful to the author than anything paraphrased.
    /// </summary>
    public List<string> Errors { get; set; } = [];

    /// <summary>
    /// Non-blocking notes, e.g. a variable declared but never referenced by the SQL (harmless under
    /// <c>sp_executesql</c>, so warn rather than reject).
    /// </summary>
    public List<string> Warnings { get; set; } = [];

    /// <summary>
    /// The columns the query actually produced, discovered from the reader's schema without ever reading a row.
    /// Empty when <see cref="Valid"/> is false.
    /// </summary>
    public List<ReportColumnDescriptor> Columns { get; set; } = [];
}
