using Core.Models;

namespace Core.Interfaces;

/// <summary>
/// Tracks whether the system is ready to operate (external SAP/SBO configured,
/// reachable and schema-ready). The cached snapshot is consulted by the lockdown
/// middleware on every request; <see cref="RefreshAsync"/> re-evaluates readiness.
/// </summary>
public interface ISystemStatusService {
    /// <summary>The last evaluated status (cheap, cached).</summary>
    SystemStatus Current { get; }

    /// <summary>Re-evaluates readiness against the current settings. Never throws.</summary>
    Task<SystemStatus> RefreshAsync(CancellationToken cancellationToken = default);
}
