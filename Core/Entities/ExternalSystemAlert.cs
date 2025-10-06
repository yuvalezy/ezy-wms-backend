using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Core.Enums;

namespace Core.Entities;

[Table("ExternalSystemAlerts")]
public class ExternalSystemAlert : BaseEntity {
    [Required]
    public AlertableObjectType ObjectType { get; set; }

    [Required]
    [MaxLength(50)]
    public required string ExternalUserId { get; set; }

    [Required]
    public bool Enabled { get; set; } = true;
}
