using Adapters.Common.SBO.Services;
using Core.Models;
using Microsoft.Data.SqlClient;

namespace Adapters.Common.SBO.Repositories;

public class SboEmployeeRepository(SboDatabaseService dbService) {
    public async Task<ExternalValue<string>?> GetByIdAsync(string id) {
        const string query = "select \"empID\", \"firstName\", \"lastName\" from \"OHEM\" where \"empID\" = @id";

        return await dbService.QuerySingleAsync(
            query,
            [new SqlParameter("@id", id)],
            reader => new ExternalValue<string> {
                Id   = reader.GetInt32(0).ToString(),
                Name = $"{reader.GetString(1)} {reader.GetString(2)}"
            });
    }

    public async Task<IEnumerable<ExternalValue<string>>> GetAllAsync() {
        const string query = "select \"empID\", \"firstName\", \"lastName\" from \"OHEM\"";

        return await dbService.QueryAsync(
            query,
            null,
            reader => new ExternalValue<string> {
                Id   = reader.GetInt32(0).ToString(),
                Name = $"{reader.GetString(1)} {reader.GetString(2)}"
            });
    }
}