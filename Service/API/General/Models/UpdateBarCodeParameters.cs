using System;
using System.Linq;

namespace Service.API.General.Models;

public class UpdateBarCodeParameters {
    public string   ItemCode       { get; set; }
    public string[] AddBarcodes    { get; set; }
    public string[] RemoveBarcodes { get; set; }

    public UpdateItemBarCodeResponse Validate(Data data) {
        if (string.IsNullOrWhiteSpace(ItemCode))
            throw new ArgumentException("Item Code is a mandatory parameter");
        if ((AddBarcodes == null || AddBarcodes.Length == 0) && (RemoveBarcodes == null || RemoveBarcodes.Length == 0))
            throw new ArgumentException("New barcode provided to add or remove");
        return AddBarcodes == null
            ? null
            : (from barcode in AddBarcodes
                select data.General.ScanItemBarCode(barcode).FirstOrDefault()
                into item
                where item != null
                select new UpdateItemBarCodeResponse { ExistItem = item.Code, Status = ResponseStatus.Error }).FirstOrDefault();
    }
}