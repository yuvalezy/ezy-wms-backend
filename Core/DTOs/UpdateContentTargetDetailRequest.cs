using System.ComponentModel.DataAnnotations;

namespace Core.DTOs;

public class UpdateContentTargetDetailRequest {
    [Required]
    public Guid ID { get; set; }
    
    public List<Guid>? RemoveRows { get; set; }
    
    public Dictionary<Guid, int>? QuantityChanges { get; set; }
}