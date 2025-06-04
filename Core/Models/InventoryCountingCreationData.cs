namespace Core.Models;

public class InventoryCountingCreationData {
    public string ItemCode { get; set; } = string.Empty;
    public int CountedQuantity { get; set; }
    public int SystemQuantity { get; set; }
    public int Variance { get; set; }
    public List<InventoryCountingCreationBin> CountedBins { get; set; } = new();
}

public class InventoryCountingCreationBin {
    public int BinEntry { get; set; }
    public int CountedQuantity { get; set; }
    public int SystemQuantity { get; set; }
}