using System.ComponentModel.DataAnnotations;
using Core.Enums;

namespace Core.DTOs.Transfer;

public class CreateTransferRequestRequest {
    [Required]
    [MinLength(1, ErrorMessage = "At least one transfer content item is required")]
    public required CreateTransferRequestRequestContent[] Contents { get; set; }
}

public class CreateTransferRequestRequestContent {
    public required string ItemCode { get; set; }
    public string? Barcode { get; set; }
    public int Quantity { get; set; }
    public UnitType Unit { get; set; }
}

public class CreateTransferRequestResponse {
    public int Number { get; set; }
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
}