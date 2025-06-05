using System.Runtime.InteropServices;
using System.Text;
using Adapters.Windows.SBO.Services;
using Core.Models;
using SAPbobsCOM;

namespace Adapters.Windows.SBO.Helpers;

public class PickingUpdate(int absEntry, string warehouse, Dictionary<string, List<PickingCreationData>> data, SboDatabaseService dbService, SboCompany sboCompany) : IDisposable {
    private Recordset? rs;

    public void Execute() {
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
            // if (pl.UserFields.Fields.Item("U_LW_YUVAL08_READY").Value.ToString() == "Y")
            //     return;

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

            
            // pl.UserFields.Fields.Item("U_LW_YUVAL08_READY").Value = "Y";
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
        for (int i = 0; i < pl.Lines.Count; i++) {
            pl.Lines.SetCurrentLine(i);
            var value = data.FirstOrDefault(v => v.PickEntry == pl.Lines.LineNumber);
            if (value == null) {
                continue;
            }

            pl.Lines.PickedQuantity = (double)value.Quantity / (value.Unit != UnitType.Unit ? value.NumInBuy : 1);

            UpdatePickListBinLocations(pl.Lines.BinAllocations, value);
        }

        if (pl.Update() != 0) {
            throw new Exception($"Could not update Pick List: {ConnectionController.Company.GetLastErrorDescription()}");
        }
    }

    private void UpdatePickListBinLocations(DocumentLinesBinAllocations bins, PickingValue value) {
        var control = new Dictionary<int, int>();
        for (int i = 0; i < bins.Count; i++) {
            if (bins.BinAbsEntry == 0)
                continue;
            bins.SetCurrentLine(i);
            control[bins.BinAbsEntry] = i;
        }

        value.BinLocations.ForEach(binValue => {
            if (control.TryGetValue(binValue.BinEntry, out int index)) {
                bins.SetCurrentLine(index);
            }
            else {
                if (bins.Count == 1 && bins.BinAbsEntry != 0)
                    bins.Add();
                bins.BinAbsEntry = binValue.BinEntry;
                control.Add(binValue.BinEntry, bins.Count - 1);
            }

            bins.Quantity = binValue.Quantity;
        });
    }

    public void Dispose() {
        if (rs != null) {
            Marshal.ReleaseComObject(rs);
            GC.Collect();
        }
    }
}