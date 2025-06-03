using System.ComponentModel.DataAnnotations;

namespace Core.DTOs;

public class UpdateLineRequest {
    [Required]
    public Guid ID { get; set; }
    
    [Required]
    public Guid LineID { get; set; }
    
    public string? Comment { get; set; }
    
    [Range(1, int.MaxValue, ErrorMessage = "Quantity in Unit cannot be less than 1")]
    public int? Quantity { get; set; }
    
    public int? CloseReason { get; set; }
    
    public string? UserName { get; set; }
}