using System.ComponentModel.DataAnnotations;

namespace Core.DTOs.Transfer;

public class TransferUpdateLineQuantityRequest {
    [Required]
    public Guid Id { get; set; }
    
    [Required]
    public Guid LineId { get; set; }
    
    [Required]
    [Range(0.01, double.MaxValue, ErrorMessage = "Quantity in Unit must be greater than 0")]
    public decimal Quantity { get; set; }
    
    public string? UserName { get; set; }
}