using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using SAPbobsCOM;
using Service.API.Picking.Models;
using Service.Shared.Company;
using Service.Shared.Utils;

namespace Service.API.Picking;

public class PickingUpdate : IDisposable {
    private readonly int    id;
    private readonly string whsCode;
    private readonly int    defaultBin;
    private readonly bool   enableBIN;

    private List<PickingValue> data;

    private Recordset rs = Shared.Utils.Shared.GetRecordset();
    private PickLists pl = (PickLists)ConnectionController.Company.GetBusinessObject(BoObjectTypes.oPickLists);

    public PickingUpdate(int id, string whsCode) {
        this.id      = id;
        this.whsCode = whsCode;
        (defaultBin, enableBIN) = $"select \"DftBinAbs\", \"BinActivat\" from OWHS where \"WhsCode\" = '{whsCode.ToQuery()}'"
            .ExecuteQueryValue<int, bool>();
    }

    public void Execute() {
        Global.TransactionMutex.WaitOne();
        ConnectionController.BeginTransaction();
        try {
            LoadPickList();
            UpdatePickingStatus(PickingStatus.Processing);
            UpdatePickList();
            UpdatePickingStatus(PickingStatus.Finished);
            ConnectionController.Commit();
        }
        catch (Exception e) {
            UpdatePickingStatus(PickingStatus.Open, e.Message);
            ConnectionController.Rollback();
            throw;
        }
        finally {
            Global.TransactionMutex.ReleaseMutex();
        }
    }


    private void LoadPickList() {
        string query = string.Format(PickingData.GetQuery("LoadPickValues"), id);
        data = query.ExecuteQueryReader<PickingValue>();
        if (!pl.GetByKey(id)) {
            throw new Exception($"Could not find Pick List ${id}");
        }

        if (pl.Status is not (BoPickStatus.ps_Released or BoPickStatus.ps_PartiallyPicked)) {
            throw new Exception("Cannot process document if the Status is not Released");
        }
    }

    private void UpdatePickList() {
        for (int i = 0; i < pl.Lines.Count; i++) {
            pl.Lines.SetCurrentLine(i);
            var value = data.FirstOrDefault(v => v.PickEntry == pl.Lines.LineNumber);
            if (value == null) {
                continue;
            }

            pl.Lines.PickedQuantity += value.Quantity;
            if (!enableBIN) 
                continue;
            bool found = false;
            var  bins  = pl.Lines.BinAllocations;
            for (int j = 0; j < bins.Count; j++) {
                if (bins.BinAbsEntry != defaultBin) 
                    continue;
                found         =  true;
                bins.Quantity += value.Quantity;
                break;
            }
            if (found) 
                continue;
            bins.Add();
            bins.BinAbsEntry = defaultBin;
            bins.Quantity    = value.Quantity;
        }

        if (pl.Update() != 0) {
            throw new Exception($"Could not update Pick List: {ConnectionController.Company.GetLastErrorDescription()}");
        }
    }

    private void UpdatePickingStatus(PickingStatus status, string errorMessage = null) {
        var sb = new StringBuilder($"update \"@LW_YUVAL08_PKL1\" set \"U_Status\" = '{(char)status}', ");
        sb.Append($"\"U_ErrorMessage\" = {errorMessage.ToSaveQuery()} ");
        sb.Append($"where \"U_AbsEntry\" = {id}");
        rs.DoQuery(sb.ToString());
    }

    public void Dispose() {
        Marshal.ReleaseComObject(rs);
        rs = null;

        Marshal.ReleaseComObject(pl);
        pl = null;

        GC.Collect();
    }
}