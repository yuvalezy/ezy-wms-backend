namespace Core.DTOs;

public class CancellationReasonResponse {
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool Transfer { get; set; }
    public bool GoodsReceipt { get; set; }
    public bool Counting { get; set; }
    public bool IsEnabled { get; set; }
    public bool CanDelete { get; set; }
}