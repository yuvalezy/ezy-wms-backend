using System.ComponentModel.DataAnnotations;
using Core.Enums;

namespace Core.DTOs;

public class TransferContentRequest {
    [Required]
    public Guid ID { get; set; }
    
    public int? BinEntry { get; set; }
    
    public string? BinCode { get; set; }
    
    [Required]
    public SourceTarget Type { get; set; }
    
    public bool TargetBins { get; set; }
    
    public bool TargetBinQuantity { get; set; }
    
    public string? ItemCode { get; set; }
}