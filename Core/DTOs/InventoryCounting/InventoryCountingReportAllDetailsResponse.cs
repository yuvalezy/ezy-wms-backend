using Core.DTOs.Package;
using Core.Enums;

namespace Core.DTOs.InventoryCounting;

public class InventoryCountingReportAllDetailsResponse {
    public Guid                  LineId            { get; set; }
    public string                CreatedByUserName { get; set; } = string.Empty;
    public DateTime              TimeStamp         { get; set; }
    public decimal               Quantity          { get; set; }
    public UnitType              Unit              { get; set; }
    public string?               BinCode           { get; set; }
    public PackageValueResponse? Package           { get; set; }
}
