using System;
using System.Data;
using System.Runtime.InteropServices;
using SAPbobsCOM;
using Service.Shared.Company;
using Service.Shared.Data;
using GeneralData = Service.API.General.GeneralData;

namespace Service.API.GoodsReceipt;

public class GoodsReceiptCreation : IDisposable {
    private readonly int id;
    private readonly int employeeID;

    private string    cardCode;
    private string    whsCode;
    private DataTable dt;
    private Documents doc;

    public int NewEntry { get; set; }

    public GoodsReceiptCreation(int id, int employeeID) {
        this.id         = id;
        this.employeeID = employeeID;
    }

    public void Execute() {
        try {
            LoadData();
            Global.TransactionMutex.WaitOne();
            Global.ConnectCompany();
            doc               = (Documents)ConnectionController.Company.GetBusinessObject(BoObjectTypes.oDrafts);
            doc.DocObjectCode = BoObjectTypes.oPurchaseDeliveryNotes;

            doc.CardCode   = cardCode;
            doc.DocDate    = DateTime.Now;
            doc.DocDueDate = DateTime.Now;
            doc.TaxDate    = DateTime.Now;
            doc.Series     = GeneralData.GetSeries("20");

            doc.UserFields.Fields.Item("U_LW_GRPO").Value = id;

            var lines = doc.Lines;

            for (int i = 0; i < dt.Rows.Count; i++) {
                var    row       = dt.Rows[i];
                string itemCode  = (string)row["ItemCode"];
                int    quantity  = (int)row["Quantity"];
                int    baseEntry = (int)row["BaseEntry"];
                int    baseLine  = (int)row["BaseLine"];
                if (i > 0)
                    lines.Add();
                lines.ItemCode      = itemCode;
                lines.WarehouseCode = whsCode;
                if (baseEntry != -1) {
                    lines.BaseType  = 22;
                    lines.BaseEntry = baseEntry;
                    lines.BaseLine  = baseLine;
                }

                lines.Quantity = quantity;
            }

            if (doc.Add() != 0) {
                throw new Exception(ConnectionController.Company.GetLastErrorDescription());
            }

            NewEntry = int.Parse(ConnectionController.Company.GetNewObjectKey());
        }
        catch (Exception e) {
            throw new Exception("Error generating GRPO: " + e.Message);
        }
        finally {
            Global.TransactionMutex.ReleaseMutex();
        }
    }

    private void LoadData() {
        const string query = "select \"U_CardCode\" \"CardCode\", \"U_WhsCode\" \"WhsCode\" from \"@LW_YUVAL08_GRPO\" where \"Code\" = @ID";
        (cardCode, whsCode) = Global.DataObject.GetValue<string, string>(query, new Parameter("@ID", SqlDbType.Int, id));
        dt = Global.DataObject.GetDataTable(GoodsReceiptData.GetQuery("ProcessGoodsReceiptLines"), new Parameters {
            new Parameter("@ID", SqlDbType.Int, id),
            new Parameter("@empID", SqlDbType.Int, employeeID),
        });
    }

    public void Dispose() {
        dt.Dispose();
        if (doc == null)
            return;
        Marshal.ReleaseComObject(doc);
        doc = null;
        GC.Collect();
    }
}