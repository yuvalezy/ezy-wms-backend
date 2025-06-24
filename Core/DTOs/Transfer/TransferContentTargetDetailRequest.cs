using System.ComponentModel.DataAnnotations;

namespace Core.DTOs.Transfer;

public class TransferContentTargetDetailRequest {
    [Required]
    public Guid ID { get; set; }
    
    public int? BinEntry { get; set; }
    
    [Required]
    public string ItemCode { get; set; } = string.Empty;
}