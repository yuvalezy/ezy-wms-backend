using System.ComponentModel.DataAnnotations;

namespace Core.DTOs.Transfer;

public class TransferAddTargetPackageRequest {
    [Required]
    public Guid TransferId { get; set; }
    
    [Required]
    public Guid PackageId { get; set; }
    
    public int? TargetBinEntry { get; set; }
}