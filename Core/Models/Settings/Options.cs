using Core.Enums;

namespace Core.Models.Settings;

public record Options {
    public bool                     GoodsReceiptDraft                           { get; init; }
    public bool                     GoodsReceiptModificationsRequiredSupervisor { get; init; }
    public bool                     GoodsReceiptCreateSupervisorRequired        { get; init; }
    public bool                     GoodsReceiptTargetDocuments                 { get; set; }
    public bool                     TransferTargetItems                         { get; init; }
    public bool                     EnablePackages                              { get; init; }
    public GoodsReceiptDocumentType GoodsReceiptType                            { get; set; } = GoodsReceiptDocumentType.Both;
    public UnitType                 DefaultUnitType                             { get; set; } = UnitType.Pack;
    public bool                     EnableUnitSelection                         { get; init; }
    /// <summary>
    /// Idle Settings for auto log out, if null or zero, ignore
    /// </summary>
    public int?                     IdleLogoutTimeout                           { get; set; }
}