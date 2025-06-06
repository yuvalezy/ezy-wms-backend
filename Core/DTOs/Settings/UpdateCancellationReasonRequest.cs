using System.ComponentModel.DataAnnotations;

namespace Core.DTOs.Settings;

public class UpdateCancellationReasonRequest {
    [Required]
    public Guid Id { get; set; }
    
    [Required]
    [MaxLength(100)]
    public required string Name { get; set; }
    
    public bool Transfer { get; set; }
    public bool GoodsReceipt { get; set; }
    public bool Counting { get; set; }
    public bool IsEnabled { get; set; }
}