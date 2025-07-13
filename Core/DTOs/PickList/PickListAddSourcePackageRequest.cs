using System.ComponentModel.DataAnnotations;

namespace Core.DTOs.PickList;

public class PickListAddSourcePackageRequest {
    [Required]
    public int AbsEntry { get; set; }

    [Required]
    public int PickEntry { get; set; }

    [Required]
    public Guid PackageId { get; set; }

    public int? BinEntry { get; set; }
}