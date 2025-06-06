using Core.Enums;

namespace Core.DTOs.InventoryCounting;

public class InventoryCountingsRequest {
    public int? ID { get; set; }
    public DateTime? Date { get; set; }
    public ObjectStatus[]? Statuses { get; set; }
}