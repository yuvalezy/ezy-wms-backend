using Core.Enums;

namespace Core.Models.Settings;

public record Options {
    // General
    public bool EnableUseBaseUn { get; set; } = true;
    public ScannerMode ScannerMode { get; set; } = ScannerMode.ItemBarcode;
    public bool DisplayVendor { get; set; } = true;
    public bool WhsCodeBinSuffix { get; set; }
    // Units
    public string? UnitLabel { get; set; }
    public string? UnitAbbr { get; set; }
    public string? DozensLabel { get; set; }
    public string? DozensAbbr { get; set; }
    public string? BoxLabel { get; set; }
    public string? BoxAbbr { get; set; }
    public UnitType? MaxUnitLevel { get; set; }

    // Goods Receipt
    public bool GoodsReceiptDraft { get; init; }
    public bool GoodsReceiptModificationsRequiredSupervisor { get; init; }
    public bool GoodsReceiptCreateSupervisorRequired { get; init; }
    public GoodsReceiptDocumentType GoodsReceiptType { get; set; } = GoodsReceiptDocumentType.Both;
    public bool GoodsReceiptTargetDocuments { get; set; }
    public bool GoodsReceiptPackages { get; set; } = true;

    public bool GoodsReceiptConfirmationAdjustStock { get; set; }
    public int? GoodsReceiptConfirmationAdjustStockPriceList { get; set; }

    //Transfer
    public bool TransferTargetItems { get; init; }
    public bool EnableTransferConfirm { get; init; }
    public bool EnableTransferRequest { get; init; }
    public bool EnableWarehouseTransfer { get; init; }
    public bool TransferCreateSupervisorRequired { get; init; }

    //Packages & Units
    public bool EnablePackages { get; init; }
    public UnitType DefaultUnitType { get; set; } = UnitType.Pack;
    public bool EnableUnitSelection { get; init; }

    /// <summary>
    /// Idle Settings for auto log out, if null or zero, ignore
    /// </summary>
    public int? IdleLogoutTimeout { get; init; }

    //Pick List
    public bool EnablePickingCheck { get; init; }

    //Quantities
    public bool EnableDecimalQuantities { get; init; }
}