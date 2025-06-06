namespace Core.DTOs.InventoryCounting;

public class InventoryCountingAddItemResponse : ResponseBase {
    public bool ClosedCounting { get; set; }
    public Guid? LineId { get; set; }
    
    public static InventoryCountingAddItemResponse Success(Guid lineId) => new() { 
        Status = Core.Enums.ResponseStatus.Ok,
        LineId = lineId,
        ClosedCounting = false 
    };
}