using Adapters.Windows.SBO.Services;
using Core.Models;

namespace Adapters.Windows.SBO.Repositories;

public class SapEmployeeRepository(SapBusinessOneDatabaseService dbService) {
    public async Task<ExternalUserResponse?> GetByIdAsync(string id) {
        const string query = "select \"empID\", \"firstName\", \"lastName\" from \"OHEM\" where \"empID\" = @id";

        return await dbService.QuerySingleAsync(
            query,
            new { id },
            reader => new ExternalUserResponse {
                Id       = reader.GetInt32(0).ToString(),
                FullName = $"{reader.GetString(1)} {reader.GetString(2)}"
            });
    }

    public async Task<IEnumerable<ExternalUserResponse>> GetAllAsync() {
        const string query = "select \"empID\", \"firstName\", \"lastName\" from \"OHEM\"";

        return await dbService.QueryAsync(
            query,
            new { },
            reader => new ExternalUserResponse {
                Id       = reader.GetInt32(0).ToString(),
                FullName = $"{reader.GetString(1)} {reader.GetString(2)}"
            });
    }
}