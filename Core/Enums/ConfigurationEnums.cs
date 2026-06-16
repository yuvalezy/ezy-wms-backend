namespace Core.Enums;

/// <summary>How a configuration section's changes take effect.</summary>
public enum ConfigReloadKind {
    /// <summary>Read per-request; changes apply live after a configuration reload.</summary>
    Live,

    /// <summary>Bound at startup/DI; changes apply only after a service restart.</summary>
    RequiresRestart
}

/// <summary>The kind of change recorded in the configuration audit trail.</summary>
public enum ConfigChangeType {
    Migration,
    Seed,
    Edit,
    Import,
    Restore,
    PreMigrationBackup
}

/// <summary>Outcome of the file -> database configuration initialization.</summary>
public enum ConfigMigrationStatus {
    NotStarted,
    Verified,
    Failed
}

/// <summary>Which source the configuration was last ingested from.</summary>
public enum ConfigSourceKind {
    None,
    LiveConfigFolder,
    InitTemplates,
    Database
}
