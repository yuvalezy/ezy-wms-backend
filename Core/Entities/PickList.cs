using System.ComponentModel.DataAnnotations;
using Core.Enums;

namespace Core.Entities;

public class PickList : BaseEntity {
    [Required]
    public int AbsEntry { get; set; }

    public int? BinEntry { get; set; }

    [StringLength(254)]
    public string? ErrorMessage { get; set; }

    [StringLength(50)]
    public required string ItemCode { get; set; }

    [Required]
    public int PickEntry { get; set; }

    [Required]
    public int Quantity { get; set; }

    [Required]
    public UnitType Unit { get; set; }

    [Required]
    public ObjectStatus Status { get; set; } = ObjectStatus.Open;
    public SyncStatus SyncStatus { get; set; } = SyncStatus.Synced;
    public DateTime?  SyncedAt   { get; set; }
    public string?    SyncError  { get; set; }
}

public sealed class PickListPackage : BaseEntity {
    [Required]
    public int AbsEntry { get; set; }           // Pick operation identifier

    [Required] 
    public int PickEntry { get; set; }          // Pick line identifier

    [Required]
    public Guid PackageId { get; set; }         // Source or target package

    [Required]
    public SourceTarget Type { get; set; }      // Source or Target

    public int? BinEntry { get; set; }          // Location information

    [Required]
    public DateTime AddedAt { get; set; } = DateTime.UtcNow;

    [Required]
    public Guid AddedByUserId { get; set; }

    // Navigation properties
    public Package Package { get; set; } = null!;
}