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
    /// Whether this user can see at least one report. Resolved by a live EXISTS on every call, so a grant
    /// shows up without a re-login — and it rides this response (already fetched on every refresh) so the
    /// Reports menu entry can hide itself with no extra request.
    /// </summary>
    public bool HasReports { get; set; }
    public Dictionary<string, CustomField[]>? CustomFields { get; set; }
    public DeviceStatus? DeviceStatus { get; set; }
    public string? DeviceName { get; set; }
    public required MetadataDefinition[] ItemMetaData { get; set; }
}
