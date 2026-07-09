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

    public bool GoodsReceiptConfirmationAdjustStock { get; set; }
    public int? GoodsReceiptConfirmationAdjustStockPriceList { get; set; }

    //Transfer
    public bool TransferTargetItems { get; init; }
    public bool EnableTransferConfirm { get; init; }
    public bool EnableTransferRequest { get; init; }
    public bool EnableWarehouseTransfer { get; init; }
    public bool TransferCreateSupervisorRequired { get; init; }
    public bool DirectTransferAll { get; init; }

    // Units
    public UnitType DefaultUnitType { get; set; } = UnitType.Pack;
    public bool EnableUnitSelection { get; init; }

    /// <summary>
    /// Idle Settings for auto log out, if null or zero, ignore
    /// </summary>
    public int? IdleLogoutTimeout { get; init; }

    //Pick List
    public bool EnablePickingCheck { get; init; }
    public bool EnablePickingPackageLabels { get; init; }
    public bool EnablePostPickRepack { get; init; }
    public string PickingPackageLabelPrefix { get; init; } = "R";

    /// <summary>
    /// When enabled, the picking detail screen presents items as a guided, location-first walk
    /// ordered by bin code (pick-path routing). Default off preserves the item-first view.
    /// </summary>
    public bool EnablePickPathRouting { get; init; }

    /// <summary>
    /// Pick-path walk-order tuning (only used when EnablePickPathRouting is on). The frontend
    /// comparator understands the bin-code structure (zone / aisle+floor / level / section) and
    /// walks floor-1 aisles, then the hueco racks, then floor-2 aisles, sections ascending within
    /// each aisle. This is the aisle letter-part that identifies the hueco/mezzanine racks that
    /// walk BETWEEN the floors (e.g. "HU" for HU1..HU4). Empty disables hueco grouping. Default "HU".
    /// </summary>
    public string? PickPathHuecoPrefix { get; init; } = "HU";

    /// <summary>
    /// Pick-path within-aisle order (only used when EnablePickPathRouting is on). True walks the
    /// aisle once taking every shelf level at each bay (section-major: A1-00-01, A1-10-01, ...,
    /// A1-00-02, ...). False finishes a whole shelf level across the aisle before the next level.
    /// Default true.
    /// </summary>
    public bool PickPathSectionFirst { get; init; } = true;

    /// <summary>
    /// When enabled, blocks picking (at scan/AddItem time) of an item that belongs to a
    /// non-finalized goods receipt in the same warehouse. Prevents the "phantom inventory"
    /// caused by picking stock that has not yet been posted to SAP.
    /// </summary>
    public bool BlockPickScanDuringGoodsReceipt { get; init; }

    /// <summary>
    /// When enabled, blocks processing a pick list to SAP if any of its items belongs to a
    /// non-finalized goods receipt in the same warehouse. Safety net for the window where the
    /// receipt enters put-away after the item was already scanned.
    /// </summary>
    public bool BlockPickProcessDuringGoodsReceipt { get; init; }

    //Quantities
    public bool EnableDecimalQuantities { get; init; }

    // Inventory Counting
    public int InventoryCountingBatchSize { get; set; } = 200;

    // Per-document-type unit overrides (optional)
    public Dictionary<ObjectType, DocumentUnitSettings>? DocumentUnitOverrides { get; set; }

    public DocumentUnitSettings GetUnitSettingsFor(ObjectType objectType)
    {
        var overrides = DocumentUnitOverrides?.GetValueOrDefault(objectType);
        return new DocumentUnitSettings
        {
            DefaultUnitType = overrides?.DefaultUnitType ?? DefaultUnitType,
            EnableUnitSelection = overrides?.EnableUnitSelection ?? EnableUnitSelection,
            EnableUseBaseUn = overrides?.EnableUseBaseUn ?? EnableUseBaseUn,
            MaxUnitLevel = overrides?.MaxUnitLevel ?? MaxUnitLevel,
        };
    }
}
