using System.Collections.Generic;

namespace Service.API.General.Models;

public class ItemCheckResponse {
    public string       ItemCode { get; set; }
    public string       ItemName { get; set; }
    public int          NumInBuy { get; set; }
    public List<string> Barcodes { get; set; } = new();
}

public class UpdateItemBarCodeResponse : ResponseBase {
    public string ExistItem { get; set; }
}