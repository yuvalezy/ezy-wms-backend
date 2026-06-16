namespace Core.Models;

/// <summary>
/// Snapshot of overall system readiness. Drives the lockdown gate: when the
/// external system (SAP/SBO) is not configured or not reachable the service still
/// starts, business endpoints return 503, and the UI locks until configured.
/// </summary>
public sealed class SystemStatus {
    /// <summary>True when SAP/SBO is configured, reachable, and schema-ready.</summary>
    public bool Ready { get; init; }

    /// <summary>True when SBO settings are present and non-placeholder (credentials may still be wrong).</summary>
    public bool SboConfigured { get; init; }

    /// <summary>Human-readable explanation of the current state.</summary>
    public string? Detail { get; init; }

    public DateTime? CheckedAtUtc { get; init; }

    public string? Version { get; init; }
}
