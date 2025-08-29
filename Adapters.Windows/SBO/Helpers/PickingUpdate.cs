using System.Data;
using System.Runtime.InteropServices;
using Adapters.Common.SBO.Services;
using Adapters.Windows.SBO.Services;
using Core.Entities;
using Microsoft.Data.SqlClient;
using SAPbobsCOM;

namespace Adapters.Windows.SBO.Helpers;

public class PickingUpdate(int absEntry, List<PickList> data, SboCompany sboCompany, SboDatabaseService databaseService) : IDisposable {
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

            ProcessPickList(pl);
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

            if (pl.UserFields.Fields.Item("U_WMS_READY").Value.ToString() == "Y")
                return;

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


            pl.UserFields.Fields.Item("U_WMS_READY").Value = "Y";

            if (pl.UpdateReleasedAllocation() != 0) {
                throw new Exception(sboCompany.Company.GetLastErrorDescription());
            }
        }
        finally {
            Marshal.ReleaseComObject(pl);
            GC.Collect();
        }
    }

    private record SourceMeasureData(int DocEntry, int LineNum, int ObjType, int NumPerMeasure, double Quantity);

    private async Task<SourceMeasureData[]> GetSourceMeasureData() {
        const string query =
        """
        select T0."OrderEntry", T0."OrderLine", T0."BaseObject", COALESCE(T1."NumPerMsr", T2."NumPerMsr", T3."NumPerMsr") "NumPerMsr", COALESCE(T1."InvQty", T2."InvQty", T3."InvQty") "InvQty"
        from PKL1 T0
                 left outer join RDR1 T1 on T1."DocEntry" = T0."OrderEntry" and T1."ObjType" = T0."BaseObject" and T1."LineNum" = T0."OrderLine"
                 left outer join PCH1 T2 on T2."DocEntry" = T0."OrderEntry" and T2."ObjType" = T0."BaseObject" and T2."LineNum" = T0."OrderLine"
                 left outer join WTQ1 T3 on T3."DocEntry" = T0."OrderEntry" and T3."ObjType" = T0."BaseObject" and T3."LineNum" = T0."OrderLine"
        where T0."AbsEntry" = @AbsEntry
        """;

        var sourceData = await databaseService.QueryAsync(query, [new SqlParameter("@AbsEntry", SqlDbType.Int) { Value = absEntry }], reader => new SourceMeasureData(
            (int)reader["OrderEntry"],
            (int)reader["OrderLine"],
            Convert.ToInt32(reader["BaseObject"]),
            Convert.ToInt32(reader["NumPerMsr"]),
            Convert.ToDouble(reader["InvQty"])
        ));

        return sourceData.ToArray();
    }

    private void ProcessPickList(PickLists pl) {
        var lines = data.GroupBy(v => v.PickEntry)
        .Select(a => new {
            PickEntry = a.Key,
            Quantity = a.Sum(b => b.Quantity),
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
            var bins = pl.Lines.BinAllocations;
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