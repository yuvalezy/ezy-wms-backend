using System.ComponentModel.DataAnnotations;

namespace Core.DTOs;

public class UpdateLineQuantityRequest {
    [Required]
    public Guid ID { get; set; }
    
    [Required]
    public Guid LineID { get; set; }
    
    [Required]
    [Range(1, int.MaxValue, ErrorMessage = "Quantity in Unit cannot be less than 1")]
    public int Quantity { get; set; }
    
    public string? UserName { get; set; }
}