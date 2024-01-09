using System;
using System.Linq;
using System.Runtime.InteropServices;
using SAPbobsCOM;
using Service.API.General.Models;
using Service.Shared.Company;

namespace Service.API.General;

public class ItemBarCodeUpdate : IDisposable {
    private readonly int      employeeID;
    private readonly string   itemCode;
    private readonly string[] addBarcodes;
    private readonly string[] removeBarcodes;

    private Items item;

    public ItemBarCodeUpdate(int employeeID, string itemCode, string[] addBarcodes, string[] removeBarcodes) {
        this.employeeID     = employeeID;
        this.itemCode       = itemCode;
        this.addBarcodes    = addBarcodes;
        this.removeBarcodes = removeBarcodes;
    }


    public UpdateItemBarCodeResponse Execute() {
        var     response = new UpdateItemBarCodeResponse();
        Company company  = null;
        try {
            Global.TransactionMutex.WaitOne();
            Global.ConnectCompany();
            company = ConnectionController.Company;
            company.StartTransaction();
            item = (Items)company.GetBusinessObject(BoObjectTypes.oItems);
            if (!item.GetByKey(itemCode))
                throw new ArgumentException($"Item Code {itemCode} not found!");

            RemoveBarcodes();
            AddNewBarcodes();
            
            item.UserFields.Fields.Item("U_LW_UPDATE_USER").Value      = employeeID;
            item.UserFields.Fields.Item("U_LW_UPDATE_TIMESTAMP").Value = DateTime.Now;

            if (item.Update() == 0) {
                response.Status = ResponseStatus.Ok;
            }
            else {
                response.ErrorMessage = company.GetLastErrorDescription();
                response.Status       = ResponseStatus.Error;
            }

            company.EndTransaction(BoWfTransOpt.wf_Commit);
        }
        catch {
            company?.EndTransaction(BoWfTransOpt.wf_RollBack);
            throw;
        }
        finally {
            Global.TransactionMutex.ReleaseMutex();
        }

        return response;
    }


    private void AddNewBarcodes() {
        if (addBarcodes == null)
            return;
        foreach (string barcode in addBarcodes) {
            if (item.BarCodes.Count == 0 || !string.IsNullOrWhiteSpace(item.BarCodes.BarCode))
                item.BarCodes.Add();
            item.BarCodes.BarCode  = barcode;
            item.BarCodes.UoMEntry = -1;
        }
    }
    private void RemoveBarcodes() {
        if (removeBarcodes == null)
            return;
        for (int i = item.BarCodes.Count - 1; i >= 0; i--) {
            item.BarCodes.SetCurrentLine(i);
            string barcode = item.BarCodes.BarCode;
            if (!removeBarcodes.Contains(barcode))
                continue;
            item.BarCodes.Delete();
            if (item.BarCode.Equals(barcode))
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