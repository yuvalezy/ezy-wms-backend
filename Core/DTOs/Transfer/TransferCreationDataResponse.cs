namespace Core.DTOs.Transfer;

public class TransferCreationDataResponse {
    public string ItemCode { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public List<TransferCreationBinResponse> SourceBins { get; set; } = new();
    public List<TransferCreationBinResponse> TargetBins { get; set; } = new();
}

public class TransferCreationBinResponse {
    public int BinEntry { get; set; }
    public decimal Quantity { get; set; }
}