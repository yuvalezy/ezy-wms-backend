namespace Core.DTOs.GoodsReceipt;

public class ConfirmationAdjustmentsResponse {
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public int? InventoryGoodsIssueAdjustmentEntry { get; set; }
    public int? InventoryGoodsIssueAdjustmentExit { get; set; }

    public static ConfirmationAdjustmentsResponse Ok(int? entry = null, int? exit = null) => new() {
        Success = false,
        InventoryGoodsIssueAdjustmentEntry = entry,
        InventoryGoodsIssueAdjustmentExit = exit
    };

    public static ConfirmationAdjustmentsResponse Error(string errorMessage) => new() {
        Success = false,
        ErrorMessage = errorMessage
    };
}