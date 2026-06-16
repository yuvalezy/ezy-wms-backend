using System.Text.Json.Nodes;
using Core.DTOs.Configuration;
using Core.Enums;
using Core.Models.Settings;

namespace Core.Services;

/// <summary>
/// Read/write access to the database-backed configuration sections for the admin
/// API: list, read (masked), validate, update, history, restore, export, import,
/// and migration status. Updates trigger a configuration reload.
/// </summary>
public interface IConfigurationManagementService {
    Task<List<ConfigSectionSummary>> ListAsync();

    Task<ConfigSectionDetail?> GetAsync(string section);

    ConfigValidationResult Validate(string section, JsonNode? json);

    /// <summary>Validates, merges masked secrets, encrypts, upserts, audits, and reloads.</summary>
    Task<ConfigSectionDetail> UpdateAsync(
        string section, JsonNode? json, int? expectedVersion, ConfigChangeType changeType, Guid? userId);

    Task<List<ConfigAuditEntryDto>> HistoryAsync(string section);

    Task<ConfigSectionDetail> RestoreAsync(string section, int version, Guid? userId);

    /// <summary>Read-only, secrets-scrubbed export of one section (or all when null).</summary>
    Task<ConfigExportBundle> ExportAsync(string? section);

    Task<ConfigImportResult> ImportAsync(ConfigImportRequest request, bool dryRun, Guid? userId);

    Task<ConfigMigrationStatusDto?> GetMigrationStatusAsync();

    /// <summary>
    /// Resolves a draft "SboSettings" payload (merging masked secrets with the
    /// stored values and decrypting) into a bound <see cref="SboSettings"/> for a
    /// connection test. Does not persist anything.
    /// </summary>
    Task<SboSettings?> ResolveSboSettingsDraftAsync(JsonNode? draft);
}

/// <summary>Thrown for configuration operations that map to a specific HTTP status.</summary>
public sealed class ConfigOperationException(int statusCode, string message, List<string>? errors = null)
    : Exception(message) {
    public int          StatusCode { get; } = statusCode;
    public List<string> Errors     { get; } = errors ?? [];
}
