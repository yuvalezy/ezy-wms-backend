using System.ComponentModel.DataAnnotations;

namespace Core.DTOs.DirectTransfer;

public class DirectTransferRequest {
    [Required]
    public int SourceBinEntry { get; set; }

    [Required]
    public required string ItemCode { get; set; }

    [Required]
    public int TargetBinEntry { get; set; }

    [Range(0, double.MaxValue, ErrorMessage = "Quantity must be zero or greater")]
    public decimal Quantity { get; set; }

    public string? UnitCode { get; set; }

    public bool TransferAll { get; set; }
}
