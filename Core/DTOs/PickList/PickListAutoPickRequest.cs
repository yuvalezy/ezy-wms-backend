using System.ComponentModel.DataAnnotations;

namespace Core.DTOs.PickList;

public class PickListAutoPickRequest {
    [Required]
    public int AbsEntry { get; set; }

    [Required]
    public int PickEntry { get; set; }

    [Required]
    public Guid SourcePackageId { get; set; }

    /// <summary>
    /// Optional target package ID. If not provided, a new package will be created.
    /// </summary>
    public Guid? TargetPackageId { get; set; }

    /// <summary>
    /// Target bin location for the picked items
    /// </summary>
    public int? TargetBinEntry { get; set; }
}