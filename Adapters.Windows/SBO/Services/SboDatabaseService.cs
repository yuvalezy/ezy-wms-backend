using System.Data;
using Core.Interfaces;
using Microsoft.Data.SqlClient;

namespace Adapters.Windows.SBO.Services;

public class SboDatabaseService(ISettings settings) {
    private string ConnectionString => settings.ConnectionStrings.ExternalAdapterConnection;

    public async Task<T?> QuerySingleAsync<T>(string query, object parameters, Func<SqlDataReader, T> mapper) {
        await using var connection = new SqlConnection(ConnectionString);
        await connection.OpenAsync();

        await using var command = new SqlCommand(query, connection);
        AddParameters(command, parameters);

        await using var reader = await command.ExecuteReaderAsync();
        return await reader.ReadAsync() ? mapper(reader) : default;
    }

    public async Task<IEnumerable<T>> QueryAsync<T>(string query, object parameters, Func<SqlDataReader, T> mapper) {
        var results = new List<T>();
        
        await using var connection = new SqlConnection(ConnectionString);
        await connection.OpenAsync();

        await using var command = new SqlCommand(query, connection);
        AddParameters(command, parameters);

        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync()) {
            results.Add(mapper(reader));
        }

        return results;
    }

    public async Task<T?> ExecuteScalarAsync<T>(string query, object parameters) {
        await using var connection = new SqlConnection(ConnectionString);
        await connection.OpenAsync();

        await using var command = new SqlCommand(query, connection);
        AddParameters(command, parameters);

        var result = await command.ExecuteScalarAsync();
        return result == null || result == DBNull.Value ? default : (T)result;
    }

    public async Task<int> ExecuteAsync(string query, object parameters) {
        await using var connection = new SqlConnection(ConnectionString);
        await connection.OpenAsync();

        await using var command = new SqlCommand(query, connection);
        AddParameters(command, parameters);

        return await command.ExecuteNonQueryAsync();
    }

    public async Task<T> ExecuteInTransactionAsync<T>(Func<SqlConnection, SqlTransaction, Task<T>> operation) {
        await using var connection = new SqlConnection(ConnectionString);
        await connection.OpenAsync();

        await using var transaction = connection.BeginTransaction();
        try {
            var result = await operation(connection, transaction);
            await transaction.CommitAsync();
            return result;
        }
        catch {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task ExecuteInTransactionAsync(Func<SqlConnection, SqlTransaction, Task> operation) {
        await using var connection = new SqlConnection(ConnectionString);
        await connection.OpenAsync();

        await using var transaction = connection.BeginTransaction();
        try {
            await operation(connection, transaction);
            await transaction.CommitAsync();
        }
        catch {
            await transaction.RollbackAsync();
            throw;
        }
    }

    private void AddParameters(SqlCommand command, object parameters) {
        if (parameters == null) return;

        var properties = parameters.GetType().GetProperties();
        foreach (var property in properties) {
            var value = property.GetValue(parameters) ?? DBNull.Value;
            command.Parameters.AddWithValue($"@{property.Name}", value);
        }
    }

    // Helper methods for common SAP B1 queries
    public async Task<bool> TableExistsAsync(string tableName) {
        const string query = @"
            SELECT COUNT(*) 
            FROM INFORMATION_SCHEMA.TABLES 
            WHERE TABLE_NAME = @tableName";

        var count = await ExecuteScalarAsync<int>(query, new { tableName });
        return count > 0;
    }

    public async Task<DataTable> GetDataTableAsync(string query, object parameters) {
        await using var connection = new SqlConnection(ConnectionString);
        await connection.OpenAsync();

        await using var command = new SqlCommand(query, connection);
        AddParameters(command, parameters);

        using var adapter = new SqlDataAdapter(command);
        var dataTable = new DataTable();
        await Task.Run(() => adapter.Fill(dataTable));
        
        return dataTable;
    }

    // SAP HANA compatibility helper
    public string FormatQuery(string query, bool isHana = false) {
        if (!isHana) return query;

        // Convert SQL Server syntax to HANA syntax
        return query
            .Replace("[", "\"")
            .Replace("]", "\"")
            .Replace("dbo.", string.Empty)
            .Replace("GETDATE()", "CURRENT_TIMESTAMP");
    }
}