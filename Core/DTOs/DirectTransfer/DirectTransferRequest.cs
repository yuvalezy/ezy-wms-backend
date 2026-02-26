using System.ComponentModel.DataAnnotations;

namespace Core.DTOs.DirectTransfer;

public class DirectTransferRequest {
    [Required]
    public int SourceBinEntry { get; set; }

    [Required]
    public required string ItemCode { get; set; }

    [Required]
    public int TargetBinEntry { get; set; }

    [Required]
    [Range(0.01, double.MaxValue, ErrorMessage = "Quantity must be greater than zero")]
    public decimal Quantity { get; set; }

    [Required]
    public required string UnitCode { get; set; }
}
