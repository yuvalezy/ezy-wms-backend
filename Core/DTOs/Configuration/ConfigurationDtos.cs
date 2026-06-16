using System.Text.Json.Nodes;

namespace Core.DTOs.Configuration;

public class ConfigSectionSummary {
    public string    Section         { get; set; } = "";
    public int       Version         { get; set; }
    public DateTime  UpdatedAtUtc    { get; set; }
    public Guid?     UpdatedByUserId { get; set; }
    public string    ReloadKind      { get; set; } = "";
    public bool      IsAdvanced      { get; set; }
    public bool      IsRestricted    { get; set; }
    public bool      HasSecrets      { get; set; }
}

public class ConfigSectionDetail : ConfigSectionSummary {
    /// <summary>Section payload with secret leaves masked.</summary>
    public JsonNode? Json { get; set; }
}

public class ConfigSectionUpdateRequest {
    public JsonNode? Json            { get; set; }
    /// <summary>Optimistic concurrency: the version the editor last read.</summary>
    public int?      ExpectedVersion { get; set; }
}

public class ConfigValidationResult {
    public bool         Valid  { get; set; }
    public List<string> Errors { get; set; } = [];
}

public class ConfigAuditEntryDto {
    public Guid      Id              { get; set; }
    public int       Version         { get; set; }
    public string    ChangeType      { get; set; } = "";
    public DateTime  ChangedAtUtc    { get; set; }
    public Guid?     ChangedByUserId { get; set; }
    public string?   Note            { get; set; }
}

public class ConfigMigrationStatusDto {
    public string    Status        { get; set; } = "";
    public string    Source        { get; set; } = "";
    public DateTime? LastRunAtUtc  { get; set; }
    public string?   ArchivePath   { get; set; }
    public int?      SectionsCount { get; set; }
    public string?   Detail        { get; set; }
}

public class ConfigExportBundle {
    public int                            FormatVersion { get; set; } = 1;
    public DateTime                       ExportedAtUtc { get; set; }
    /// <summary>Section name -> masked (secrets-scrubbed) JSON.</summary>
    public Dictionary<string, JsonNode?>  Sections      { get; set; } = new();
}

public class ConfigImportRequest {
    public Dictionary<string, JsonNode?> Sections { get; set; } = new();
}

public class ConfigSectionImportResult {
    public string       Section { get; set; } = "";
    public bool         Valid   { get; set; }
    public bool         Applied { get; set; }
    public List<string> Errors  { get; set; } = [];
}

public class ConfigImportResult {
    public bool                            Success  { get; set; }
    public bool                            DryRun   { get; set; }
    public List<ConfigSectionImportResult> Sections { get; set; } = [];
}
