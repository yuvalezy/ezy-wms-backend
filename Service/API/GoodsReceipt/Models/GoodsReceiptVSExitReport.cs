using System.Collections.Generic;
using Core.Enums;
using Service.API.General;

namespace Service.API.GoodsReceipt.Models;

public class GoodsReceiptVSExitReport(int objectType, int number, string cardName, string address) {
    public int                                ObjectType { get; } = objectType;
    public int                                Number     { get; } = number;
    public string                             CardName   { get; } = cardName;
    public string                             Address    { get; } = address;
    public List<GoodsReceiptVSExitReportLine> Lines      { get; } = [];
}

public class GoodsReceiptVSExitReportLine {
    public string   ItemCode     { get; set; }
    public string   ItemName     { get; set; }
    public double   OpenQuantity { get; set; }
    public double   Quantity     { get; set; }
    public int      NumInBuy     { get; set; }
    public string   BuyUnitMsr   { get; set; }
    public int      PurPackUn    { get; set; }
    public string   PurPackMsr   { get; set; }
    public UnitType Unit         { get; set; }
}