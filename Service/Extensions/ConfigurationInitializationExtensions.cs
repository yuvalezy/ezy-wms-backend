using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Core.Configuration;
using Core.Entities;
using Core.Enums;
using Infrastructure.Configuration;
using Infrastructure.DbContexts;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Service.Extensions;

/// <summary>
/// Resolves the configuration source on startup and ingests it into the database:
///   1. config/ present     -> migrate -> verify -> archive
///   2. else DB has rows    -> DB-only boot
///   3. else init/ present  -> seed    -> verify (no archive)
///   4. else                -> throw (no configuration source)
///
/// Migrate and seed share one engine that reads each YAML section at the
/// IConfiguration level (preserving [JsonIgnore] SQL fields), stores it as JSON
/// (secrets encrypted), then verifies the stored result key-for-key against the
/// source. A verification mismatch throws and fails startup; the source folder is
/// left untouched. Runs after ConfigureDatabase() (tables exist) and before
/// TestConfigurations().
/// </summary>
public static class ConfigurationInitializationExtensions {
    public static async Task<WebApplication> InitializeConfigurationAsync(this WebApplication app) {
        using var scope = app.Services.CreateScope();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<ConfigurationInitializerMarker>>();
        var db     = scope.ServiceProvider.GetRequiredService<SystemDbContext>();

        string? connString = app.Configuration.GetConnectionString("DefaultConnection");
        string? encKey     = app.Configuration["Licensing:EncryptionKey"];

        string contentRoot = app.Environment.ContentRootPath;
        string configDir   = Path.Combine(contentRoot, "config");
        string initDir     = Path.Combine(contentRoot, "init");
        string archiveDir  = Path.Combine(contentRoot, "config_archive");

        bool configExists = HasYaml(configDir);
        bool dbHasRows    = await db.SystemConfiguration.AnyAsync();
        bool initExists   = HasYaml(initDir);

        if (configExists) {
            logger.LogInformation("Configuration: migrating from {Dir}", configDir);
            await IngestAsync(app, db, logger, connString, encKey,
                configDir, ConfigChangeType.Migration, ConfigSourceKind.LiveConfigFolder,
                archiveAfter: true, configDir, archiveDir);
        }
        else if (dbHasRows) {
            logger.LogInformation("Configuration: loaded from database ({Count} sections).",
                await db.SystemConfiguration.CountAsync());
        }
        else if (initExists) {
            logger.LogInformation("Configuration: fresh install, seeding from templates in {Dir}", initDir);
            await IngestAsync(app, db, logger, connString, encKey,
                initDir, ConfigChangeType.Seed, ConfigSourceKind.InitTemplates,
                archiveAfter: false, configDir, archiveDir);
        }
        else {
            throw new InvalidOperationException(
                "No configuration source: the config/ folder, database configuration, and init/ templates are all absent.");
        }

        await PruneRetiredSectionsAsync(db, logger);

        return app;
    }

    /// <summary>
    /// Removes stored configuration rows for sections that have been retired from the
    /// product (see <see cref="ConfigSectionCatalog.RetiredSections"/>). Idempotent;
    /// audit history is intentionally preserved.
    /// </summary>
    private static async Task PruneRetiredSectionsAsync(SystemDbContext db, ILogger logger) {
        var rows = await db.SystemConfiguration.ToListAsync();
        var retired = rows.Where(r => ConfigSectionCatalog.RetiredSections.Contains(r.Section)).ToList();
        if (retired.Count == 0) {
            return;
        }

        db.SystemConfiguration.RemoveRange(retired);
        await db.SaveChangesAsync();
        logger.LogInformation("Configuration: pruned {Count} retired section(s): {Sections}",
            retired.Count, string.Join(", ", retired.Select(r => r.Section)));
    }

