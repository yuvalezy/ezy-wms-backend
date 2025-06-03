using System.Runtime.InteropServices;
using Adapters.Windows.SBO.Services;
using Core.Enums;
using Core.Models;
using SAPbobsCOM;

namespace Adapters.Windows.SBO.Helpers;

public class ItemBarCodeUpdate(SboDatabaseService dbService, SboCompany sboCompany, string itemCode, string[]? addBarcodes, string[]? removeBarcodes) : IDisposable {
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
    private Items? item;
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.

    public UpdateItemBarCodeResponse Execute() {
        var      response = new UpdateItemBarCodeResponse();
        Company? company  = null;
        try {
            if (sboCompany.TransactionMutex.WaitOne()) {
                try {
                    sboCompany.ConnectCompany();
                    company = sboCompany.Company!;
                    company.StartTransaction();
                    item = (Items)company.GetBusinessObject(BoObjectTypes.oItems);
                    if (!item.GetByKey(itemCode))
                        throw new ArgumentException($"Item Code {itemCode} not found!");

                    RemoveBarcodes();
                    AddNewBarcodes();

                    if (item.Update() == 0) {
                        response.Status = ResponseStatus.Ok;
                    }
                    else {
                        response.ErrorMessage = company.GetLastErrorDescription();
                        response.Status       = ResponseStatus.Error;
                    }

                    company.EndTransaction(BoWfTransOpt.wf_Commit);
                }
                finally {
                    sboCompany.TransactionMutex.ReleaseMutex();
                }
            }
        }
        catch {
            company?.EndTransaction(BoWfTransOpt.wf_RollBack);
            throw;
        }

        return response;
    }


    private void AddNewBarcodes() {
        if (addBarcodes == null)
            return;
        foreach (string barcode in addBarcodes) {
            if (item!.BarCodes.Count == 0 || !string.IsNullOrWhiteSpace(item.BarCodes.BarCode))
                item.BarCodes.Add();
            item.BarCodes.BarCode  = barcode;
            item.BarCodes.UoMEntry = -1;
        }
    }

    private void RemoveBarcodes() {
        if (removeBarcodes == null)
            return;
        for (int i = item!.BarCodes.Count - 1; i >= 0; i--) {
            item.BarCodes.SetCurrentLine(i);
            string barcode = item.BarCodes.BarCode;
            if (!removeBarcodes.Contains(barcode))
                continue;
            item.BarCodes.Delete();
            if (item!.BarCode.Equals(barcode))
                item.BarCode = null;
        }
    }

    public void Dispose() {
        if (item == null)
            return;
        Marshal.ReleaseComObject(item);
        item = null;
        GC.Collect();
    }
}