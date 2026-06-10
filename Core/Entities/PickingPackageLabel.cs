using System.ComponentModel.DataAnnotations;

namespace Core.Entities;

public class PickingPackageLabel : BaseEntity {
    [Required]
    public int AbsEntry { get; set; }

    [Required]
    [StringLength(50)]
    public required string WhsCode { get; set; }

    [Required]
    [StringLength(32)]
    public required string Code { get; set; }

    [Required]
    public int Sequence { get; set; }

    public ICollection<PickList> PickLists { get; set; } = [];
}
