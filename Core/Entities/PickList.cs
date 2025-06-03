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
    public string? ItemCode { get; set; }

    [Required]
    public int PickEntry { get; set; }

    [Required]
    public int Quantity { get; set; }

    [Required]
    public ObjectStatus Status { get; set; } = ObjectStatus.Open;
}