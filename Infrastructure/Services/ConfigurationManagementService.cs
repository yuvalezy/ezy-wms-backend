using System.Text;
using System.Text.Json.Nodes;
using Core.Configuration;
using Core.DTOs.Configuration;
using Core.Entities;
using Core.Enums;
using Core.Models.Settings;
using Core.Services;
using Infrastructure.Configuration;
using Infrastructure.DbContexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services;

public sealed class ConfigurationManagementService : IConfigurationManagementService {
    private readonly SystemDbContext db;
    private readonly IConfiguration  configuration;
    private readonly ILogger<ConfigurationManagementService> logger;
    private readonly ConfigSecretProtector protector;
    private readonly HashSet<string> assemblyAllowlist;

    public ConfigurationManagementService(
        SystemDbContext db, IConfiguration configuration, ILogger<ConfigurationManagementService> logger) {
        this.db            = db;
        this.configuration = configuration;
        this.logger        = logger;
        protector          = new ConfigSecretProtector(configuration["Licensing:EncryptionKey"]);
        assemblyAllowlist  = new HashSet<string>(
            configuration.GetSection("Configuration:AssemblyAllowlist").Get<string[]>() ?? [],
            StringComparer.OrdinalIgnoreCase);
    }

    public async Task<List<ConfigSectionSummary>> ListAsync() {
        var rows = await db.SystemConfiguration.AsNoTracking().OrderBy(r => r.Section).ToListAsync();
        return rows.Select(ToSummary).ToList();
    }

    public async Task<ConfigSectionDetail?> GetAsync(string section) {
        var row = await db.SystemConfiguration.AsNoTracking().FirstOrDefaultAsync(r => r.Section == section);
        if (row is null) {
            return null;
        }

        var detail = new ConfigSectionDetail();
        CopySummary(ToSummary(row), detail);
        detail.Json = JsonNode.Parse(ConfigSecretJson.Mask(row.Json));
        return detail;
    }

    public ConfigValidationResult Validate(string section, JsonNode? json) {
        var result = new ConfigValidationResult { Valid = true };
        if (json is null) {
            return result;
        }

        string jsonString = json.ToJsonString();

        // Section-specific structural validation (reuse existing rules where present).
        if (section.Equals("Item", StringComparison.OrdinalIgnoreCase)) {
            try {
                var item = Bind<MetaDataDefinitions>("Item", jsonString);
                var errors = item?.ValidateMetadataDefinitions().ToList() ?? [];
                if (errors.Count > 0) {
                    result.Valid = false;
                    result.Errors.AddRange(errors);
                }
            }
            catch (Exception ex) {
                result.Valid = false;
                result.Errors.Add($"Could not parse Item metadata: {ex.Message}");
            }
        }

        // Hardening: restricted (code-loading) assembly/type pairs must be allowlisted.
        foreach (var (assembly, type) in ConfigSecretJson.CollectAssemblyTypePairs(jsonString)) {
            if (!IsAssemblyAllowed(assembly, type)) {
                result.Valid = false;
                result.Errors.Add($"Assembly/type not allowlisted: '{assembly}' / '{type}'.");
            }
        }

        return result;
    }

    public async Task<ConfigSectionDetail> UpdateAsync(
        string section, JsonNode? json, int? expectedVersion, ConfigChangeType changeType, Guid? userId) {
        EnsureSectionWritable(section);

        var validation = Validate(section, json);
        if (!validation.Valid) {
            throw new ConfigOperationException(400, $"Validation failed for '{section}'.", validation.Errors);
        }

        var row = await db.SystemConfiguration.FirstOrDefaultAsync(r => r.Section == section);
        if (expectedVersion.HasValue && row is not null && row.Version != expectedVersion.Value) {
            throw new ConfigOperationException(409,
                $"Section '{section}' was modified by someone else (expected v{expectedVersion}, current v{row.Version}). Reload and try again.");
        }

        await ApplyAsync(section, json, changeType, userId, row);
        await db.SaveChangesAsync();
        Reload();
        return (await GetAsync(section))!;
    }

    public async Task<List<ConfigAuditEntryDto>> HistoryAsync(string section) {
        return await db.SystemConfigurationAudits.AsNoTracking()
            .Where(a => a.Section == section)
            .OrderByDescending(a => a.ChangedAtUtc)
            .Select(a => new ConfigAuditEntryDto {
                Id              = a.Id,
                Version         = a.Version,
                ChangeType      = a.ChangeType.ToString(),
                ChangedAtUtc    = a.ChangedAtUtc,
                ChangedByUserId = a.ChangedByUserId,
                Note            = a.Note
            })
            .ToListAsync();
    }

