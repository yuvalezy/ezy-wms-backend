using System.ComponentModel.DataAnnotations;

namespace Core.Entities;

public class CancellationReason : BaseEntity {
    [Required]
    [MaxLength(100)]
    public required string Name { get; set; }
    
    public bool IsEnabled { get; set; } = true;
    
    // Object type flags
    public bool Transfer { get; set; }
    public bool GoodsReceipt { get; set; }
    public bool Counting { get; set; }
}