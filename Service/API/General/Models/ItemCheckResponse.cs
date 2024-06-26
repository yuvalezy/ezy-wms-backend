using System.Collections.Generic;

namespace Service.API.General.Models;

public class ItemCheckResponse(string itemCode, string itemName, int purPackUn) {
    public string ItemCode  { get; set; } = itemCode;
    public string ItemName  { get; set; } = itemName;
    public int    PurPackUn { get; set; } = purPackUn;
    public List<string> Barcodes { get; set; } = [];
}

public class UpdateItemBarCodeResponse : ResponseBase {
    public string ExistItem { get; set; }
}