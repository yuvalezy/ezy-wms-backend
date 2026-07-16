using System.ComponentModel.DataAnnotations;
using Core.Enums;

namespace Core.DTOs.Settings;

public class UpdateAuthorizationGroupRequest {
    [Required]
    public Guid Id { get; set; }
    
    [Required]
    [MaxLength(50)]
    public required string Name { get; set; }
    
    [MaxLength(200)]
    public string? Description { get; set; }

    [Required]
    [MinLength(1, ErrorMessage = "At least one authorization is required")]
    public required ICollection<RoleType> Authorizations { get; set; }

    /// <summary>
    /// Report definitions this group may run; replaces the group's grants wholesale. Empty clears them.
    /// Unlike <see cref="Authorizations"/> (which <c>SessionInfo</c> snapshots at login), these take effect
    /// immediately — every access check reads the join table live.
    /// </summary>
    public ICollection<Guid> ReportIds { get; set; } = new List<Guid>();
}