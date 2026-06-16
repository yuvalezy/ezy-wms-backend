using Core.Models;
using Core.Models.Settings;

namespace Core.Interfaces;

/// <summary>
/// Performs a standalone SAP Business One Service Layer login against the supplied
/// settings (without touching the shared connection), for the "test connection"
/// admin action.
/// </summary>
public interface ISboConnectionTester {
    Task<SboConnectionResult> TestServiceLayerLoginAsync(SboSettings? settings, CancellationToken cancellationToken = default);
}
