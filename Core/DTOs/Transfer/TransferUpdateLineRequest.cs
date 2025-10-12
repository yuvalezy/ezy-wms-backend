using System.ComponentModel.DataAnnotations;

namespace Core.DTOs.Transfer;

public class TransferUpdateLineRequest {
    [Required]
    public Guid Id { get; set; }
    
    [Required]
    public Guid LineId { get; set; }
    
    public string? Comment { get; set; }

    [Range(0.01, double.MaxValue, ErrorMessage = "Quantity in Unit must be greater than 0")]
    public decimal? Quantity { get; set; }

    public Guid? CancellationReasonId { get; set; }
    
    public string? UserName { get; set; }
}