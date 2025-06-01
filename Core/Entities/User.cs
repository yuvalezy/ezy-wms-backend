using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Core.Entities;

public class User : BaseEntity {
    [Required]
    public required string FullName { get; set; }

    public string? Email     { get; set; }
    public string? Position  { get; set; }
    public bool    SuperUser { get; set; }

    [ForeignKey("AuthorizationGroup")]
    public Guid? AuthorizationGroupId { get; set; }

    public AuthorizationGroup? AuthorizationGroup { get; set; }
}