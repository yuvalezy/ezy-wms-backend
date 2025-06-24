using System.ComponentModel.DataAnnotations;

namespace Core.DTOs.Transfer;

public class CreateTransferRequestRequest {
    [Required]
    [MinLength(1, ErrorMessage = "At least one transfer content item is required")]
    public required TransferContentResponse[] Contents { get; set; }
}

public class CreateTransferRequestResponse {
    public int Number { get; set; }
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
}