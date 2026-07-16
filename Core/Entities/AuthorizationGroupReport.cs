using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Core.Entities;

/// <summary>
/// Explicit join granting one <see cref="AuthorizationGroup"/> access to one <see cref="ReportDefinition"/>.
/// </summary>
/// <remarks>
/// <para>
/// This has to be a join table rather than new <c>RoleType</c> values: <c>AuthorizationGroup.Authorizations</c>
/// is a static C# enum persisted as a CSV of ints, and reports are created at <b>runtime</b>, so there is no
/// enum value to mint per report. No new <c>RoleType</c> is added by this feature and the CSV column is untouched.
/// </para>
/// <para>
/// Access is checked <b>live against these rows</b> on every list/run call, never against the login-time
/// <c>SessionInfo.Roles</c> snapshot — so a grant takes effect immediately and a newly created report isn't
/// invisible to everyone already logged in.
/// </para>
/// <para>Composite primary key (<see cref="AuthorizationGroupId"/>, <see cref="ReportDefinitionId"/>).</para>
/// </remarks>
[Table("AuthorizationGroupReports")]
public class AuthorizationGroupReport {
    [Required]
    public Guid AuthorizationGroupId { get; set; }

    [Required]
    public Guid ReportDefinitionId { get; set; }

    // Navigation properties
    public AuthorizationGroup AuthorizationGroup { get; set; } = null!;

    public ReportDefinition ReportDefinition { get; set; } = null!;
}
