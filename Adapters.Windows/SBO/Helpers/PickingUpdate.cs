using System.Data;
using System.Runtime.InteropServices;
using Adapters.Windows.SBO.Services;
using Core.Entities;
using Microsoft.Data.SqlClient;
using SAPbobsCOM;

namespace Adapters.Windows.SBO.Helpers;

public class PickingUpdate(int absEntry, List<PickList> data, SboCompany sboCompany, string? filtersPickReady) : IDisposable {
    private Recordset? rs;

    public async Task Execute() {
        try {
            if (!sboCompany.TransactionMutex.WaitOne())
                return;
            try {
                sboCompany.ConnectCompany();
                sboCompany.Company.StartTransaction();
                PreparePickList();
                Process();
                if (sboCompany.Company.InTransaction)
                    sboCompany.Company.EndTransaction(BoWfTransOpt.wf_Commit);
            }
            finally {
                sboCompany.TransactionMutex.ReleaseMutex();
            }
        }
        catch {
            if (sboCompany.Company.InTransaction)
                sboCompany.Company.EndTransaction(BoWfTransOpt.wf_RollBack);
            throw;
        }
    }

    private void Process() {
        var pl = (PickLists)sboCompany.Company.GetBusinessObject(BoObjectTypes.oPickLists);
        try {
            if (!pl.GetByKey(absEntry)) {
                throw new Exception($"Could not find Pick List ${absEntry}");
            }
            if (pl.Status == BoPickStatus.ps_Closed) {
                throw new Exception($"Cannot process document if the Status is closed");
            }

            UpdatePickList(pl);
        }
        finally {
            Marshal.ReleaseComObject(pl);
            GC.Collect();
        }
    }

    private void PreparePickList() {
        var pl = (PickLists)sboCompany.Company.GetBusinessObject(BoObjectTypes.oPickLists);
        try {
            if (!pl.GetByKey(absEntry)) {
                throw new Exception($"Could not find Pick List ${absEntry}");
            }

            //todo figure if it's needed
            if (!string.IsNullOrWhiteSpace(filtersPickReady)) {
                if (pl.UserFields.Fields.Item(filtersPickReady).Value.ToString() == "Y")
                    return;
            }

            if (pl.Status == BoPickStatus.ps_Closed) {
                throw new Exception("Cannot process document if the Status is closed");
            }

            for (int i = 0; i < pl.Lines.Count; i++) {
                pl.Lines.SetCurrentLine(i);
                for (int j = 0; i < pl.Lines.BinAllocations.Count; i++) {
                    pl.Lines.BinAllocations.SetCurrentLine(j);
                    pl.Lines.BinAllocations.Quantity = 0;
                }
            }


            if (!string.IsNullOrWhiteSpace(filtersPickReady)) {
                pl.UserFields.Fields.Item(filtersPickReady).Value = "Y";
            }

            if (pl.UpdateReleasedAllocation() != 0) {
                throw new Exception(sboCompany.Company.GetLastErrorDescription());
            }
        }
        finally {
            Marshal.ReleaseComObject(pl);
            GC.Collect();
        }
    }


    private void UpdatePickList(PickLists pl) {
        var lines = data.GroupBy(v => v.PickEntry)
            .Select(a => new {
                PickEntry = a.Key,
                Quantity  = a.Sum(b => b.Quantity),
                Bins = a.GroupBy(b => b.BinEntry)
                    .Select(c => new { BinEntry = c.Key, Quantity = c.Sum(d => d.Quantity) })
            });
        for (int i = 0; i < pl.Lines.Count; i++) {
            pl.Lines.SetCurrentLine(i);
            var value = lines.FirstOrDefault(v => v.PickEntry == pl.Lines.LineNumber);
            if (value == null) {
                continue;
            }

            pl.Lines.PickedQuantity += value.Quantity;

            // Process bins
            var bins    = pl.Lines.BinAllocations;
            var control = new Dictionary<int, int>();
            for (int j = 0; j < bins.Count; j++) {
                if (bins.BinAbsEntry == 0)
                    continue;
                bins.SetCurrentLine(j);
                control[bins.BinAbsEntry] = j;
            }

            foreach (var binValue in value.Bins) {
                int binEntry = binValue.BinEntry!.Value;
                if (control.TryGetValue(binEntry, out int index)) {
                    bins.SetCurrentLine(index);
                }
                else {
                    if (bins.Count == 1 && bins.BinAbsEntry != 0)
                        bins.Add();
                    bins.BinAbsEntry = binEntry;
                    control.Add(binEntry, bins.Count - 1);
                }

                bins.Quantity += binValue.Quantity;
            }
        }

        if (pl.Update() != 0) {
            throw new Exception($"Could not update Pick List: {sboCompany.Company.GetLastErrorDescription()}");
        }
    }

    public void Dispose() {
        if (rs != null) {
            Marshal.ReleaseComObject(rs);
            GC.Collect();
        }
    }
}