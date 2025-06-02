using Core.Interfaces;
using Core.Models;
using Microsoft.Data.SqlClient;

namespace Adapters.CrossPlatform;

public class SapBusinessOneDiApiAdapter(ISettings settings) : IExternalSystemAdapter {
    public async Task<ExternalUserResponse?> GetUserInfoAsync(string id) {
        await using var cn    = new SqlConnection(settings.ConnectionStrings.DefaultConnection);
        const string    query = "select \"empID\", \"firstName\", \"lastName\" from \"OHEM\" where \"empID\" = @id";

        await cn.OpenAsync();
        try {
            await using var cmd = new SqlCommand(query, cn);
            cmd.Parameters.AddWithValue("@id", id);
            await using var reader = await cmd.ExecuteReaderAsync();

            if (await reader.ReadAsync()) {
                return new ExternalUserResponse {
                    Id       = reader.GetString(0),
                    FullName = $"{reader.GetString(1)} {reader.GetString(2)}"
                };
            }

            return null;
        }
        finally {
            await cn.CloseAsync();
        }
    }

    public async Task<IEnumerable<ExternalUserResponse>> GetUsersAsync() {
        await using var cn    = new SqlConnection(settings.ConnectionStrings.DefaultConnection);
        const string    query = "select \"empID\", \"firstName\", \"lastName\" from \"OHEM\"";

        var result = new List<ExternalUserResponse>();
        await cn.OpenAsync();
        try {
            await using var cmd    = new SqlCommand(query, cn);
            await using var reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync()) {
                result.Add(new ExternalUserResponse {
                    Id       = reader.GetString(0),
                    FullName = $"{reader.GetString(1)} {reader.GetString(2)}"
                });
            }
        }
        finally {
            await cn.CloseAsync();
        }

        return result;
    }
}