    public async Task<ConfigSectionDetail> RestoreAsync(string section, int version, Guid? userId) {
        var audit = await db.SystemConfigurationAudits.AsNoTracking()
            .Where(a => a.Section == section && a.Version == version)
            .OrderByDescending(a => a.ChangedAtUtc)
            .FirstOrDefaultAsync();
        if (audit is null) {
            throw new ConfigOperationException(404, $"Version {version} of section '{section}' was not found.");
        }

        var row        = await db.SystemConfiguration.FirstOrDefaultAsync(r => r.Section == section);
        int newVersion = (row?.Version ?? 0) + 1;

        if (row is null) {
            row = new SystemConfigurationEntry { Section = section, Json = audit.Json, Version = newVersion };
            db.SystemConfiguration.Add(row);
        }
        else {
            row.Json    = audit.Json; // already-encrypted historical payload
            row.Version = newVersion;
        }
        row.UpdatedAtUtc    = DateTime.UtcNow;
        row.UpdatedByUserId = userId;

        db.SystemConfigurationAudits.Add(new SystemConfigurationAudit {
            Id              = Guid.NewGuid(),
            Section         = section,
            Json            = audit.Json,
            Version         = newVersion,
            ChangeType      = ConfigChangeType.Restore,
            ChangedAtUtc    = DateTime.UtcNow,
            ChangedByUserId = userId,
            Note            = $"Restored from v{version}"
        });

        await db.SaveChangesAsync();
        Reload();
        return (await GetAsync(section))!;
    }

    public async Task<ConfigExportBundle> ExportAsync(string? section) {
        var query = db.SystemConfiguration.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(section)) {
            query = query.Where(r => r.Section == section);
        }

