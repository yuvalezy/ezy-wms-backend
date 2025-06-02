using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Core.Enums;

namespace Core.Entities;

[Table("AuthorizationGroups")]
public class AuthorizationGroup : BaseEntity {
    [Required]
    [MaxLength(50)]
    public required string Name { get; set; }
    
    [MaxLength(200)]
    public string? Description { get; set; }

    [Required]
    public required ICollection<Authorization> Authorizations { get; set; }
}