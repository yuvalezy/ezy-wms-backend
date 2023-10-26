using System;

namespace Service.API.General.Models; 

public class ItemBarCodeParameters {
    public string ItemCode { get; set; }
    public string Barcode  { get; set; }

    public void ValidateAll() {
        if (string.IsNullOrWhiteSpace(ItemCode))
            throw new ArgumentException("Item Code is a mandatory parameter");
        if (string.IsNullOrWhiteSpace(Barcode))
            throw new ArgumentException("Bar Code is a mandatory parameter");
    }
}