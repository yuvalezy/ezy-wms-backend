using Core.Enums;

namespace Core.DTOs.GoodsReceipt;

public class GoodsReceiptValidateProcessLineDetailsResponse {
    public          DateTime TimeStamp         { get; set; }
    public required string   CreatedByUserName { get; set; }
    public          decimal  Quantity          { get; set; }
    public          decimal  ScannedQuantity   { get; set; }
    public          UnitType Unit              { get; set; }
}