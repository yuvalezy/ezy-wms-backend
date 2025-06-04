using Core.Entities;
using Core.Enums;

namespace Core.DTOs;

public class InventoryCountingResponse {
    public Guid Id { get; set; }
    public int Number { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public UserInfoResponse? Employee { get; set; }
    public ObjectStatus Status { get; set; }
    public DateTime StatusDate { get; set; }
    public UserInfoResponse? StatusEmployee { get; set; }
    public string WhsCode { get; set; } = string.Empty;
    public bool Error { get; set; }
    public int ErrorCode { get; set; }
    public object[]? ErrorParameters { get; set; }
    public List<InventoryCountingLineResponse>? Lines { get; set; }

    public static InventoryCountingResponse FromEntity(InventoryCounting counting) {
        return new InventoryCountingResponse {
            Id = counting.Id,
            Number = counting.Number,
            Name = counting.Name ?? string.Empty,
            Date = counting.Date,
            Status = counting.Status,
            StatusDate = counting.UpdatedAt ?? counting.CreatedAt,
            WhsCode = counting.WhsCode,
            Lines = counting.Lines?.Select(InventoryCountingLineResponse.FromEntity).ToList()
        };
    }
}

public class InventoryCountingLineResponse {
    public Guid Id { get; set; }
    public string BarCode { get; set; } = string.Empty;
    public int? BinEntry { get; set; }
    public string? Comments { get; set; }
    public DateTime Date { get; set; }
    public string ItemCode { get; set; } = string.Empty;
    public LineStatus LineStatus { get; set; }
    public int Quantity { get; set; }
    public int? StatusReason { get; set; }
    public Guid? CancellationReasonId { get; set; }
    public UnitType Unit { get; set; }

    public static InventoryCountingLineResponse FromEntity(InventoryCountingLine line) {
        return new InventoryCountingLineResponse {
            Id = line.Id,
            BarCode = line.BarCode,
            BinEntry = line.BinEntry,
            Comments = line.Comments,
            Date = line.Date,
            ItemCode = line.ItemCode,
            LineStatus = line.LineStatus,
            Quantity = line.Quantity,
            StatusReason = line.StatusReason,
            CancellationReasonId = line.CancellationReasonId,
            Unit = line.Unit
        };
    }
}