using System.ComponentModel.DataAnnotations;

namespace Core.DTOs;

public class UpdateGoodsReceiptAllRequest {
    [Required]
    public Guid Id { get; set; }

    [Required]
    public required Dictionary<Guid, decimal> QuantityChanges { get; set; }

    [Required]
    public required Guid[] RemoveRows { get; set; }
}