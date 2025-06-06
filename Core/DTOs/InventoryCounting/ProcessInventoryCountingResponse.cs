using Core.Enums;

namespace Core.DTOs.InventoryCounting;

public class ProcessInventoryCountingResponse {
    public bool Success { get; set; }
    public ResponseStatus Status { get; set; }
    public string? ErrorMessage { get; set; }
    public int? ExternalEntry { get; set; }
    public int? ExternalNumber { get; set; }
}