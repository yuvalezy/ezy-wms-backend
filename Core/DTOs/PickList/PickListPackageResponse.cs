using Core.DTOs.Package;

namespace Core.DTOs.PickList;

public class PickListPackageResponse {
    public bool Success { get; set; } = true;
    public string? ErrorMessage { get; set; }
    public Guid? PackageId { get; set; }
    public List<PackageContentDto> PackageContents { get; set; } = new();
    public bool IsAutoPickResult { get; set; } = false;
    public Guid? TargetPackageId { get; set; }
    public List<Guid> CreatedPickListLineIds { get; set; } = new();
}