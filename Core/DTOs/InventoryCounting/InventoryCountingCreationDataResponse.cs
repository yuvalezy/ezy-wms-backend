namespace Core.DTOs.InventoryCounting;

public class InventoryCountingCreationDataResponse {
    public string ItemCode { get; set; } = string.Empty;
    public int CountedQuantity { get; set; }
    public int SystemQuantity { get; set; }
    public int Variance { get; set; }
    public List<InventoryCountingCreationBinResponse> CountedBins { get; set; } = new();
}

public class InventoryCountingCreationBinResponse {
    public int BinEntry { get; set; }
    public int CountedQuantity { get; set; }
    public int SystemQuantity { get; set; }
}