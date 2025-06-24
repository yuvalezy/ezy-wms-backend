using System.ComponentModel.DataAnnotations;

namespace Core.DTOs.GoodsReceipt;

public class GoodsReceiptValidateProcessLineDetailsRequest {
    [Required]
    public Guid ID { get; set; }
    
    public int BaseType  { get; set; }
    public int BaseEntry { get; set; }
    public int BaseLine { get; set; }
}