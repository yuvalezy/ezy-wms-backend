using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Runtime.InteropServices;
using SAPbobsCOM;
using Service.API.GoodsReceipt.Models;
using Service.Shared.Company;
using Service.Shared.Data;
using GeneralData = Service.API.General.GeneralData;

namespace Service.API.GoodsReceipt;

public class GoodsReceiptCreation : IDisposable {
    private readonly int id;
    private readonly int employeeID;

    private string           whsCode;
    private GoodsReceiptType type;
    private Documents        doc;

    private Dictionary<(string CardCode, int Type, int Entry), List<GoodsReceiptCreationValue>> data;

    public List<(int Entry, int Number)> NewEntries { get; } = new();

    public GoodsReceiptCreation(int id, int employeeID) {
        this.id         = id;
        this.employeeID = employeeID;
    }

    public void Execute() {
        bool releaseMutex = false;
        try {
            LoadData();
            Global.TransactionMutex.WaitOne();
            releaseMutex = true;
            Global.ConnectCompany();
            foreach (var pair in data)
                CreateDocument(pair.Key.CardCode, pair.Key.Type, pair.Key.Entry, pair.Value);
        }
        catch (Exception e) {
            throw new Exception("Error generating GRPO: " + e.Message);
        }
        finally {
            if (releaseMutex)
                Global.TransactionMutex.ReleaseMutex();
        }
    }

    // ReSharper disable once ParameterHidesMember
    private void CreateDocument(string cardCode, int baseType, int baseEntry, List<GoodsReceiptCreationValue> values) {
        if (Global.GRPODraft) {
            doc               = (Documents)ConnectionController.Company.GetBusinessObject(BoObjectTypes.oDrafts);
            doc.DocObjectCode = BoObjectTypes.oPurchaseDeliveryNotes;
        }
        else {
            doc = (Documents)ConnectionController.Company.GetBusinessObject(BoObjectTypes.oPurchaseDeliveryNotes);
        }

        doc.CardCode   = cardCode;
        doc.DocDate    = DateTime.Now;
        doc.DocDueDate = DateTime.Now;
        doc.TaxDate    = DateTime.Now;
        doc.Series     = GeneralData.GetSeries("20");

        doc.UserFields.Fields.Item("U_LW_GRPO").Value = id;

        var lines = doc.Lines;

        for (int i = 0; i < values.Count; i++) {
            if (i > 0)
                lines.Add();
            var value = values[i];
            lines.ItemCode      = value.ItemCode;
            lines.WarehouseCode = whsCode;
            if (baseType != -1) {
                lines.BaseType  = baseType;
                lines.BaseEntry = baseEntry;
                lines.BaseLine  = value.BaseLine;
            }

            if (value.UseBaseUn) {
                lines.UseBaseUnits      = BoYesNoEnum.tYES;
                lines.UnitsOfMeasurment = 1;
            }

            lines.Quantity = value.Quantity;
        }

        if (doc.Add() != 0) {
            throw new Exception(ConnectionController.Company.GetLastErrorDescription());
        }

        int entry  = int.Parse(ConnectionController.Company.GetNewObjectKey());
        int number = Global.DataObject.GetValue<int>("select \"DocNum\" from OPDN where \"DocEntry\" = " + entry);
        NewEntries.Add((entry, number));
    }

    private void LoadData() {
        const string query = """select "U_WhsCode" "WhsCode", "U_Type" "Type" from "@LW_YUVAL08_GRPO" where "Code" = @ID""";
        (whsCode, char typeValue) = Global.DataObject.GetValue<string, char>(query, new Parameter("@ID", SqlDbType.Int, id));
        type                      = (GoodsReceiptType)typeValue;
        using var dt = Global.DataObject.GetDataTable(GoodsReceiptData.GetQuery("ProcessGoodsReceiptLines"), new Parameters {
            new Parameter("@ID", SqlDbType.Int, id),
            new Parameter("@empID", SqlDbType.Int, employeeID),
        });
        data = dt.Rows.Cast<DataRow>()
            .Select(dr => new GoodsReceiptCreationValue(dr))
            .GroupBy(v => (
                v.CardCode,
                BaseType: v.BaseLine >= 0 ? v.BaseType : -1,
                BaseEntry: v.BaseLine >= 0 ? v.BaseEntry : -1
            ))
            .ToDictionary(g => g.Key, g => g.ToList());
    }

    public void Dispose() {
        if (doc == null)
            return;
        Marshal.ReleaseComObject(doc);
        doc = null;
        GC.Collect();
    }
}