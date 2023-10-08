using System.Collections.Generic;

namespace Service.API.GoodsReceipt.Models; 

public class GoodsReceiptVSExitReport {
    public int                                ObjectType { get; }
    public int                                Number     { get; }
    public string                             CardName   { get; }
    public string                             Address    { get; }
    public List<GoodsReceiptVSExitReportLine> Lines      { get; } = new();

    public GoodsReceiptVSExitReport(int objectType, int number, string cardName, string address) {
        ObjectType   = objectType;
        Number       = number;
        CardName     = cardName;
        Address = address;
    }
}
public class GoodsReceiptVSExitReportLine {
    public string ItemCode     { get; }
    public string ItemName     { get; }
    public double OpenQuantity { get; }
    public double Quantity     { get; }

    public GoodsReceiptVSExitReportLine(string itemCode, string itemName, double openQuantity, double quantity) {
        ItemCode      = itemCode;
        ItemName      = itemName;
        OpenQuantity  = openQuantity;
        Quantity = quantity;
    }
}
