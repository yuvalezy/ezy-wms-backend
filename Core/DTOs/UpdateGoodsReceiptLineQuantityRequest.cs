using System.ComponentModel.DataAnnotations;

namespace Core.DTOs;

public class UpdateGoodsReceiptLineQuantityRequest {
    [Required]
    public Guid Id { get; set; }
    
    [Required]
    public Guid LineID { get; set; }
    
    [Required]
    public decimal Quantity { get; set; }
}