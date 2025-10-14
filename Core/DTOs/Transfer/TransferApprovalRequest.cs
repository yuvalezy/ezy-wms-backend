namespace Core.DTOs.Transfer;

public class TransferApprovalRequest {
    public Guid TransferId { get; set; }
    public bool Approved { get; set; }
    public string? RejectionReason { get; set; }
}
