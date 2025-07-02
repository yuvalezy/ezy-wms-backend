namespace Core.Models.Settings;

public record Options {
    public bool GoodsReceiptDraft                           { get; init; }
    public bool GoodsReceiptModificationsRequiredSupervisor { get; init; } 
    public bool GoodsReceiptCreateSupervisorRequired        { get; init; }
    public bool GoodsReceiptTargetDocuments                 { get; set; }
    public bool TransferTargetItems                         { get; init; }
    public bool EnablePackages                              { get; set; }
}