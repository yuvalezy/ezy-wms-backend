using Core.Enums;

namespace Core.DTOs.GoodsReceipt;

public class GoodsReceiptsRequest {
    public string?   Name     { get; set; }
    public int?      Number   { get; set; }
    public DateTime? Date     { get; set; }
    public DateTime? DateFrom { get; set; }
    public DateTime? DateTo { get; set; }
    public string?         Vendor { get; set; }
    public ObjectStatus[]? Statuses { get; set; }
    public bool?           Confirm  { get; set; }
    public string?         WhsCode  { get; set; }
}