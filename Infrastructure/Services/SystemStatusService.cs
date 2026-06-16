using System.Reflection;
using Core.Interfaces;
using Core.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services;

/// <summary>
/// Evaluates and caches system readiness. "Ready" means SBO settings are present
/// and non-placeholder, SAP is reachable, and the required <c>U_WMS_READY</c> UDF
/// exists. Failures are captured (never thrown) so the service can boot and be
/// recovered from the configuration UI.
/// </summary>
public sealed class SystemStatusService(IServiceScopeFactory scopeFactory, ILogger<SystemStatusService> logger)
    : ISystemStatusService {

    private static readonly string AppVersion =
        Assembly.GetEntryAssembly()?.GetName().Version?.ToString() ?? "unknown";

    private volatile SystemStatus current = new() {
        Ready = false, SboConfigured = false, Detail = "Not checked yet.", Version = AppVersion
    };

    public SystemStatus Current => current;

    public async Task<SystemStatus> RefreshAsync(CancellationToken cancellationToken = default) {
        using var scope = scopeFactory.CreateScope();
        var settings = scope.ServiceProvider.GetRequiredService<ISettings>();
        var sbo = settings.SboSettings;

        bool configured = sbo is not null
            && Present(sbo.ServiceLayerUrl)
            && Present(sbo.Database)
            && Present(sbo.User)
            && Present(sbo.Password);

        if (!configured) {
            return Set(ready: false, configured: false,
                "SAP Business One is not configured. Set the SBO Settings to continue.");
        }

        try {
            var adapter = scope.ServiceProvider.GetRequiredService<IExternalSystemAdapter>();
            bool udfOk = await adapter.ValidateUserDefinedFieldAsync("OPKL", "WMS_READY");
            if (!udfOk) {
                return Set(ready: false, configured: true,
                    "Connected to SAP, but the required user-defined field 'U_WMS_READY' is missing on table 'OPKL'. " +
                    "Create the UDF in SAP Business One (Tools → Customization Tools → User-Defined Fields - Management) and re-check.");
            }
            return Set(ready: true, configured: true, "System ready.");
        }
        catch (Exception ex) {
            logger.LogWarning(ex, "System readiness check failed");
            return Set(ready: false, configured: true, $"SAP connection failed: {ex.Message}");
        }
    }

    private SystemStatus Set(bool ready, bool configured, string detail) {
        current = new SystemStatus {
            Ready = ready, SboConfigured = configured, Detail = detail,
            CheckedAtUtc = DateTime.UtcNow, Version = AppVersion
        };
        return current;
    }

    /// <summary>Non-empty and not a seed-template placeholder (e.g. "YOUR_SAP_SERVER").</summary>
    private static bool Present(string? value) =>
        !string.IsNullOrWhiteSpace(value) && !value.Contains("YOUR_", StringComparison.OrdinalIgnoreCase);
}
