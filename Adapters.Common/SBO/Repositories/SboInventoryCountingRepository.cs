using Adapters.Common.SBO.Services;

namespace Adapters.Common.SBO.Repositories;

public class SboInventoryCountingRepository(SboDatabaseService dbService) {
    public async Task<bool> ValidateOpenInventoryCounting(string whsCode, int binEntry, string itemCode) {
        const string query =
            """
            select 1
            from INC1 T0
            where T0."BinEntry" = @BinEntry and T0."ItemCode" = @ItemCode and T0."LineStatus" = 'O'
            """;
        
        var parameters = new[] {
            new Microsoft.Data.SqlClient.SqlParameter("@BinEntry", binEntry),
            new Microsoft.Data.SqlClient.SqlParameter("@ItemCode", itemCode)
        };
        
        int? result = await dbService.ExecuteScalarAsync<int?>(query, parameters);
        return result.HasValue;
    }
}