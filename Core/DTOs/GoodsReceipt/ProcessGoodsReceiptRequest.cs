using System.ComponentModel.DataAnnotations;

namespace Core.DTOs.GoodsReceipt;

public class ProcessGoodsReceiptRequest {
    [Required]
    public Guid Id { get; set; }
}