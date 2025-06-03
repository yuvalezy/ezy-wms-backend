using System.ComponentModel.DataAnnotations;

namespace Core.DTOs;

public class ProcessTransferRequest {
    [Required]
    public Guid ID { get; set; }
}