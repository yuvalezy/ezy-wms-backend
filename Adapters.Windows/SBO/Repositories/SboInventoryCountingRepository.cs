using System.Data;
using Adapters.Windows.SBO.Services;
using Adapters.Windows.SBO.Utils;
using Core.DTOs;
using Microsoft.Data.SqlClient;
using SAPbobsCOM;

namespace Adapters.Windows.SBO.Repositories;

public class SboInventoryCountingRepository(SboDatabaseService dbService, SboCompany sboCompany) {
    
    public async Task ProcessInventoryCounting(int countingNumber, string warehouse) {
        // sboCompany.TransactionMutex.WaitOne();
        //
        // try {
        //     var company = sboCompany.Company;
        //     
        //     // Begin transaction
        //     if (company.InTransaction) {
        //         company.EndTransaction(BoWfTransOpt.wf_RollBack);
        //     }
        //     company.StartTransaction();
        //     
        //     try {
        //         // Get company service
        //         var companyService = company.GetCompanyService();
        //         var inventoryCountingsService = companyService.GetBusinessService(ServiceTypes.InventoryCountingsService) as InventoryCountingsService;
        //         
        //         // Create inventory counting object
        //         var counting = inventoryCountingsService.GetDataInterface(InventoryCountingsServiceDataInterfaces.icsInventoryCounting) as InventoryCounting;
        //         
        //         // Set counting properties
        //         counting.CountingType = CountingTypeEnum.ctSingleCounter;
        //         counting.Counted = BoYesNoEnum.tYES;
        //         
        //         // Get series for inventory counting document  
        //         var series = GetDocumentSeries(company, (int)BoObjectTypes.oInventoryCounting);
        //         if (series > 0) {
        //             counting.Series = series;
        //         }
        //         
        //         // Load counting data and add lines
        //         await LoadCountingLines(counting, countingNumber, warehouse);
        //         
        //         // Add the counting to SAP B1
        //         var countingParams = inventoryCountingsService.Add(counting);
        //         
        //         company.EndTransaction(BoWfTransOpt.wf_Commit);
        //         
        //         // Update the WMS database with the SAP document entry
        //         await UpdateWmsWithSapEntry(countingNumber, countingParams.CountingNumber);
        //     }
        //     catch (Exception) {
        //         if (company.InTransaction) {
        //             company.EndTransaction(BoWfTransOpt.wf_RollBack);
        //         }
        //         throw;
        //     }
        // }
        // finally {
        //     SboAssembly.TransactionMutex.ReleaseMutex();
        // }
        throw new NotImplementedException();
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
    
    public async Task<InventoryCountingSummaryResponse> GetInventoryCountingSummary(Guid countingId) {
        var query = @"
SELECT 
    h.""U_ID"" AS ""CountingId"",
    h.""DocEntry"" AS ""Number"",
    h.""U_Name"" AS ""Name"",
    h.""U_Date"" AS ""Date"",
    h.""U_WhsCode"" AS ""WhsCode"",
    COUNT(l.""LineId"") AS ""TotalLines"",
    SUM(CASE WHEN l.""U_LineStatus"" = 'C' THEN 1 ELSE 0 END) AS ""ProcessedLines"",
    SUM(CASE WHEN l.""U_Quantity"" <> COALESCE(oibq.""OnHandQty"", 0) THEN 1 ELSE 0 END) AS ""VarianceLines"",
    SUM(COALESCE(oibq.""OnHandQty"", 0) * COALESCE(oitm.""LastPurPrc"", 0)) AS ""TotalSystemValue"",
    SUM(l.""U_Quantity"" * COALESCE(oitm.""LastPurPrc"", 0)) AS ""TotalCountedValue""
FROM @LW_YUVAL08_OINC h
INNER JOIN @LW_YUVAL08_OINC1 l ON l.""DocEntry"" = h.""DocEntry""
LEFT JOIN OITM oitm ON oitm.""ItemCode"" = l.""U_ItemCode""
LEFT JOIN OIBQ oibq ON oibq.""ItemCode"" = l.""U_ItemCode"" AND oibq.""BinAbs"" = l.""U_BinEntry""
WHERE h.""U_ID"" = @CountingId
GROUP BY h.""U_ID"", h.""DocEntry"", h.""U_Name"", h.""U_Date"", h.""U_WhsCode""";
        
        var parameters = new[] {
            new SqlParameter("@CountingId", SqlDbType.UniqueIdentifier) { Value = countingId }
        };
        
        var result = await dbService.QuerySingleAsync(query, parameters, reader => new InventoryCountingSummaryResponse {
            CountingId = reader.GetGuid("CountingId"),
            Number = reader.GetInt32("Number"),
            Name = reader.GetString("Name"),
            Date = reader.GetDateTime("Date"),
            WhsCode = reader.GetString("WhsCode"),
            TotalLines = reader.GetInt32("TotalLines"),
            ProcessedLines = reader.GetInt32("ProcessedLines"),
            VarianceLines = reader.GetInt32("VarianceLines"),
            TotalSystemValue = reader.GetDecimal("TotalSystemValue"),
            TotalCountedValue = reader.GetDecimal("TotalCountedValue")
        });
        
        if (result != null) {
            result.TotalVarianceValue = result.TotalCountedValue - result.TotalSystemValue;
        }
        
        return result ?? new InventoryCountingSummaryResponse { CountingId = countingId };
    }
    
    private async Task LoadCountingLines(InventoryCounting counting, int countingNumber, string warehouse) {
        // Use the ProcessCountingLines query logic adapted for the new architecture
        var query = @"
SELECT 
    T0.""U_ItemCode"" AS ""ItemCode"",
    T0.""U_WhsCode"" AS ""WhsCode"",
    T0.""U_BinEntry"" AS ""BinEntry"",
    SUM(T0.""U_Quantity"") AS ""CountedQuantity""
FROM @LW_YUVAL08_OINC1 T0
INNER JOIN @LW_YUVAL08_OINC T1 ON T1.""DocEntry"" = T0.""DocEntry""
WHERE T1.""DocEntry"" = @CountingNumber
    AND T0.""U_LineStatus"" = 'O'
GROUP BY T0.""U_ItemCode"", T0.""U_WhsCode"", T0.""U_BinEntry""
HAVING SUM(T0.""U_Quantity"") > 0

UNION ALL

-- Zero out uncounted items in counted bin locations
SELECT DISTINCT
    OIBQ.""ItemCode"",
    @WhsCode AS ""WhsCode"",
    OIBQ.""BinAbs"" AS ""BinEntry"",
    0 AS ""CountedQuantity""
FROM OIBQ
INNER JOIN (
    SELECT DISTINCT T0.""U_BinEntry""
    FROM @LW_YUVAL08_OINC1 T0
    INNER JOIN @LW_YUVAL08_OINC T1 ON T1.""DocEntry"" = T0.""DocEntry""
    WHERE T1.""DocEntry"" = @CountingNumber
        AND T0.""U_LineStatus"" = 'O'
        AND T0.""U_BinEntry"" IS NOT NULL
) CountedBins ON CountedBins.""U_BinEntry"" = OIBQ.""BinAbs""
INNER JOIN OBIN ON OBIN.""AbsEntry"" = OIBQ.""BinAbs"" AND OBIN.""WhsCode"" = @WhsCode
LEFT JOIN (
    SELECT T0.""U_ItemCode"", T0.""U_BinEntry""
    FROM @LW_YUVAL08_OINC1 T0
    INNER JOIN @LW_YUVAL08_OINC T1 ON T1.""DocEntry"" = T0.""DocEntry""
    WHERE T1.""DocEntry"" = @CountingNumber
        AND T0.""U_LineStatus"" = 'O'
) Counted ON Counted.""U_ItemCode"" = OIBQ.""ItemCode"" AND Counted.""U_BinEntry"" = OIBQ.""BinAbs""
WHERE OIBQ.""OnHandQty"" > 0
    AND Counted.""U_ItemCode"" IS NULL
ORDER BY ""ItemCode"", ""BinEntry""";
        
        var parameters = new[] {
            new SqlParameter("@CountingNumber", SqlDbType.Int) { Value = countingNumber },
            new SqlParameter("@WhsCode", SqlDbType.NVarChar, 8) { Value = warehouse }
        };
        
        var countingData = await dbService.QueryAsync(query, parameters, reader => new {
            ItemCode = reader.GetString("ItemCode"),
            WhsCode = reader.GetString("WhsCode"),
            BinEntry = reader.IsDBNull("BinEntry") ? (int?)null : reader.GetInt32("BinEntry"),
            CountedQuantity = reader.GetInt32("CountedQuantity")
        });
        
        var lines = counting.InventoryCountingLines;
        
        foreach (var data in countingData) {
            var line = lines.Add();
            line.ItemCode = data.ItemCode;
            line.WarehouseCode = data.WhsCode;
            
            if (data.BinEntry.HasValue) {
                line.BinEntry = data.BinEntry.Value;
            }
            
            line.Counted = BoYesNoEnum.tYES;
            line.CountedQuantity = data.CountedQuantity;
        }
    }
    
    private int GetDocumentSeries(Company company, int objectType) {
        throw new NotImplementedException();
        // try {
        //     var seriesService = company.GetCompanyService().GetBusinessService(ServiceTypes.SeriesService) as SeriesService;
        //     var series = seriesService.GetDocumentSeries((BoObjectTypes)objectType);
        //     
        //     // Return the first available series
        //     if (series.Count > 0) {
        //         return series.Item(0).Series;
        //     }
        // }
        // catch {
        //     // If series lookup fails, return 0 to use default
        // }
        //
        // return 0;
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