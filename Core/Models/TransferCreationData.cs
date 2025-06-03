namespace Core.Models;

public class TransferCreationData {
    public string ItemCode { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public List<TransferCreationBin> SourceBins { get; set; } = new();
    public List<TransferCreationBin> TargetBins { get; set; } = new();
}

public class TransferCreationBin {
    public int BinEntry { get; set; }
    public decimal Quantity { get; set; }
}