namespace Core.DTOs.InventoryCounting;

public class InventoryCountingCreationDataResponse {
    public string ItemCode { get; set; } = string.Empty;
    public decimal CountedQuantity { get; set; }
    public decimal SystemQuantity { get; set; }
    public decimal Variance { get; set; }
    public List<InventoryCountingCreationBinResponse> CountedBins { get; set; } = new();
}

public class InventoryCountingCreationBinResponse {
    public int BinEntry { get; set; }
    public decimal CountedQuantity { get; set; }
    public decimal SystemQuantity { get; set; }
}