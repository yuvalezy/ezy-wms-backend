using Core.DTOs.Items;
using Core.Enums;
using Core.Models.Settings;

namespace Core.DTOs.Settings;

public class UserInfoResponse {
    public required string                             ID               { get; set; }
    public required string                             Name             { get; set; }
    public          IEnumerable<RoleType>?             Roles            { get; set; }
    public required IEnumerable<WarehouseResponse>     Warehouses       { get; set; }
    public required string                             CurrentWarehouse { get; set; }
    public          bool                               BinLocations     { get; set; }
    public required Options                            Settings         { get; set; }
    public          bool                               SuperUser        { get; set; }
    public          Dictionary<string, CustomField[]>? CustomFields     { get; set; }
}