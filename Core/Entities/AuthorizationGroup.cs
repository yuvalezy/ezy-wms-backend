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
    public required ICollection<RoleType> Authorizations { get; set; }

    /// <summary>
    /// Per-report grants. Reports are created at runtime, so there is no <see cref="RoleType"/> to mint per
    /// report and they cannot ride the <see cref="Authorizations"/> CSV — hence an explicit join table.
    /// These rows are read live on every access check, so a grant takes effect without a re-login.
    /// </summary>
    public ICollection<AuthorizationGroupReport> Reports { get; set; } = new List<AuthorizationGroupReport>();
}