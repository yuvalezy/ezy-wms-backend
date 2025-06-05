using System.ComponentModel.DataAnnotations;

namespace Core.DTOs;

public class CancelGoodsReceiptRequest {
    [Required]
    public int ID { get; set; }
}