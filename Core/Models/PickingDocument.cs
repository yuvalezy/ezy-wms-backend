using Core.Enums;

namespace Core.Models;

public class PickingDocument {
    public int Entry { get; set; }
    public DateTime Date { get; set; }
    public int SalesOrders { get; set; }
    public int Invoices { get; set; }
    public int Transfers { get; set; }
    public string? Remarks { get; set; }
    public ObjectStatus Status { get; set; }
    public int Quantity { get; set; }
    public int OpenQuantity { get; set; }
    public int UpdateQuantity { get; set; }
}