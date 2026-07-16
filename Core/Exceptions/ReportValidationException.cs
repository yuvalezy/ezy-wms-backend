namespace Core.Exceptions;

/// <summary>
/// Thrown when a report definition or an execution request is rejected: a bad variable name, a duplicate,
/// a missing/duplicated placeholder token, an unknown or non-sortable sort key, an out-of-range clamp, a
/// value that failed strict parsing, or a static-select value that isn't one of the declared options.
/// </summary>
/// <remarks>
/// Carries the full list so a save surfaces every problem at once rather than one per round trip.
/// Maps to a 400.
/// </remarks>
public class ReportValidationException(IReadOnlyList<string> errors)
    : Exception(string.Join("; ", errors)) {
    public IReadOnlyList<string> Errors { get; } = errors;

    public ReportValidationException(string error) : this([error]) { }
}
