using System.ComponentModel.DataAnnotations;

namespace Core.Entities;

public class PickListCheckPackage : BaseEntity {
    [Required]
    public Guid CheckSessionId { get; set; }

    [Required]
    public Guid PackageId { get; set; }

    [Required]
    [StringLength(50)]
    public string PackageBarcode { get; set; } = string.Empty;

    [Required]
    public DateTime CheckedAt { get; set; }

    [Required]
    public Guid CheckedByUserId { get; set; }

    // Navigation properties
    public virtual PickListCheckSession CheckSession { get; set; } = null!;
    public virtual Package Package { get; set; } = null!;
    public virtual User CheckedByUser { get; set; } = null!;
}