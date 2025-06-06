using System.ComponentModel.DataAnnotations;

namespace Core.DTOs.Transfer;

public class ProcessTransferRequest {
    [Required]
    public Guid ID { get; set; }
}