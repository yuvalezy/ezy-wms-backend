using System;
using System.Collections.Generic;
using System.Data;

namespace Service.API.General.Models;

public class ItemCheckResponse {
    public string       ItemCode   { get; set; }
    public string       ItemName   { get; set; }
    public int          NumInBuy   { get; set; }
    public string       BuyUnitMsr { get; set; }
    public int          PurPackUn  { get; set; }
    public string       PurPackMsr { get; set; }
    public List<string> Barcodes   { get; set; } = [];
}

public class UpdateItemBarCodeResponse : ResponseBase {
    public string ExistItem { get; set; }
}