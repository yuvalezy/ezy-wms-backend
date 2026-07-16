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

    /// <summary>
    /// Report definitions this group may run. Not a <see cref="RoleType"/> — reports are runtime data, so
    /// they are granted through a join table rather than the <see cref="Authorizations"/> CSV. Optional and
    /// empty by default, so a caller that predates reports keeps working.
    /// </summary>
    public ICollection<Guid> ReportIds { get; set; } = new List<Guid>();
}