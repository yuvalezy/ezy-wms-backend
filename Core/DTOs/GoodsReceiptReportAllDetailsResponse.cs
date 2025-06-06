using Core.Enums;

namespace Core.DTOs;

public class GoodsReceiptReportAllDetailsResponse {
    public Guid     LineId            { get; set; }
    public string   CreatedByUserName { get; set; } = string.Empty;
    public DateTime TimeStamp         { get; set; }

    public decimal  Quantity { get; set; }
    public UnitType Unit     { get; set; }
}