using System.Data;
using Adapters.Windows.SBO.Helpers;
using Adapters.Windows.SBO.Services;
using Core.DTOs.InventoryCounting;
using Core.Enums;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using SAPbobsCOM;

namespace Adapters.Windows.SBO.Repositories;

public class SboInventoryCountingRepository(SboDatabaseService dbService, SboCompany sboCompany, ILoggerFactory loggerFactory) {
    
    public async Task<ProcessInventoryCountingResponse> ProcessInventoryCounting(int countingNumber, string whsCode, Dictionary<string, InventoryCountingCreationDataResponse> data, int i) {
        int       series           = await GetSeries("1470000065");
        using var creation = new CountingCreation(sboCompany, countingNumber, whsCode, series, data, loggerFactory);
        try {
            return creation.Execute();
        }
        catch (Exception e) {
            return new ProcessInventoryCountingResponse {
                Success      = false,
                Status       = ResponseStatus.Error,
                ErrorMessage = e.Message
            };
        }
        //todo send alert to sap
//     private void ProcessTransferSendAlert(int id, List<string> sendTo, TransferCreation creation) {
//         try {
//             using var alert = new Alert();
//             alert.Subject = string.Format(ErrorMessages.WMSTransactionAlert, id);
//             var transactionColumn = new AlertColumn(ErrorMessages.WMSTransaction);
//             var transferColumn    = new AlertColumn(ErrorMessages.InventoryTransfer, true);
//             alert.Columns.AddRange([transactionColumn, transferColumn]);
//             transactionColumn.Values.Add(new AlertValue(id.ToString()));
//             transferColumn.Values.Add(new AlertValue(creation.Number.ToString(), "67", creation.Entry.ToString()));
//
//             alert.Send(sendTo);
//         }
//         catch (Exception e) {
//             //todo log error handler
//         }
//     }
    }
    
    private async Task<int> GetSeries(BoObjectTypes objectType) => await GetSeries(((int)objectType).ToString());

    private async Task<int> GetSeries(string objectCode) {
        const string query =
            """
            select top 1 T1."Series"
            from OFPR T0
                     inner join NNM1 T1 on T1."ObjectCode" = @ObjectCode and T1."Indicator" = T0."Indicator"
            where (T1."LastNum" is null or T1."LastNum" >= "NextNumber")
            and T0."F_RefDate" <= @Date and T0."T_RefDate" >= @Date
            """;
        var parameters = new[] {
            new SqlParameter("@ObjectCode", SqlDbType.NVarChar, 50) { Value = objectCode },
            new SqlParameter("@Date", SqlDbType.DateTime) { Value = DateTime.UtcNow }
        };
        return await dbService.QuerySingleAsync(query, parameters, reader => reader.GetInt32(0));
    }
    
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