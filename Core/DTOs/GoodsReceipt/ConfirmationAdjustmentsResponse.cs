namespace Core.DTOs.GoodsReceipt;

public class ConfirmationAdjustmentsResponse {
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public ConfirmationAdjustmentsResponseNumber? InventoryGoodsIssueAdjustmentEntry { get; set; }
    public ConfirmationAdjustmentsResponseNumber? InventoryGoodsIssueAdjustmentExit { get; set; }

    public static ConfirmationAdjustmentsResponse Ok(ConfirmationAdjustmentsResponseNumber? entry = null, ConfirmationAdjustmentsResponseNumber? exit = null) => new() {
        Success = true,
        InventoryGoodsIssueAdjustmentEntry = entry,
        InventoryGoodsIssueAdjustmentExit = exit
    };

    public static ConfirmationAdjustmentsResponse Error(string errorMessage) => new() {
        Success = false,
        ErrorMessage = errorMessage
    };
}
public record ConfirmationAdjustmentsResponseNumber(int Number, int Entry);