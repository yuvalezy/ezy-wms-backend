using System.ComponentModel.DataAnnotations;

namespace Core.DTOs.GoodsReceipt;

public class CancelGoodsReceiptRequest {
    [Required]
    public int ID { get; set; }
}