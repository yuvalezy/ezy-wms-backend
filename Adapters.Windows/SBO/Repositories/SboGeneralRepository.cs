using System.Data;
using Adapters.Windows.SBO.Services;
using Adapters.Windows.Utils;
using Core.Models;
using Microsoft.Data.SqlClient;

namespace Adapters.Windows.SBO.Repositories;

public class SboGeneralRepository(SboDatabaseService dbService) {
    public async Task<string?> GetCompanyNameAsync() {
        const string query = "SELECT COALESCE(\"PrintHeadr\", \"CompnyName\") FROM OADM";

        return await dbService.QuerySingleAsync(query, null, reader => reader.GetString(0));
    }

    public async Task<IEnumerable<ExternalValue>> GetWarehousesAsync(string[]? filter) {
        string            query      = "select \"WhsCode\" as \"Id\", \"WhsName\" as \"Name\" from OWHS";
        SqlParameter[]? parameters = null;

        if (filter?.Length > 0) {
            query      += SqlQueryUtils.BuildInClause("WhsCode", "WhsCode", filter.Length);
            parameters =  SqlQueryUtils.BuildInParameters("WhsCode", filter, parameters);
        }

        return await dbService.QueryAsync(query, parameters, reader => new ExternalValue {
            Id   = reader.GetString(0),
            Name = reader.GetString(1)
        });
    }

}