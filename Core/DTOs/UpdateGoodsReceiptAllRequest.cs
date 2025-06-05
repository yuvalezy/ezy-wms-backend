using System.ComponentModel.DataAnnotations;

namespace Core.DTOs;

public class UpdateGoodsReceiptAllRequest {
    [Required]
    public Guid Id { get; set; }
    
    [Required]
    public required List<UpdateGoodsReceiptAllLineRequest> Lines { get; set; }
}

public class UpdateGoodsReceiptAllLineRequest {
    [Required]
    public Guid LineID { get; set; }
    
    [Required]
    public decimal Quantity { get; set; }
    
    public string? Comments { get; set; }
}