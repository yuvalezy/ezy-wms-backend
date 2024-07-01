using System.Collections.Generic;

namespace Service.API.GoodsReceipt.Models;

public class GoodsReceiptVSExitReport(int objectType, int number, string cardName, string address) {
    public int                                ObjectType { get; } = objectType;
    public int                                Number     { get; } = number;
    public string                             CardName   { get; } = cardName;
    public string                             Address    { get; } = address;
    public List<GoodsReceiptVSExitReportLine> Lines      { get; } = new();
}

public class GoodsReceiptVSExitReportLine(string itemCode, string itemName, double openQuantity, double quantity) {
    public string ItemCode     { get; } = itemCode;
    public string ItemName     { get; } = itemName;
    public double OpenQuantity { get; } = openQuantity;
    public double Quantity     { get; } = quantity;
}