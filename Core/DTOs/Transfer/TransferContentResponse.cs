using Core.Enums;

namespace Core.DTOs.Transfer;

public class TransferContentResponse {
    public string ItemCode { get; set; } = string.Empty;
    public string ItemName { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal OpenQuantity { get; set; }
    public int? BinQuantity { get; set; }
    public int? Progress { get; set; }
    public List<TransferContentBin>? Bins { get; set; }
    public decimal NumInBuy { get; set; }
    public string BuyUnitMsr { get; set; } = string.Empty;
    public decimal PurPackUn { get; set; }
    public string PurPackMsr { get; set; } = string.Empty;
    public decimal Factor1 { get; set; }
    public decimal Factor2 { get; set; }
    public decimal Factor3 { get; set; }
    public decimal Factor4 { get; set; }
    public UnitType Unit { get; set; }
    public Dictionary<string, object>? CustomFields { get; set; }
}

public class TransferContentBin {
    public int Entry { get; set; }
    public string Code { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
}