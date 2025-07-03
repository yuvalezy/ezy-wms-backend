using Core.DTOs.Package;
using Core.Enums;

namespace Core.DTOs.GoodsReceipt;

public class GoodsReceiptReportAllDetailsResponse {
    public Guid                  LineId            { get; set; }
    public string                CreatedByUserName { get; set; } = string.Empty;
    public DateTime              TimeStamp         { get; set; }
    public decimal               Quantity          { get; set; }
    public UnitType              Unit              { get; set; }
    public PackageValueResponse? Package           { get; set; }
}