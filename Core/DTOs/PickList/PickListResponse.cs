using Core.Entities;
using Core.Enums;

namespace Core.DTOs.PickList;

public class PickListResponse {
    public int Entry { get; set; }
    public DateTime Date { get; set; }
    public string? SalesOrders { get; set; }
    public string? Invoices { get; set; }
    public string? Transfers { get; set; }
    public string? Remarks { get; set; }
    public ObjectStatus Status { get; set; }
    public SyncStatus SyncStatus { get; set; }
    public decimal Quantity { get; set; }
    public decimal OpenQuantity { get; set; }
    public decimal UpdateQuantity { get; set; }
    public List<PickListDetailResponse>? Detail { get; set; }
    public bool PickPackOnly { get; set; }
    public bool CheckStarted { get; set; }
    public bool HasCheck { get; set; }
}

public class PickListDetailResponse {
    public int Type { get; set; }
    public int Entry { get; set; }
    public int Number { get; set; }
    public DateTime Date { get; set; }
    public string CardCode { get; set; } = string.Empty;
    public string CardName { get; set; } = string.Empty;
    public decimal TotalItems { get; set; }
    public decimal TotalOpenItems { get; set; }
    public List<PickListDetailItemResponse>? Items { get; set; }
    public Dictionary<string, object>? CustomFields { get; set; }
}

public class PickListDetailItemResponse {
    public string ItemCode { get; set; } = string.Empty;
    public string ItemName { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal Picked { get; set; }
    public decimal OpenQuantity { get; set; }
    public decimal NumInBuy { get; set; }
    public string BuyUnitMsr { get; set; } = string.Empty;
    public decimal PurPackUn { get; set; }
    public string PurPackMsr { get; set; } = string.Empty;
    public decimal Factor1 { get; set; }
    public decimal Factor2 { get; set; }
    public decimal Factor3 { get; set; }
    public decimal Factor4 { get; set; }
    public List<BinLocationQuantityResponse>? BinQuantities { get; set; }
    public decimal? Available { get; set; }

    public required Dictionary<string, object> CustomFields { get; set; }
    public BinLocationPackageQuantityResponse[]? Packages { get; set; }
}

public class BinLocationQuantityResponse {
    public int Entry { get; set; }
    public string Code { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public BinLocationPackageQuantityResponse[]? Packages { get; set; }
}

public record BinLocationPackageQuantityResponse(Guid Id, string Barcode, int BinEntry, string ItemCode, decimal Quantity) {
    public bool FullPackage { get; set; }
}