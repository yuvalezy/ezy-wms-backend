using Core.Enums;

namespace Core.DTOs.Transfer;

/// <summary>
/// A supervisor's whole-document view of a transfer: what was taken from the source bins and where it landed
/// in the target bins, one row per item. Unlike <see cref="TransferContentResponse"/> — which serves the
/// scanning screen and reports a single side at a time — this reports both sides together.
/// </summary>
public class TransferSummaryReportResponse {
    public Guid Id { get; set; }
    public int Number { get; set; }
    public string? Name { get; set; }
    public DateTime Date { get; set; }
    public ObjectStatus Status { get; set; }
    public string? Comments { get; set; }

    /// <summary>Source warehouse.</summary>
    public required string WhsCode { get; set; }

    public string? TargetWhsCode { get; set; }
    public string? CreatedByUserName { get; set; }

    /// <summary>Null on transfers not yet posted to SAP, and on those posted before the column existed.</summary>
    public int? SapDocNumber { get; set; }

    public List<TransferSummaryReportLine> Lines { get; set; } = [];
}

public class TransferSummaryReportLine {
    public required string ItemCode { get; set; }
    public required string ItemName { get; set; }

    /// <summary>Base pieces selected from the source bins.</summary>
    public decimal SourceQuantity { get; set; }

    /// <summary>Base pieces placed into the target bins.</summary>
    public decimal TargetQuantity { get; set; }

    /// <summary>Source minus target: still to be placed. Negative would mean over-placement.</summary>
    public decimal OpenQuantity { get; set; }

    public List<TransferContentBin> SourceBins { get; set; } = [];
    public List<TransferContentBin> TargetBins { get; set; } = [];

    public decimal NumInBuy { get; set; }
    public string BuyUnitMsr { get; set; } = string.Empty;
    public decimal PurPackUn { get; set; }
    public string PurPackMsr { get; set; } = string.Empty;
    public decimal Factor1 { get; set; }
    public decimal Factor2 { get; set; }
    public decimal Factor3 { get; set; }
    public decimal Factor4 { get; set; }
    public Dictionary<string, object>? CustomFields { get; set; }
}
