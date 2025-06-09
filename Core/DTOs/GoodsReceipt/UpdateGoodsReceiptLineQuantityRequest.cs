using System.ComponentModel.DataAnnotations;

namespace Core.DTOs.GoodsReceipt;

public class UpdateGoodsReceiptLineQuantityRequest {
    [Required]
    public Guid Id { get; set; }
    
    [Required]
    public Guid LineId { get; set; }
    
    [Required]
    public decimal Quantity { get; set; }
}