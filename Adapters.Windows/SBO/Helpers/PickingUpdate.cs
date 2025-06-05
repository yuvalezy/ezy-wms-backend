using System.Data;
using System.Runtime.InteropServices;
using System.Text;
using Adapters.Windows.SBO.Services;
using Core.Entities;
using Core.Enums;
using Core.Models;
using Microsoft.Data.SqlClient;
using SAPbobsCOM;

namespace Adapters.Windows.SBO.Helpers;

public class PickingUpdate(int absEntry, string warehouse, List<PickList> data, SboDatabaseService dbService, SboCompany sboCompany) : IDisposable {
    private Recordset? rs;

    private readonly Dictionary<int, (string itemCode, int numInBuy, bool useBaseUnit)> additionalData = new();

    public void Execute() {
        LoadAdditionalData();
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

    private async Task LoadAdditionalData() {
        const string query =
            """
            select PKL1."PickEntry", T3."ItemCode", COALESCE(T3."NumInBuy", 1) "NumInBuy",
                   CASE 
                       WHEN T2."TransType" = 17 THEN T4."UseBaseUn"
                       WHEN T2."TransType" = 13 THEN T5."UseBaseUn"
            		   Else 'Y'
                   END AS "UseBaseUn"
            from PKL1
            inner join OILM T2 on T2."TransType" = PKL1."BaseObject" and T2.DocEntry = PKL1."OrderEntry" and T2."DocLineNum" = PKL1."OrderLine"
            inner join OITM T3 on T3."ItemCode" = T2."ItemCode" 
            LEFT JOIN RDR1 T4 ON T2."TransType" = 17 
                AND T4."DocEntry" = T2."DocEntry" 
                AND T4."LineNum" = T2."DocLineNum"
            LEFT JOIN INV1 T5 ON T2."TransType" = 13 
                AND T5."DocEntry" = T2."DocEntry" 
                AND T5."LineNum" = T2."DocLineNum"
            where PKL1."AbsEntry" = @AbsEntry
            """;
        using var dt = await dbService.GetDataTableAsync(query, [new SqlParameter("@AbsEntry", SqlDbType.Int) { Value = absEntry }]);
        foreach (DataRow row in dt.Rows) {
            int    pickEntry   = Convert.ToInt32(row["PickEntry"]);
            string itemCode    = row["ItemCode"]!.ToString();
            int    numInBuy    = Convert.ToInt32(row["NumInBuy"]);
            bool   useBaseUnit = row["UseBaseUn"]!.ToString().Equals("Y");
            additionalData.Add(pickEntry, (itemCode, numInBuy, useBaseUnit)!);
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

            (_, int numInBuy, bool useBaseUnit) = additionalData[value.PickEntry];

            pl.Lines.PickedQuantity = value.Quantity / (useBaseUnit ? numInBuy : 1);
            // pl.Lines.PickedQuantity = (double)value.Quantity / (value.Unit != UnitType.Unit ? value.NumInBuy : 1);

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

                bins.Quantity = binValue.Quantity;
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