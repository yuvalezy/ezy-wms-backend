using System.ComponentModel.DataAnnotations;

namespace Core.DTOs;

public class CancelTransferRequest {
    [Required]
    public Guid ID { get; set; }
}