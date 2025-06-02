using System;
using System.Collections.Generic;
using Service.API.General;

namespace Service.API.GoodsReceipt.Models;

public class GoodsReceiptValidateProcess {
    public int    DocumentNumber { get; set; }
    public string CardCode       { get; set; }
    public string CardName       { get; set; }
    public int    BaseType       { get; set; }
    public int    BaseEntry      { get; set; }

    public List<GoodsReceiptValidateProcessLine> Lines { get; } = [];
}

public class GoodsReceiptValidateProcessLine {
    public int      LineNumber { get; set; }
    public string   ItemCode   { get; set; }
    public string   ItemName   { get; set; }
    public decimal  Quantity   { get; set; }
    public int      BaseLine   { get; set; }
    public decimal  OpenInvQty { get; set; }
    public int      NumInBuy   { get; set; }
    public string   BuyUnitMsr { get; set; }
    public int      PurPackUn  { get; set; }
    public string   PurPackMsr { get; set; }
    public UnitType UnitType   { get; set; }

    public GoodsReceiptValidateProcessLineStatus LineStatus { get; set; }
}

public class GoodsReceiptValidateProcessLineDetails {
    public DateTime TimeStamp       { get; set; }
    public string   Employee        { get; set; }
    public decimal  Quantity        { get; set; }
    public decimal  ScannedQuantity { get; set; }
    public UnitType Unit            { get; set; }
}

public class GoodsReceiptValidateProcessLineDetailsParameters {
    public int ID        { get; set; }
    public int BaseType  { get; set; }
    public int BaseEntry { get; set; }
    public int BaseLine  { get; set; }
}

public enum GoodsReceiptValidateProcessLineStatus {
    OK          = 0,
    LessScan    = 1,
    MoreScan    = 2,
    ClosedLine  = 3,
    NotReceived = 4
}