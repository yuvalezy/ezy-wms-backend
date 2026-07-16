using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace Adapters.Common.SBO.Services;
public class SboDatabaseService(IConfiguration configuration) {
    private string ConnectionString => configuration.GetConnectionString("ExternalAdapterConnection") ?? throw new Exception("External Adapter Connection string not found");

    /// <summary>
    /// Connection used to execute admin-authored report SQL. Falls back to
    /// <c>ExternalAdapterConnection</c> when unset, so nothing breaks if a deployment never configures it —
    /// but configuring it is what lets reports run as a read-only principal (<c>db_datareader</c> with
    /// <c>DENY INSERT, UPDATE, DELETE, EXECUTE</c>). That login is the only report guard with real teeth;
    /// the rollback scope, the leading-keyword check and the <c>;</c> rejection are defence in depth around it.
    /// </summary>
    /// <remarks>
    /// Blank counts as unset, not as a connection string. <c>GetConnectionString</c> returns <c>""</c>
    /// (not null) for a key that is present but empty, so a <c>??</c> fallback would never fire and
    /// reports would fail against an empty connection string — which is exactly the state a customer
    /// lands in after copying the shipped template and not filling this in.
    /// </remarks>
    private string ReportingConnectionString =>
        ResolveReportingConnectionString(configuration.GetConnectionString("ReportingConnection"), ConnectionString);

    /// <summary>
    /// Resolves the reporting connection string, treating blank as unset. Pure so the fallback is
    /// covered by a unit test rather than only by a live deployment.
    /// </summary>
    /// <param name="reporting">The configured <c>ReportingConnection</c>, if any.</param>
    /// <param name="fallback">The <c>ExternalAdapterConnection</c> to fall back to.</param>
    public static string ResolveReportingConnectionString(string? reporting, string fallback) =>
        string.IsNullOrWhiteSpace(reporting) ? fallback : reporting;
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
    /// Opens the external SAP database connection and begins a transaction that is always rolled back
    /// (never committed) when the returned scope is disposed.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Originally this only served "mimic" validation queries, but it is now the <b>execution</b> path for
    /// admin-authored SQL as well — hence the neutral name. For a pure <c>SELECT</c> under <c>READ COMMITTED</c>
    /// the explicit transaction adds no blocking (shared locks release as rows are read) and no correctness
    /// impact; what it buys is that a typo in operator-authored SQL cannot persist anything.
    /// </para>
    /// <para>
    /// It does pin the log for its duration, so callers must keep the scope short — bounded by an explicit
    /// <c>CommandTimeout</c>, and paged rather than streamed for large result sets.
    /// </para>
    /// </remarks>
    public Task<SboRollbackScope> BeginRollbackScopeAsync() => BeginScopeAsync(ConnectionString);

    /// <summary>
    /// The same always-rollback scope as <see cref="BeginRollbackScopeAsync"/>, but on
    /// <see cref="ReportingConnectionString"/> — i.e. the read-only principal when one is configured.
    /// Every report path (page, count, discover, lookup, validate) goes through here.
    /// </summary>
    public Task<SboRollbackScope> BeginReportingScopeAsync() => BeginScopeAsync(ReportingConnectionString);

    private static async Task<SboRollbackScope> BeginScopeAsync(string connectionString) {
        var connection = new SqlConnection(connectionString);
        try {
            await connection.OpenAsync();
            var transaction = (SqlTransaction)await connection.BeginTransactionAsync();
            return new SboRollbackScope(connection, transaction);
        }
        catch {
            // Nothing has been handed to the caller yet, so a failure here would otherwise leak the connection
            // back to the pool unusable — there is no scope for them to dispose.
            await connection.DisposeAsync();
            throw;
        }
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
/// (never commits) on disposal. Used both for "mimic" validation queries and for executing
/// admin-authored report SQL: the work is fully parsed, bound and run by SQL Server, but nothing it
/// wrote — by mistake or otherwise — is ever persisted.
/// </summary>
/// <remarks>
/// Commands must be built against <see cref="Connection"/> <b>and</b> <see cref="Transaction"/>
/// (<c>new SqlCommand(sql, scope.Connection, scope.Transaction)</c>). Helpers that open their own
/// connection — <c>GetDataTableAsync</c>, <c>QueryAsync</c>, <c>ExecuteScalarAsync</c> — run untransacted
/// and so cannot be used inside a scope.
/// </remarks>
public sealed class SboRollbackScope(SqlConnection connection, SqlTransaction transaction) : IAsyncDisposable {
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