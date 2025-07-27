using System.ComponentModel.DataAnnotations;

namespace Core.DTOs.PickList;

public class PickListAddPackageRequest {
    /// <summary>
    /// The pick list absolute entry (PickList.AbsEntry)
    /// </summary>
    [Required]
    public int ID { get; set; }
    
    /// <summary>
    /// The document type (e.g., Sales Order = 17, Invoice = 13)
    /// </summary>
    [Required]
    public int Type { get; set; }
    
    /// <summary>
    /// The document entry number
    /// </summary>
    [Required]
    public int Entry { get; set; }
    
    /// <summary>
    /// The package ID to add to the pick list
    /// </summary>
    [Required]
    public Guid PackageId { get; set; }
    
    /// <summary>
    /// Optional bin entry where the package is located
    /// </summary>
    public int? BinEntry { get; set; }

    /// <summary>
    /// Optional add package into a new picking package
    /// </summary>
    public Guid? PickingPackageId { get; set; }
}