        var rows   = await query.OrderBy(r => r.Section).ToListAsync();
        var bundle = new ConfigExportBundle { ExportedAtUtc = DateTime.UtcNow };
        foreach (var row in rows) {
            bundle.Sections[row.Section] = JsonNode.Parse(ConfigSecretJson.Mask(row.Json));
        }
        return bundle;
    }

    public async Task<ConfigImportResult> ImportAsync(ConfigImportRequest request, bool dryRun, Guid? userId) {
        var result = new ConfigImportResult { DryRun = dryRun };

        var valid = new List<(string Section, JsonNode? Json)>();
        foreach (var (section, json) in request.Sections) {
            var sectionResult = new ConfigSectionImportResult { Section = section };

            bool known  = ConfigSectionCatalog.IsKnownSection(section);
            bool exists = await db.SystemConfiguration.AnyAsync(r => r.Section == section);
            if (!known && !exists) {
                sectionResult.Valid = false;
                sectionResult.Errors.Add($"Unknown configuration section '{section}'.");
            }
            else {
                var v = Validate(section, json);
                sectionResult.Valid = v.Valid;
                sectionResult.Errors = v.Errors;
            }

            result.Sections.Add(sectionResult);
            if (sectionResult.Valid) {
                valid.Add((section, json));
            }
        }

        result.Success = result.Sections.All(s => s.Valid) && result.Sections.Count > 0;

        // Atomic: only apply when every section is valid and this is not a dry run.
        if (dryRun || !result.Success) {
            return result;
        }

        await using var tx = await db.Database.BeginTransactionAsync();
        foreach (var (section, json) in valid) {
            var row = await db.SystemConfiguration.FirstOrDefaultAsync(r => r.Section == section);
            await ApplyAsync(section, json, ConfigChangeType.Import, userId, row);
        }
        await db.SaveChangesAsync();
        await tx.CommitAsync();
        Reload();

        foreach (var s in result.Sections) {
            s.Applied = true;
        }
        return result;
    }

    public async Task<ConfigMigrationStatusDto?> GetMigrationStatusAsync() {
        var state = await db.ConfigurationMigrationState.AsNoTracking().FirstOrDefaultAsync(s => s.Id == 1);
        if (state is null) {
            return null;
        }
        return new ConfigMigrationStatusDto {
            Status        = state.Status.ToString(),
            Source        = state.Source.ToString(),
            LastRunAtUtc  = state.LastRunAtUtc,
            ArchivePath   = state.ArchivePath,
            SectionsCount = state.SectionsCount,
            Detail        = state.Detail
        };
    }

    public async Task<SboSettings?> ResolveSboSettingsDraftAsync(JsonNode? draft) {
        const string section = "SboSettings";
        var row = await db.SystemConfiguration.AsNoTracking().FirstOrDefaultAsync(r => r.Section == section);

        string incoming = draft?.ToJsonString() ?? "null";
        // Restore masked secrets from the stored (encrypted) payload, then decrypt
        // them by flattening through the same path the runtime config uses.
        string merged   = ConfigSecretJson.MergeMaskedSecrets(incoming, row?.Json);
        var pairs       = ConfigSectionJson.Flatten(section, merged, protector);

        IConfigurationRoot cfg = new ConfigurationBuilder().AddInMemoryCollection(pairs).Build();
        return cfg.GetSection(section).Get<SboSettings>();
    }

    // --- internals ---

    private async Task ApplyAsync(
        string section, JsonNode? json, ConfigChangeType changeType, Guid? userId, SystemConfigurationEntry? row) {
        string incoming  = json?.ToJsonString() ?? "null";
        string merged    = ConfigSecretJson.MergeMaskedSecrets(incoming, row?.Json);
        string encrypted = ConfigSecretJson.Encrypt(merged, protector);

        row ??= await db.SystemConfiguration.FirstOrDefaultAsync(r => r.Section == section);
        int newVersion = (row?.Version ?? 0) + 1;

        if (row is null) {
            row = new SystemConfigurationEntry { Section = section, Json = encrypted, Version = newVersion };
            db.SystemConfiguration.Add(row);
        }
        else {
            row.Json    = encrypted;
            row.Version = newVersion;
        }
        row.UpdatedAtUtc    = DateTime.UtcNow;
        row.UpdatedByUserId = userId;

        db.SystemConfigurationAudits.Add(new SystemConfigurationAudit {
            Id              = Guid.NewGuid(),
            Section         = section,
            Json            = encrypted,
            Version         = newVersion,
            ChangeType      = changeType,
            ChangedAtUtc    = DateTime.UtcNow,
            ChangedByUserId = userId
        });
    }

    private void EnsureSectionWritable(string section) {
        if (ConfigSectionCatalog.IsKnownSection(section)) {
            return;
        }
        bool exists = db.SystemConfiguration.Any(r => r.Section == section);
        if (!exists) {
            throw new ConfigOperationException(404, $"Unknown configuration section '{section}'.");
        }
    }

    private bool IsAssemblyAllowed(string assembly, string type) {
        // No allowlist configured -> permit but warn (operators populate it to enforce).
        if (assemblyAllowlist.Count == 0) {
            logger.LogWarning(
                "Configuration: assembly/type '{Assembly}'/'{Type}' accepted because no Configuration:AssemblyAllowlist is configured.",
                assembly, type);
            return true;
        }
        return assemblyAllowlist.Contains($"{assembly}|{type}");
    }

    private void Reload() => (configuration as IConfigurationRoot)?.Reload();

    private static ConfigSectionSummary ToSummary(SystemConfigurationEntry row) {
        var meta = ConfigSectionCatalog.GetMeta(row.Section);
        return new ConfigSectionSummary {
            Section         = row.Section,
            Version         = row.Version,
            UpdatedAtUtc    = row.UpdatedAtUtc,
            UpdatedByUserId = row.UpdatedByUserId,
            ReloadKind      = meta.ReloadKind.ToString(),
            IsAdvanced      = meta.IsAdvanced,
            IsRestricted    = meta.IsRestricted,
            HasSecrets      = ConfigSecretJson.HasSecret(row.Json)
        };
    }

    private static void CopySummary(ConfigSectionSummary from, ConfigSectionSummary to) {
        to.Section         = from.Section;
        to.Version         = from.Version;
        to.UpdatedAtUtc    = from.UpdatedAtUtc;
        to.UpdatedByUserId = from.UpdatedByUserId;
        to.ReloadKind      = from.ReloadKind;
        to.IsAdvanced      = from.IsAdvanced;
        to.IsRestricted    = from.IsRestricted;
        to.HasSecrets      = from.HasSecrets;
    }

    private static T? Bind<T>(string section, string json) {
        var wrapper = new JsonObject { [section] = JsonNode.Parse(json) };
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(wrapper.ToJsonString()));
        IConfigurationRoot cfg = new ConfigurationBuilder().AddJsonStream(stream).Build();
        return cfg.GetSection(section).Get<T>();
    }
}
