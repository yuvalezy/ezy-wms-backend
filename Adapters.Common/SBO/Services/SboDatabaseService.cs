using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace Adapters.Common.SBO.Services;
public class SboDatabaseService(IConfiguration configuration) {
    private string ConnectionString => configuration.GetConnectionString("ExternalAdapterConnection") ?? throw new Exception("External Adapter Connection string not found");
    public async Task<T?> QuerySingleAsync<T>(string query, SqlParameter[]? parameters, Func<SqlDataReader, T> mapper) {
        await using var connection = new SqlConnection(ConnectionString);
        await connection.OpenAsync();

        await using var command = new SqlCommand(query, connection);
        AddParameters(command, parameters);

        await using var reader = await command.ExecuteReaderAsync();
        return await reader.ReadAsync() ? mapper(reader) : default;
    }

    public async Task<IEnumerable<T>> QueryAsync<T>(string query, SqlParameter[]? parameters, Func<SqlDataReader, T> mapper) {
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


    public async Task<T?> ExecuteScalarAsync<T>(string query, SqlParameter[]? parameters) {
        await using var connection = new SqlConnection(ConnectionString);
        await connection.OpenAsync();

        await using var command = new SqlCommand(query, connection);
        AddParameters(command, parameters);

        var result = await command.ExecuteScalarAsync();
        return result == null || result == DBNull.Value ? default : (T)result;
    }

    public async Task<int> ExecuteAsync(string query, SqlParameter[]? parameters) {
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

    /// <summary>
    /// Opens the external SAP database connection and begins a transaction that is always rolled
    /// back (never committed) when the returned scope is disposed. Intended for "mimic" validation
    /// queries that must be fully parsed and bound by SQL Server without persisting anything.
    /// </summary>
    public async Task<SboValidationScope> BeginValidationAsync() {
        var connection = new SqlConnection(ConnectionString);
        await connection.OpenAsync();
        var transaction = (SqlTransaction)await connection.BeginTransactionAsync();
        return new SboValidationScope(connection, transaction);
    }

    /// <summary>
    /// Executes a SQL query and processes the results with the provided reader function.
    /// </summary>
    /// <param name="query">SQL query to execute</param>
    /// <param name="parameters">Optional SQL parameters</param>
    /// <param name="readerFunc">Function to process the SqlDataReader</param>
    /// <typeparam name="T">Return type of the reader function</typeparam>
    /// <returns>Result from processing the reader</returns>
    public async Task ExecuteReaderAsync(string query, SqlParameter[]? parameters, Action<SqlDataReader> readerFunc) {
        await using var connection = new SqlConnection(ConnectionString);
        await connection.OpenAsync();

        await using var command = new SqlCommand(query, connection);
        AddParameters(command, parameters);

        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync()) {
            readerFunc(reader);
        }
    }

    private void AddParameters(SqlCommand command, SqlParameter[]? parameters) {
        if (parameters == null) return;
        command.Parameters.AddRange(parameters);
    }

    // Helper methods for common SAP B1 queries
    public async Task<bool> TableExistsAsync(string tableName) {
        const string query = @"
            SELECT COUNT(*) 
            FROM INFORMATION_SCHEMA.TABLES 
            WHERE TABLE_NAME = @tableName";

        var count = await ExecuteScalarAsync<int>(query, [new SqlParameter("@tableName", tableName)]);
        return count > 0;
    }

    public async Task<DataTable> GetDataTableAsync(string query, SqlParameter[]? parameters) {
        await using var connection = new SqlConnection(ConnectionString);
        await connection.OpenAsync();

        await using var command = new SqlCommand(query, connection);
        AddParameters(command, parameters);

        using var adapter   = new SqlDataAdapter(command);
        var       dataTable = new DataTable();
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

/// <summary>
/// A scoped open connection + transaction against the external SAP database that always rolls back
/// (never commits) on disposal. Used for "mimic" validation queries — the work is fully parsed and
/// bound by SQL Server but nothing is ever persisted.
/// </summary>
public sealed class SboValidationScope(SqlConnection connection, SqlTransaction transaction) : IAsyncDisposable {
    public SqlConnection  Connection  { get; } = connection;
    public SqlTransaction Transaction { get; } = transaction;

    public async ValueTask DisposeAsync() {
        try {
            await Transaction.RollbackAsync();
        }
        catch {
            // The connection is closing anyway; a failed rollback (e.g. already aborted) is benign.
        }
        await Transaction.DisposeAsync();
        await Connection.DisposeAsync();
    }
}