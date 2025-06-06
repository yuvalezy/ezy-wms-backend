using System.ComponentModel.DataAnnotations;
using Core.Enums;

namespace Core.DTOs.Settings;

public class CreateAuthorizationGroupRequest {
    [Required]
    [MaxLength(50)]
    public required string Name { get; set; }
    
    [MaxLength(200)]
    public string? Description { get; set; }

    [Required]
    [MinLength(1, ErrorMessage = "At least one authorization is required")]
    public required ICollection<RoleType> Authorizations { get; set; }
}