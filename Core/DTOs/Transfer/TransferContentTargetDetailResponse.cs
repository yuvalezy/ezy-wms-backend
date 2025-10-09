namespace Core.DTOs.Transfer;

public class TransferContentTargetDetailResponse {
    public Guid LineId { get; set; }
    public string CreatedByName { get; set; } = string.Empty;
    public DateTime TimeStamp { get; set; }
    public decimal Quantity { get; set; }
}