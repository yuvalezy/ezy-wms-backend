using System.ComponentModel.DataAnnotations;

namespace Core.DTOs.GoodsReceipt;

public class GoodsReceiptValidateProcessLineDetailsRequest {
    [Required]
    public Guid ID { get; set; }
    
    [Required]
    public Guid LineID { get; set; }
    
    public int? SourceType { get; set; }
    public int? SourceEntry { get; set; }
}