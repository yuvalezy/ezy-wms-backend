using Core.Enums;

namespace Core.Configuration;

/// <summary>Static metadata for one configuration section.</summary>
public sealed record ConfigSectionMeta(
    string Name,
    ConfigReloadKind ReloadKind,
    bool IsAdvanced,
    bool IsRestricted);

/// <summary>
/// Single source of truth for the configuration sections that are migrated from
/// the YAML files into the database, their reload semantics, and the rules used
/// for secret handling and the assembly-load hardening.
///
/// Note: the actual set of sections to ingest is discovered dynamically from the
/// source YAML files at runtime; this catalog supplies metadata and is the
/// authoritative review list. Sections sourced from appsettings.json
/// (Smtp, Licensing, BackgroundServices, SessionManagement, PresenceTracking)
/// are intentionally NOT here — they stay in appsettings.json.
/// </summary>
public static class ConfigSectionCatalog {
    /// <summary>Marker prefix on an encrypted secret value stored in section JSON.</summary>
    public const string EncryptedPrefix = "ENC::";

    /// <summary>Value returned/accepted by the API in place of a real secret.</summary>
    public const string SecretMask = "********";

    /// <summary>Leaf property names whose values are treated as secrets (case-insensitive).</summary>
    private static readonly HashSet<string> SecretLeafNames = new(StringComparer.OrdinalIgnoreCase) {
        "Password",
        "ServerPassword",
        "PrivateKeyPassphrase"
    };

    /// <summary>Leaf property names that load/execute code and must be allowlisted.</summary>
    private static readonly HashSet<string> RestrictedLeafNames = new(StringComparer.OrdinalIgnoreCase) {
        "Assembly",
        "TypeName"
    };

    private static readonly Dictionary<string, ConfigSectionMeta> Meta = new(StringComparer.OrdinalIgnoreCase) {
        ["Options"]          = new("Options",          ConfigReloadKind.Live,            IsAdvanced: false, IsRestricted: false),
        ["Filters"]          = new("Filters",          ConfigReloadKind.Live,            IsAdvanced: true,  IsRestricted: false),
        ["Warehouses"]       = new("Warehouses",       ConfigReloadKind.Live,            IsAdvanced: false, IsRestricted: false),
        ["CustomFields"]     = new("CustomFields",     ConfigReloadKind.Live,            IsAdvanced: false, IsRestricted: false),
        ["Item"]             = new("Item",             ConfigReloadKind.Live,            IsAdvanced: false, IsRestricted: false),
        ["PickingDetails"]   = new("PickingDetails",   ConfigReloadKind.Live,            IsAdvanced: false, IsRestricted: false),
        ["ExternalCommands"] = new("ExternalCommands", ConfigReloadKind.Live,            IsAdvanced: true,  IsRestricted: true),
        ["SboSettings"]      = new("SboSettings",      ConfigReloadKind.Live,            IsAdvanced: false, IsRestricted: false),
        ["ExternalAdapter"]  = new("ExternalAdapter",  ConfigReloadKind.RequiresRestart, IsAdvanced: false, IsRestricted: false),
        ["PickingPostProcessingProcessors"] =
            new("PickingPostProcessingProcessors", ConfigReloadKind.RequiresRestart, IsAdvanced: true, IsRestricted: true),
    };

    /// <summary>The reviewable list of sections expected to live in the YAML config files.</summary>
    public static IReadOnlyCollection<string> KnownSections => Meta.Keys;

    public static ConfigSectionMeta GetMeta(string section) =>
        Meta.TryGetValue(section, out var m)
            ? m
            : new ConfigSectionMeta(section, ConfigReloadKind.Live, IsAdvanced: true, IsRestricted: false);

    public static bool IsKnownSection(string section) => Meta.ContainsKey(section);

    /// <summary>True if a flattened config key (e.g. "SboSettings:Password") is a secret leaf.</summary>
    public static bool IsSecretKey(string fullKey) => SecretLeafNames.Contains(LastSegment(fullKey));

    /// <summary>True if a flattened config key is a restricted, code-loading leaf.</summary>
    public static bool IsRestrictedKey(string fullKey) => RestrictedLeafNames.Contains(LastSegment(fullKey));

    public static bool IsEncrypted(string? value) =>
        value is not null && value.StartsWith(EncryptedPrefix, StringComparison.Ordinal);

    private static string LastSegment(string key) {
        int idx = key.LastIndexOf(':');
        return idx >= 0 ? key[(idx + 1)..] : key;
    }
}
