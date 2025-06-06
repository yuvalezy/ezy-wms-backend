using Core.Enums;

namespace Core.DTOs.Transfer;

public class TransferContentResponse {
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public int OpenQuantity { get; set; }
    public int? BinQuantity { get; set; }
    public int? Progress { get; set; }
    public List<TransferContentBin>? Bins { get; set; }
    public int NumInBuy { get; set; }
    public string BuyUnitMsr { get; set; } = string.Empty;
    public int PurPackUn { get; set; }
    public string PurPackMsr { get; set; } = string.Empty;
    public UnitType Unit { get; set; }
}

public class TransferContentBin {
    public int Entry { get; set; }
    public string Code { get; set; } = string.Empty;
    public int Quantity { get; set; }
}