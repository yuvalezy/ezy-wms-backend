using Core.DTOs.General;

namespace Core.DTOs.InventoryCounting;

public class InventoryCountingAddItemResponse : ResponseBase {
    public bool ClosedCounting { get; set; }
    public Guid? LineId { get; set; }
    
    
    // Package-related properties
    public Guid? PackageId { get; set; }
    public string? PackageBarcode { get; set; }
    
    public static InventoryCountingAddItemResponse Success(Guid lineId) => new() { 
        Status = Enums.ResponseStatus.Ok,
        LineId = lineId,
        ClosedCounting = false 
    };
}