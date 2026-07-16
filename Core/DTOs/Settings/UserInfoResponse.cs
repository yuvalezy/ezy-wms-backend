using Core.DTOs.Items;
using Core.Enums;
using Core.Models.Settings;

namespace Core.DTOs.Settings;

public class UserInfoResponse
{
    public required string ID { get; set; }
    public required string Name { get; set; }
    public IEnumerable<RoleType>? Roles { get; set; }
    public required IEnumerable<WarehouseResponse> Warehouses { get; set; }
    public required string CurrentWarehouse { get; set; }
    public bool BinLocations { get; set; }
    public required Options Settings { get; set; }
    public bool SuperUser { get; set; }

    /// <summary>
    /// The reports this user may run, name-ordered — one menu entry each, rather than a single link into an
    /// index. Resolved live on every call, so a grant (or a newly created report) shows up on the user's next
    /// refresh instead of their next login, and it rides this response — already fetched on every refresh — so
    /// the menu costs no extra request.
    /// </summary>
    /// <remarks>
    /// Deliberately not the full <c>ReportDefinitionSummary</c>: this is on the hot path for every screen, and
    /// a menu needs a label and a destination, nothing else. Empty means the Reports group disappears.
    /// </remarks>
    public IReadOnlyList<UserReportLink> Reports { get; set; } = [];
    public Dictionary<string, CustomField[]>? CustomFields { get; set; }
    public DeviceStatus? DeviceStatus { get; set; }
    public string? DeviceName { get; set; }
    public required MetadataDefinition[] ItemMetaData { get; set; }
}

/// <summary>
/// One report as the navigation menu needs it: a label and a destination.
/// </summary>
public class UserReportLink {
    public required string Slug { get; set; }
    public required string Name { get; set; }
}
