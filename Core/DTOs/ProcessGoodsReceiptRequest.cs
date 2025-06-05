using System.ComponentModel.DataAnnotations;

namespace Core.DTOs;

public class ProcessGoodsReceiptRequest {
    [Required]
    public Guid Id { get; set; }
}