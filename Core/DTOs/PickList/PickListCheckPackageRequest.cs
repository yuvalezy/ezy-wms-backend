using System.ComponentModel.DataAnnotations;

namespace Core.DTOs.PickList;

public class PickListCheckPackageRequest {
    [Required]
    public int PickListId { get; set; }
    
    [Required]
    public Guid PackageId { get; set; }
}