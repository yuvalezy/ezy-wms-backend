using System.ComponentModel.DataAnnotations;

namespace Core.DTOs.Transfer;

public class TransferUpdateLineQuantityRequest {
    [Required]
    public Guid Id { get; set; }
    
    [Required]
    public Guid LineId { get; set; }
    
    [Required]
    [Range(1, int.MaxValue, ErrorMessage = "Quantity in Unit cannot be less than 1")]
    public int Quantity { get; set; }
    
    public string? UserName { get; set; }
}