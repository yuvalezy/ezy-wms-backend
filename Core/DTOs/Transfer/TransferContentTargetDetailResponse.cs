namespace Core.DTOs.Transfer;

public class TransferContentTargetDetailResponse {
    public Guid LineID { get; set; }
    public string CreatedByName { get; set; } = string.Empty;
    public DateTime TimeStamp { get; set; }
    public int Quantity { get; set; }
}