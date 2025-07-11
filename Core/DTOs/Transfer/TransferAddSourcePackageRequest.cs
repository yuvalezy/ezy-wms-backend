using System.ComponentModel.DataAnnotations;

namespace Core.DTOs.Transfer;

public class TransferAddSourcePackageRequest {
    [Required]
    public Guid TransferId { get; set; }

    [Required]
    public Guid PackageId { get; set; }

    public int? BinEntry { get; set; }
}