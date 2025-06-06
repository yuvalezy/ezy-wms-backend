using System.ComponentModel.DataAnnotations;

namespace Core.DTOs.Transfer;

public class CancelTransferRequest {
    [Required]
    public Guid ID { get; set; }
}