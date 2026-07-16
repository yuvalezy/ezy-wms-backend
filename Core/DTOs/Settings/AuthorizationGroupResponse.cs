
using Core.Enums;

namespace Core.DTOs.Settings;

public class AuthorizationGroupResponse {
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public ICollection<RoleType> Authorizations { get; set; } = new List<RoleType>();

    /// <summary>
    /// Report definitions granted to this group. Only live (non-deleted) reports are listed.
    /// </summary>
    public ICollection<Guid> ReportIds { get; set; } = new List<Guid>();

    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public bool CanDelete { get; set; }
}