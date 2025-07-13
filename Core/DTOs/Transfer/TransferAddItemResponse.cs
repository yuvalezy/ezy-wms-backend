using Core.DTOs.Package;

namespace Core.DTOs.Transfer;

public class TransferAddItemResponse {
    public Guid?   LineId         { get; set; }
    public bool    ClosedTransfer { get; set; }
    public string? ErrorMessage   { get; set; }

    // Package-related properties
    public bool                     IsPackageScan     { get; set; }
    public bool                     IsPackageTransfer { get; set; }
    public Guid?                    PackageId         { get; set; }
    public List<PackageContentDto>? PackageContents   { get; set; }
    public Guid[]?                  LinesIds          { get; set; }
}