    private static async Task IngestAsync(
        WebApplication app, SystemDbContext db, ILogger logger,
        string? connString, string? encKey,
        string sourceDir, ConfigChangeType changeType, ConfigSourceKind sourceKind,
        bool archiveAfter, string configDir, string archiveDir) {

        var protector = new ConfigSecretProtector(encKey);

        // 1. Build an isolated configuration over only the source YAML files.
        IConfigurationRoot sourceConfig = BuildYamlConfig(sourceDir);

        // 2. Discover the sections (top-level keys) and serialize each subtree to JSON.
        var sections = sourceConfig.GetChildren().Select(c => c.Key).ToList();
        var payloads = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (string section in sections) {
            payloads[section] = ConfigSectionJson.SectionToJson(sourceConfig.GetSection(section), protector);
        }

        // 3. Transactional: back up existing rows to audit, then upsert each section.
        await using (var tx = await db.Database.BeginTransactionAsync()) {
            var existing = await db.SystemConfiguration.ToListAsync();

            foreach (var row in existing) {
                db.SystemConfigurationAudits.Add(new SystemConfigurationAudit {
                    Id           = Guid.NewGuid(),
                    Section      = row.Section,
                    Json         = row.Json,
                    Version      = row.Version,
                    ChangeType   = ConfigChangeType.PreMigrationBackup,
                    ChangedAtUtc = DateTime.UtcNow,
                    Note         = $"Backup before {changeType}"
                });
            }

            foreach (var (section, json) in payloads) {
                var row        = existing.FirstOrDefault(e => e.Section == section);
                int newVersion = (row?.Version ?? 0) + 1;

                if (row is null) {
                    db.SystemConfiguration.Add(new SystemConfigurationEntry {
                        Section = section, Json = json, Version = newVersion, UpdatedAtUtc = DateTime.UtcNow
                    });
                }
                else {
                    row.Json         = json;
                    row.Version      = newVersion;
                    row.UpdatedAtUtc = DateTime.UtcNow;
                }

                db.SystemConfigurationAudits.Add(new SystemConfigurationAudit {
                    Id           = Guid.NewGuid(),
                    Section      = section,
                    Json         = json,
                    Version      = newVersion,
                    ChangeType   = changeType,
                    ChangedAtUtc = DateTime.UtcNow
                });
            }

            await db.SaveChangesAsync();
            await tx.CommitAsync();
        }

        // 4. Reload the live database configuration provider.
        (app.Configuration as IConfigurationRoot)?.Reload();

        // 5. Verify the stored configuration matches the source, key-for-key.
        IConfigurationRoot dbConfig = new ConfigurationBuilder()
            .Add(new DatabaseConfigurationSource(connString, encKey))
            .Build();

        var diffs = CompareTrees(sourceConfig, dbConfig, sections);
        if (diffs.Count > 0) {
            string detail = string.Join("\n", diffs.Take(50));
            await RecordStateAsync(db, ConfigMigrationStatus.Failed, sourceKind, sections.Count, archivePath: null, detail);
            logger.LogError("Configuration verification FAILED ({Count} differences):\n{Detail}", diffs.Count, detail);
            throw new InvalidOperationException(
                $"Configuration verification failed: {diffs.Count} differences between {sourceDir} and the database. Startup aborted; the source folder was left untouched.");
        }

        // 6. Record success and (for the live config/ folder) archive it.
        string? archivedTo = null;
        if (archiveAfter) {
            archivedTo = ArchiveConfigDir(configDir, archiveDir, logger);
        }

        await RecordStateAsync(db, ConfigMigrationStatus.Verified, sourceKind, sections.Count, archivedTo,
            $"{changeType} verified for {sections.Count} sections.");
        logger.LogInformation("Configuration: {ChangeType} verified for {Count} sections.{Archive}",
            changeType, sections.Count, archivedTo is null ? "" : $" Archived to {archivedTo}.");
    }

