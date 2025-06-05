using Core.Enums;

namespace Core.DTOs;

public class GoodsReceiptsRequest {
    public int? ID { get; set; }
    public DateTime? Date { get; set; }
    public string? CardCode { get; set; }
    public ObjectStatus[]? Statuses { get; set; }
    public bool? Confirm { get; set; }
    public string? WhsCode { get; set; }
}