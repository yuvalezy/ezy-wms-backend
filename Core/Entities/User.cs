using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Core.Entities;

[Table("Users")]
public class User : BaseEntity {
    [Required]
    [MaxLength(50)]
    public required string FullName { get; set; }
    
    [Required]
    [MaxLength(500)]
    public required string Password { get; set; }

    [MaxLength(100)]
    [EmailAddress]
    public string? Email { get; set; }
    
    [MaxLength(100)]
    public string? Position { get; set; }
    
    [Required]
    public bool SuperUser { get; set; }

    [ForeignKey("AuthorizationGroup")]
    public Guid? AuthorizationGroupId { get; set; }

    public AuthorizationGroup? AuthorizationGroup { get; set; }
}