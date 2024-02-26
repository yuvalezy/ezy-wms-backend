using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Runtime.InteropServices;
using SAPbobsCOM;
using Service.API.Transfer.Models;
using Service.Shared;
using Service.Shared.Company;
using Service.Shared.Data;
using GeneralData = Service.API.General.GeneralData;

namespace Service.API.Transfer;

public class TransferCreation(int id, int employeeID) : IDisposable {
    private string                                 whsCode;
    private StockTransfer                          transfer;
    private Recordset                              rs;
    private Dictionary<string, CreateTransferLine> data;

    public int Entry  { get; private set; }
    public int Number { get; private set; }

    public void Execute() {
        bool releaseMutex = false;
        try {
            LoadData();
            int transferSeries = GeneralData.GetSeries("67");
            Global.TransactionMutex.WaitOne();
            releaseMutex = true;
            ConnectionController.BeginTransaction();
            Global.ConnectCompany();
            CreateTransfer(transferSeries);

            ConnectionController.Commit();
        }
        catch (Exception e) {
            ConnectionController.TryRollback();
            throw new Exception("Error generating GRPO: " + e.Message);
        }
        finally {
            if (releaseMutex)
                Global.TransactionMutex.ReleaseMutex();
        }
    }

    private void CreateTransfer(int transferSeries) {
        transfer         = (StockTransfer)ConnectionController.Company.GetBusinessObject(BoObjectTypes.oStockTransfer);
        transfer.DocDate = DateTime.Now;
        transfer.Series  = transferSeries;

        transfer.UserFields.Fields.Item("U_LW_GRPO").Value = id;

        var lines = transfer.Lines;

        foreach (var pair in data) {
            if (!string.IsNullOrWhiteSpace(lines.ItemCode))
                lines.Add();
            var value = pair.Value;
            lines.ItemCode          = value.ItemCode;
            lines.FromWarehouseCode = whsCode;
            lines.WarehouseCode     = whsCode;
            lines.Quantity          = value.Quantity;

            value.SourceBins.ForEach(source => {
                if (lines.BinAllocations.BinAbsEntry > 0) lines.BinAllocations.Add();
                lines.BinAllocations.BinActionType = BinActionTypeEnum.batFromWarehouse;
                lines.BinAllocations.BinAbsEntry   = source.BinEntry;
                lines.BinAllocations.Quantity      = source.Quantity;
            });
            value.TargetBins.ForEach(target => {
                if (lines.BinAllocations.BinAbsEntry > 0) lines.BinAllocations.Add();
                lines.BinAllocations.BinActionType = BinActionTypeEnum.batToWarehouse;
                lines.BinAllocations.BinAbsEntry   = target.BinEntry;
                lines.BinAllocations.Quantity      = target.Quantity;
            });
        }

        if (transfer.Add() != 0) {
            throw new Exception(ConnectionController.Company.GetLastErrorDescription());
        }

        Entry = int.Parse(ConnectionController.Company.GetNewObjectKey());
        rs    = (Recordset)ConnectionController.Company.GetBusinessObject(BoObjectTypes.BoRecordset);
        rs.DoQuery($"select \"DocNum\" from OWTR where \"DocEntry\" = {Entry}");
        Number = (int)rs.Fields.Item(0).Value;
    }

    private void LoadData() {
        const string query = """select "U_WhsCode" "WhsCode" from "@LW_YUVAL08_TRANS" where "Code" = @ID""";
        whsCode = Global.DataObject.GetValue<string>(query, new Parameter("@ID", SqlDbType.Int, id));

        using (var dt = Global.DataObject.GetDataTable(TransferData.GetQuery("ProcessTransferLines"), [new Parameter("@ID", SqlDbType.Int, id)])) {
            data = dt.Rows.Cast<DataRow>()
                .Select(dr => new CreateTransferLine(dr))
                .ToDictionary(g => g.ItemCode, g => g);
        }

        using (var dt = Global.DataObject.GetDataTable(TransferData.GetQuery("ProcessTransferLinesBins"), [new Parameter("@ID", SqlDbType.Int, id)])) {
            foreach (DataRow dr in dt.Rows) {
                string itemCode = (string)dr["ItemCode"];
                var    type     = (SourceTarget)Convert.ToChar(dr["Type"]);
                switch (type) {
                    case SourceTarget.Source:
                        data[itemCode].SourceBins.Add(new CreateTransferLineBin(dr));
                        break;
                    case SourceTarget.Target:
                        data[itemCode].TargetBins.Add(new CreateTransferLineBin(dr));
                        break;
                    default:
                        throw new Exception("Invalid Bin Row Type");
                }
            }
        }
    }

    public void Dispose() {
        if (transfer != null) {
            Marshal.ReleaseComObject(transfer);
            transfer = null;
        }

        if (rs != null) {
            Marshal.ReleaseComObject(rs);
            rs = null;
        }

        GC.Collect();
    }

    public void SetFinishedLines() {
        string sqlStr = $"update \"@LW_YUVAL08_TRANS1\" set \"U_LineStatus\" = 'F' where U_ID = {id} and \"U_LineStatus\" <> 'C'";
        Global.DataObject.Execute(sqlStr);
    }
}