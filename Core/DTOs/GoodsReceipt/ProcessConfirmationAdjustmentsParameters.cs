namespace Core.DTOs.GoodsReceipt;

public record ProcessConfirmationAdjustmentsParameters(
    int Number,
    string Warehouse,
    bool EnableBinLocation,
    int? DefaultBinLocation,
    List<(string ItemCode, decimal Quantity)> NegativeItems,
    List<(string ItemCode, decimal Quantity)> PositiveItems) {
    public Dictionary<string, decimal>? ItemsCost { get; set; }
}