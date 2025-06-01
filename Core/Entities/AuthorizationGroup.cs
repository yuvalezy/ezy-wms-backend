using System.ComponentModel.DataAnnotations;
using Core.Enums;

namespace Core.Entities;

public class AuthorizationGroup : BaseEntity {
    [Required]
    public required string Name { get; set; }

    [Required]
    public required ICollection<Authorization> Authorizations { get; set; }
}