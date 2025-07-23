using Core.Enums;

namespace Core.Models.Settings;

public record Options
{
    // Goods Receipt
    public bool GoodsReceiptDraft { get; init; }
    public bool GoodsReceiptModificationsRequiredSupervisor { get; init; }
    public bool GoodsReceiptCreateSupervisorRequired { get; init; }
    public GoodsReceiptDocumentType GoodsReceiptType { get; set; } = GoodsReceiptDocumentType.Both;
    public bool GoodsReceiptTargetDocuments { get; set; }
    public bool GoodsReceiptPackages { get; set; } = true;

    //Transfer
    public bool TransferTargetItems { get; init; }

    //Packages & Units
    public bool EnablePackages { get; init; }
    public UnitType DefaultUnitType { get; set; } = UnitType.Pack;
    public bool EnableUnitSelection { get; init; }

    /// <summary>
    /// Idle Settings for auto log out, if null or zero, ignore
    /// </summary>
    public int? IdleLogoutTimeout { get; set; }

    //Pick List
    public bool EnablePickingCheck { get; set; }
}