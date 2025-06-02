using Adapters.Windows.SBO.Services;

namespace Adapters.Windows.SBO.Repositories;

public class SboGeneralRepository(SboDatabaseService dbService) {
    public async Task<string?> GetCompanyNameAsync() {
        const string query = "SELECT COALESCE(\"PrintHeadr\", \"CompnyName\") FROM OADM";
        
        return await dbService.QuerySingleAsync(query, new { }, reader => reader.GetString(0));
    }
}