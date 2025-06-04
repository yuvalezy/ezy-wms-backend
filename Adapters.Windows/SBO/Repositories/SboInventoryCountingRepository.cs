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