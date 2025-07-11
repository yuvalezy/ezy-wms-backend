using System.ComponentModel.DataAnnotations;

namespace Core.DTOs.Transfer;

public class TransferAddSourcePackageRequest {
    [Required]
    public Guid TransferId { get; set; }
    
    [Required]
    public string Barcode { get; set; } = string.Empty;
    
    public int? BinEntry { get; set; }
}