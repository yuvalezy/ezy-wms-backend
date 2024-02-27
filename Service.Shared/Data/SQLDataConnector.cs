using System;
using System.Data;
using System.Data.SqlClient;
using System.Linq;

namespace Service.Shared.Data;

public class SQLDataConnector : DataConnector {
    private SqlConnection  conn;
    private SqlTransaction transaction;

    public SQLDataConnector(string connectionString = null) : base(connectionString) {
    }

    private void ADO_Connection() {
        try {
            conn?.Close();
        }
        catch {
            //ignore
        }

        conn?.Dispose();
        conn = new SqlConnection(ConnectionString ?? Company.ConnectionController.ConnectionString);
        conn.Open();

        if (conn.State != ConnectionState.Open)
            throw new Exception($"Cannot connect to database {Company.ConnectionController.Database}");
        Conn = conn;
    }

    public override bool         InTransaction => transaction != null;
    public override DatabaseType DatabaseType  => DatabaseType.SQL;

    public override void BeginTransaction() {
        CheckConnection();
        transaction = conn.BeginTransaction("LW-yuval08");
    }

    public override void CommitTransaction() => transaction?.Commit();

    public override void RollbackTransaction() {
        try {
            transaction?.Rollback();
        }
        catch {
            //ignore
        }
    }

    public override void DisposeTransaction() {
        transaction?.Dispose();
        transaction = null;
    }

    public override DataTable GetDataTable(string sqlStr, Parameters parameters = null, CommandType commandType = CommandType.Text) {
        var dt = new DataTable();
        try {
            CheckConnection();

            using var sqlDa = new SqlDataAdapter(sqlStr, conn);
            sqlDa.SelectCommand.CommandType = commandType;
            sqlDa.SelectCommand.Transaction = transaction;
            LoadParameters(parameters, sqlDa.SelectCommand);
            sqlDa.Fill(dt);

            return dt;
        }
        catch (Exception ex) {
            dt.Dispose();
            throw new Exception($"Error executing query: \n{sqlStr}\n{ex.Message}");
        }
    }

    public override void ExecuteReader(string query, Parameters parameters, CommandType commandType, Action<IDataReader> action) {
        bool          withTransaction = true;
        SqlDataReader reader          = null;
        try {
            if (transaction == null) {
                CheckConnection();
                BeginTransaction();
                withTransaction = false;
            }

            using var cmdMain = new SqlCommand(query, conn);
            cmdMain.Transaction = transaction;
            cmdMain.CommandType = commandType;
            LoadParameters(parameters, cmdMain);
            reader = cmdMain.ExecuteReader();
            while (reader.Read())
                action(reader);
            reader.Close();
            if (!withTransaction)
                CommitTransaction();
        }
        catch (Exception ex) {
            if (!withTransaction)
                RollbackTransaction();
            throw new Exception($"Error executing query: \n{query}\n{ex.Message}");
        }
        finally {
            if (reader != null) {
                if (!reader.IsClosed)
                    reader.Close();
                reader.Dispose();
            }

            if (!withTransaction)
                DisposeTransaction();
        }
    }

    protected override void GetValuesExecution(string sqlStr, Parameters parameters, CommandType commandType, Action<IDataReader> readerAction) {
        bool          withTransaction = true;
        SqlDataReader dr              = null;
        try {
            if (transaction == null) {
                CheckConnection();
                BeginTransaction();
                withTransaction = false;
            }

            using var cmdMain = new SqlCommand(sqlStr, conn);
            cmdMain.Transaction = transaction;
            cmdMain.CommandType = commandType;
            LoadParameters(parameters, cmdMain);
            dr = cmdMain.ExecuteReader();
            if (dr.Read())
                readerAction(dr);
            dr.Close();
            if (!withTransaction)
                CommitTransaction();
        }
        catch (Exception ex) {
            if (!withTransaction)
                RollbackTransaction();
            throw new Exception($"Error executing query: \n{sqlStr}\n{ex.Message}");
        }
        finally {
            GetValuesClose(dr);
            if (!withTransaction)
                DisposeTransaction();
        }
    }

    public override int Execute(string sqlStr, Parameters parameters = null, CommandType commandType = CommandType.Text, bool scopeIdentity = false, int? timeout = null) {
        bool withTransaction = true;
        try {
            if (transaction == null) {
                CheckConnection();
                BeginTransaction();
                withTransaction = false;
            }

            if (scopeIdentity && commandType == CommandType.Text)
                sqlStr += ";select SCOPE_IDENTITY()";
            using var cmdMain = new SqlCommand(sqlStr, conn);
            cmdMain.Transaction = transaction;
            cmdMain.CommandType = commandType;
            if (scopeIdentity && commandType == CommandType.StoredProcedure)
                cmdMain.Parameters.Add("@ReturnValue", SqlDbType.Int).Direction = ParameterDirection.ReturnValue;
            if (timeout.HasValue)
                cmdMain.CommandTimeout = timeout.Value;
            LoadParameters(parameters, cmdMain);
            int i = !scopeIdentity || commandType == CommandType.StoredProcedure ? cmdMain.ExecuteNonQuery() : Convert.ToInt32(cmdMain.ExecuteScalar());
            if (scopeIdentity && commandType == CommandType.StoredProcedure)
                i = (int)cmdMain.Parameters["@ReturnValue"].Value;
            if (!withTransaction)
                CommitTransaction();
            return i;
        }
        catch (Exception ex) {
            if (!withTransaction)
                RollbackTransaction();

            throw new Exception($"Error executing query: \n{sqlStr}\n{ex.Message}");
        }
        finally {
            if (!withTransaction)
                DisposeTransaction();
        }
    }

    public override void Dispose() {
        if (conn == null)
            return;
        transaction?.Dispose();
        if (conn.State == ConnectionState.Open)
            conn.Close();
        conn.Dispose();
    }

    public override void CheckConnection() {
        try {
            if (conn == null || conn.State == ConnectionState.Closed)
                ADO_Connection();
        }
        catch {
            ADO_Connection();
        }
    }

    public override void CreateCommonDatabase() => Execute($"CREATE DATABASE LW_YUVAL08_COMMON");

    public override void ChangeDatabase(string dbName) {
        if (conn is { State: ConnectionState.Open })
            conn.ChangeDatabase(dbName);
    }

    private static void LoadParameters(Parameters parameters, SqlCommand cmdMain) {
        if (parameters == null || !parameters.Any())
            return;
        foreach (var param in parameters) {
            var sqlParam = cmdMain.Parameters.Add(param.Name, param.Type);
            if (param.Size != 0)
                sqlParam.Size = param.Size;
            sqlParam.Value     = param.Value ?? DBNull.Value;
            sqlParam.Direction = param.Direction;
        }
    }

    public static SqlCommand ConvertToCommand(Procedure proc) {
        var cm = new SqlCommand(proc.Name) { CommandType = CommandType.StoredProcedure };
        proc.Parameters.ForEach(value => {
            var parameter = cm.Parameters.Add($"@{value.Name}", value.Type);
            if (value.Size > 0)
                parameter.Size = value.Size;
            parameter.Direction = value.Direction;
            parameter.Value     = value.Value ?? DBNull.Value;
        });
        return cm;
    }
}