    private static IConfigurationRoot BuildYamlConfig(string dir) {
        var builder = new ConfigurationBuilder().SetBasePath(dir);
        foreach (string file in Directory.GetFiles(dir, "*.yaml").OrderBy(f => f, StringComparer.Ordinal)) {
            if (file.EndsWith(".backup.yaml", StringComparison.OrdinalIgnoreCase)) {
                continue; // operator backup, never a config source
            }
            builder.AddYamlFile(Path.GetFileName(file), optional: false, reloadOnChange: false);
        }
        return builder.Build();
    }

    /// <summary>Compares the two trees restricted to the given sections, ignoring null/empty leaves.</summary>
    private static List<string> CompareTrees(IConfiguration source, IConfiguration db, List<string> sections) {
        var src = FlattenForSections(source, sections);
        var dst = FlattenForSections(db, sections);
        var diffs = new List<string>();

        foreach (var (key, value) in src) {
            if (!dst.TryGetValue(key, out var other)) {
                diffs.Add($"MISSING in db: {key} = {value}");
            }
            else if (!string.Equals(value, other, StringComparison.Ordinal)) {
                diffs.Add($"CHANGED: {key}: source='{value}' db='{other}'");
            }
        }
        foreach (var key in dst.Keys) {
            if (!src.ContainsKey(key)) {
                diffs.Add($"EXTRA in db: {key} = {dst[key]}");
            }
        }
        return diffs;
    }

    private static Dictionary<string, string> FlattenForSections(IConfiguration config, List<string> sections) {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var kv in config.AsEnumerable()) {
            if (string.IsNullOrEmpty(kv.Value)) {
                continue;
            }
            string top = kv.Key.Split(':', 2)[0];
            if (sections.Contains(top, StringComparer.OrdinalIgnoreCase)) {
                result[kv.Key] = kv.Value;
            }
        }
        return result;
    }

    private static string? ArchiveConfigDir(string configDir, string archiveDir, ILogger logger) {
        string target = archiveDir;
        if (Directory.Exists(archiveDir)) {
            target = Path.Combine(archiveDir, DateTime.UtcNow.ToString("yyyyMMddHHmmss"));
        }

        Directory.CreateDirectory(Path.GetDirectoryName(target.TrimEnd(Path.DirectorySeparatorChar))!);

        try {
            Directory.Move(configDir, target);
        }
        catch (IOException) {
            // Cross-device move: fall back to copy-then-delete.
            CopyDirectory(configDir, target);
            Directory.Delete(configDir, recursive: true);
        }

        logger.LogInformation("Configuration: archived {Source} -> {Target}", configDir, target);
        return target;
    }

    private static void CopyDirectory(string source, string target) {
        Directory.CreateDirectory(target);
        foreach (string file in Directory.GetFiles(source)) {
            File.Copy(file, Path.Combine(target, Path.GetFileName(file)), overwrite: true);
        }
        foreach (string dir in Directory.GetDirectories(source)) {
            CopyDirectory(dir, Path.Combine(target, Path.GetFileName(dir)));
        }
    }

    private static async Task RecordStateAsync(
        SystemDbContext db, ConfigMigrationStatus status, ConfigSourceKind source,
        int sectionsCount, string? archivePath, string? detail) {
        var state = await db.ConfigurationMigrationState.FirstOrDefaultAsync(s => s.Id == 1);
        if (state is null) {
            state = new ConfigurationMigrationStateEntity { Id = 1 };
            db.ConfigurationMigrationState.Add(state);
        }

        state.Status        = status;
        state.Source        = source;
        state.SectionsCount = sectionsCount;
        state.ArchivePath   = archivePath;
        state.Detail        = detail;
        state.LastRunAtUtc  = DateTime.UtcNow;

        await db.SaveChangesAsync();
    }

    private static bool HasYaml(string dir) {
        if (!Directory.Exists(dir)) {
            return false;
        }
        return Directory.EnumerateFiles(dir, "*.yaml")
            .Any(f => !f.EndsWith(".backup.yaml", StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>Marker type for an ILogger category name.</summary>
    public sealed class ConfigurationInitializerMarker;
}
