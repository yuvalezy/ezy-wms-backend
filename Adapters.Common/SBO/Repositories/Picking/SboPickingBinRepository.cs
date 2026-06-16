using System.Data;
using Adapters.Common.SBO.Services;
using Core.DTOs.Items;
using Microsoft.Data.SqlClient;

namespace Adapters.Common.SBO.Repositories;

public class SboPickingBinRepository(SboDatabaseService dbService) {
    public async Task<IEnumerable<ItemBinLocationResponseQuantity>> GetPickingDetailItemsBins(Dictionary<string, object> parameters) {
        string query =
        """
        select DISTINCT T2."ItemCode",
        T3."BinAbs" "BinEntry",
        T4."BinCode",
        T3."OnHandQty" - COALESCE(T5."BinQty", 0) "OnHandQty"
        from PKL1 T1
        inner join OILM T2 on T2.TransType = T1.BaseObject and T2.DocEntry = T1.OrderEntry and T2.DocLineNum = T1.OrderLine
        inner join OIBQ T3 on T3."ItemCode" = T2."ItemCode" and T3."WhsCode" = T2."LocCode"
        inner join OBIN T4 on T4."AbsEntry" = T3."BinAbs"
        left outer join (
        	select T2."BinAbs", T3."ItemCode", Sum(T2."PickQtty") "BinQty"
        	from PKL1 T0
        	inner join OPKL T1 on T1."AbsEntry" = T0."AbsEntry" and T1."Status" = 'P'
        	inner join PKL2 T2 on T2."AbsEntry" = T0."AbsEntry" and T2."PickEntry" = T0."PickEntry"
            inner join OILM T3 on T3.TransType = T0.BaseObject and T3.DocEntry = T0.OrderEntry and T3.DocLineNum = T0.OrderLine
        	Group By T2."BinAbs", T3."ItemCode"
        ) T5 on T5."BinAbs" = T3."BinAbs" and T5."ItemCode" = T3."ItemCode"
        where T1."AbsEntry" = @AbsEntry
        and T1."BaseObject" = @Type
        and T1."OrderEntry" = @Entry
        and T3."OnHandQty" - COALESCE(T5."BinQty", 0) > 0
        """;

        if (parameters.ContainsKey("@BinEntry")) {
            query += " AND T3.\"BinAbs\" = @BinEntry";
        }

        query += " ORDER BY T2.\"ItemCode\", T4.\"BinCode\"";

        var sqlParams = SboPickingRepositoryHelpers.ConvertToSqlParameters(parameters);

        return await dbService.QueryAsync(query, sqlParams, reader => new ItemBinLocationResponseQuantity {
            ItemCode = reader.GetString(0),
            Entry = reader.GetInt32(1),
            Code = reader.GetString(2),
            Quantity = reader.GetDecimal(3)
        });
    }

    public async Task<IEnumerable<PickingSelectionResponse>> GetPickingSelection(int absEntry) {
        const string query =
        """
        select T2."ItemCode",
               T3."BinAbs",
               Sum(T3."PickQtty") "Quantity",
                T4."NumInBuy",
                T4.PurPackUn,
                T4."CodeBars",
                T4."PurFactor1",
                T4."PurFactor2",
                T4."PurFactor3",
                T4."PurFactor4"
        from PKL1 T1
                 inner join OILM T2 on T2.TransType = T1.BaseObject and T2.DocEntry = T1.OrderEntry and T2.DocLineNum = T1.OrderLine
                 inner join PKL2 T3 on T3.AbsEntry = T1.AbsEntry and T3.PickEntry = T1.PickEntry
                inner join OITM T4 on T4."ItemCode" = T2."ItemCode"
        where T1."AbsEntry" = @AbsEntry
        GROUP BY T2."ItemCode", T3."BinAbs", T4."NumInBuy", T4.PurPackUn, T4."CodeBars",
        T4."PurFactor1", T4."PurFactor2", T4."PurFactor3", T4."PurFactor4"
        """;

        return await dbService.QueryAsync(query, [new SqlParameter("@AbsEntry", SqlDbType.Int) { Value = absEntry }],
            reader => new PickingSelectionResponse {
                ItemCode = (string)reader["ItemCode"],
                CodeBars = (string)reader["CodeBars"],
                BinEntry = (int)reader["BinAbs"],
                Quantity = Convert.ToDecimal(reader["Quantity"]),
                NumInBuy = Convert.ToDecimal(reader["NumInBuy"]),
                PackUn = Convert.ToDecimal(reader["PurPackUn"]),
                Factor1 = Convert.ToDecimal(reader["PurFactor1"]),
                Factor2 = Convert.ToDecimal(reader["PurFactor1"]),
                Factor3 = Convert.ToDecimal(reader["PurFactor1"]),
                Factor4 = Convert.ToDecimal(reader["PurFactor1"])
            });
    }
}
