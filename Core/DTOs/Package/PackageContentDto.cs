using Core.DTOs.Items;
using Core.Enums;

namespace Core.DTOs.Package;

public class PackageContentDto {
    public          Guid               Id        { get; set; }
    public          Guid               PackageId { get; set; }
    public required string             ItemCode  { get; set; }
    public          ItemUnitResponse            ItemData  { get; set; }
    public          decimal            Quantity  { get; set; }
    public required string             WhsCode   { get; set; }
    public          string?            BinCode   { get; set; }
    public          DateTime           CreatedAt { get; set; }
    public          UserAuditResponse? CreatedBy { get; set; }
}