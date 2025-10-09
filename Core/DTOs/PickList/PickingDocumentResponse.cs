using Core.Enums;

namespace Core.DTOs.PickList;

public class PickingDocumentResponse {
    public int Entry { get; set; }
    public DateTime Date { get; set; }
    public string? SalesOrders { get; set; }
    public string? Invoices { get; set; }
    public string? Transfers { get; set; }
    public string? Remarks { get; set; }
    public ObjectStatus Status { get; set; }
    public decimal Quantity { get; set; }
    public decimal OpenQuantity { get; set; }
    public decimal UpdateQuantity { get; set; }
    public bool PickPackOnly { get; set; }
}