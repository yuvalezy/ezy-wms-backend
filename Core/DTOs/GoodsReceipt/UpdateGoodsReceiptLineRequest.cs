using System.ComponentModel.DataAnnotations;
using Core.Enums;

namespace Core.DTOs.GoodsReceipt;

public class UpdateGoodsReceiptLineRequest {
    [Required]
    public Guid Id { get; set; }
    
    [Required]
    public Guid LineID { get; set; }
    
    public LineStatus? Status { get; set; }
    
    public int? StatusReason { get; set; }
    
    public Guid? CancellationReasonId { get; set; }
    
    [MaxLength(4000)]
    public string? Comment { get; set; }
}