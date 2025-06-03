using System.Collections.Generic;
using Core.Enums;
using Service.API.General;

namespace Service.API.Transfer.Models;

public class TransferContent {
    public string                   Code         { get; set; }
    public string                   Name         { get; set; }
    public int                      Quantity     { get; set; }
    public int                      OpenQuantity { get; set; }
    public int?                     BinQuantity  { get; set; }
    public int?                     Progress     { get; set; }
    public List<TransferContentBin> Bins         { get; set; }
    public int                      NumInBuy     { get; set; }
    public string                   BuyUnitMsr   { get; set; }
    public int                      PurPackUn    { get; set; }
    public string                   PurPackMsr   { get; set; }
    public UnitType                 Unit         { get; set; }
}

public class TransferContentBin {
    public int    Entry    { get; set; }
    public string Code     { get; set; }
    public int    Quantity { get; set; }
}