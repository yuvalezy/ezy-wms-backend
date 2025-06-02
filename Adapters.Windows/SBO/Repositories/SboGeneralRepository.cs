using Adapters.Windows.SBO.Services;
using Adapters.Windows.Utils;
using Core.Models;

namespace Adapters.Windows.SBO.Repositories;

public class SboGeneralRepository(SboDatabaseService dbService) {
    public async Task<string?> GetCompanyNameAsync() {
        const string query = "SELECT COALESCE(\"PrintHeadr\", \"CompnyName\") FROM OADM";

        return await dbService.QuerySingleAsync(query, new { }, reader => reader.GetString(0));
    }

    public async Task<IEnumerable<ExternalValue>> GetWarehousesAsync(string[]? filter) {
        string     query      = "select \"WhsCode\" as \"Id\", \"WhsName\" as \"Name\" from OWHS";
        object? parameters = null;

        if (filter?.Length > 0) {
            query      += SqlQueryUtils.BuildInClause("WhsCode", "WhsCode", filter.Length);
            parameters =  SqlQueryUtils.BuildInParameters("WhsCode", filter);
        }

        return await dbService.QueryAsync(query, parameters, reader => new ExternalValue {
            Id   = reader.GetString(0),
            Name = reader.GetString(1)
        });
    }

}