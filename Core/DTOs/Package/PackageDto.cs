using Core.DTOs.General;
using Core.Enums;
using Core.Models.Settings;

namespace Core.DTOs.Package;

public class PackageDto {
    public          Guid                        Id               { get; set; }
    public required string                      Barcode          { get; set; }
    public          PackageStatus               Status           { get; set; }
    public required string                      WhsCode          { get; set; }
    public          int?                        BinEntry         { get; set; }
    public          string?                     BinCode          { get; set; }
    public          UserAuditResponse?          CreatedBy        { get; set; }
    public          DateTime                    CreatedAt        { get; set; }
    public          DateTime?                   ClosedAt         { get; set; }
    public          Guid?                       ClosedBy         { get; set; }
    public          string?                     Notes            { get; set; }
    public          Dictionary<string, object>? CustomAttributes { get; set; } = [];
    public          PackageContentDto[]         Contents         { get; set; } = [];
    public          PackageLocationHistoryDto[] LocationHistory  { get; set; } = [];
    
    /// <summary>
    /// Available metadata field definitions for this package
    /// </summary>
    public          PackageMetadataDefinition[] MetadataDefinitions { get; set; } = [];
}
