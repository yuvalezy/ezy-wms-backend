using Core.Entities;
using Core.Enums;

namespace Core.DTOs;

public class TransferLineResponse {
    public required string BarCode  { get; set; }
    public required string ItemCode { get; set; }

    public Guid         Id                     { get; set; }
    public int          Quantity               { get; set; }
    public LineStatus   LineStatus             { get; set; }
    public SourceTarget Type                   { get; set; }
    public UnitType     UnitType               { get; set; }
    public DateTime     Date                   { get; set; }
    public int?         BinEntry               { get; set; }
    public string?      Comments               { get; set; }
    public int?         StatusReason           { get; set; }
    public Guid?        CancellationReasonId   { get; set; }
    public string?      CancellationReasonName { get; set; } // Flattened from CancellationReason

    // Include basic transfer info if needed (flattened, no circular reference)
    public Guid    TransferId     { get; set; }
    public int?    TransferNumber { get; set; }
    public string? TransferName   { get; set; }

    // Audit fields
    public DateTime  CreatedAt         { get; set; }
    public Guid?     CreatedByUserId   { get; set; }
    public string?   CreatedByUserName { get; set; } // Flattened from User
    public DateTime? UpdatedAt         { get; set; }
    public Guid?     UpdatedByUserId   { get; set; }
    public string?   UpdatedByUserName { get; set; } // Flattened from User

    public static TransferLineResponse FromTransferLine(TransferLine line, bool includeTransferInfo = false) {
        var response = new TransferLineResponse {
            Id                     = line.Id,
            BarCode                = line.BarCode,
            ItemCode               = line.ItemCode,
            Quantity               = line.Quantity,
            LineStatus             = line.LineStatus,
            Type                   = line.Type,
            UnitType               = line.UnitType,
            Date                   = line.Date,
            BinEntry               = line.BinEntry,
            Comments               = line.Comments,
            StatusReason           = line.StatusReason,
            CancellationReasonId   = line.CancellationReasonId,
            CancellationReasonName = line.CancellationReason?.Name, // Assuming CancellationReason has a Name property
            TransferId             = line.TransferId,
            CreatedAt              = line.CreatedAt,
            CreatedByUserId        = line.CreatedByUserId,
            CreatedByUserName      = line.CreatedByUser?.FullName, // Assuming User has a Name property
            UpdatedAt              = line.UpdatedAt,
            UpdatedByUserId        = line.UpdatedByUserId,
            UpdatedByUserName      = line.UpdatedByUser?.FullName
        };

        if (includeTransferInfo && line.Transfer != null) {
            response.TransferNumber = line.Transfer.Number;
            response.TransferName   = line.Transfer.Name;
        }

        return response;
    }

    // Helper method for converting collections
    public static List<TransferLineResponse> FromTransferLines(IEnumerable<TransferLine> lines, bool includeTransferInfo = false) {
        return lines.Select(l => FromTransferLine(l, includeTransferInfo)).ToList();
    }
}