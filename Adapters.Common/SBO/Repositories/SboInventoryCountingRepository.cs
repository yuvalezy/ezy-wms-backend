using System.Data;
using Adapters.Common.SBO.Services;
using Core.DTOs.InventoryCounting;
using Microsoft.Data.SqlClient;

namespace Adapters.Common.SBO.Repositories;

public class SboInventoryCountingRepository(SboDatabaseService dbService) {
    public async Task<IEnumerable<InventoryCountingContentResponse>> GetInventoryCountingContent(Guid countingId, int? binEntry) {
        var query = @"
SELECT 
    ic.""ItemCode"",
    COALESCE(oitm.""ItemName"", '') AS ""ItemName"",
    ic.""BinEntry"",
    COALESCE(obin.""BinCode"", '') AS ""BinCode"",
    COALESCE(oibq.""OnHandQty"", 0) AS ""SystemQuantity"",
    SUM(ic.""Quantity"") AS ""CountedQuantity"",
    (SUM(ic.""Quantity"") - COALESCE(oibq.""OnHandQty"", 0)) AS ""Variance"",
    COALESCE(oitm.""LastPurPrc"", 0) AS ""UnitCost""
FROM @LW_YUVAL08_OINC1 ic
LEFT JOIN OITM oitm ON oitm.""ItemCode"" = ic.""ItemCode""
LEFT JOIN OBIN obin ON obin.""AbsEntry"" = ic.""BinEntry""
LEFT JOIN OIBQ oibq ON oibq.""ItemCode"" = ic.""ItemCode"" AND oibq.""BinAbs"" = ic.""BinEntry""
WHERE ic.""DocEntry"" = (
    SELECT ""DocEntry"" FROM @LW_YUVAL08_OINC WHERE ""U_ID"" = @CountingId
)";
        
        if (binEntry.HasValue) {
            query += " AND ic.\"BinEntry\" = @BinEntry";
        }
        
        query += @"
GROUP BY ic.""ItemCode"", oitm.""ItemName"", ic.""BinEntry"", obin.""BinCode"", oibq.""OnHandQty"", oitm.""LastPurPrc""
ORDER BY ic.""ItemCode"", ic.""BinEntry""";
        
        var parameters = new List<SqlParameter> {
            new("@CountingId", SqlDbType.UniqueIdentifier) { Value = countingId }
        };
        
        if (binEntry.HasValue) {
            parameters.Add(new SqlParameter("@BinEntry", SqlDbType.Int) { Value = binEntry.Value });
        }
        
        return await dbService.QueryAsync(query, parameters.ToArray(), reader => {
            var systemQuantity = reader.GetInt32("SystemQuantity");
            var countedQuantity = reader.GetInt32("CountedQuantity");
            var variance = countedQuantity - systemQuantity;
            var unitCost = reader.GetDecimal("UnitCost");
            
            return new InventoryCountingContentResponse {
                ItemCode = reader.GetString("ItemCode"),
                ItemName = reader.GetString("ItemName"),
                BinEntry = reader.IsDBNull("BinEntry") ? null : reader.GetInt32("BinEntry"),
                BinCode = reader.IsDBNull("BinCode") ? null : reader.GetString("BinCode"),
                SystemQuantity = systemQuantity,
                CountedQuantity = countedQuantity,
                Variance = variance,
                SystemValue = systemQuantity * unitCost,
                CountedValue = countedQuantity * unitCost,
                VarianceValue = variance * unitCost
            };
        });
    }
    
    private async Task UpdateWmsWithSapEntry(int countingNumber, int sapCountingNumber) {
        var query = @"
UPDATE @LW_YUVAL08_OINC 
SET ""U_InvCountEntry"" = @SapCountingNumber
WHERE ""DocEntry"" = @CountingNumber";
        
        var parameters = new[] {
            new SqlParameter("@CountingNumber", SqlDbType.Int) { Value = countingNumber },
            new SqlParameter("@SapCountingNumber", SqlDbType.Int) { Value = sapCountingNumber }
        };
        
        await dbService.ExecuteAsync(query, parameters);
    }
}