using System;
using System.Data;
using System.Linq;
using Sap.Data.Hana;
using ConnectionController = Service.Shared.Company.ConnectionController;

namespace Service.Shared.Data;

public class HANADataConnector : DataConnector {
    private HanaConnection  conn;
    private HanaTransaction transaction;

    public HANADataConnector(string connectionString = null) : base(connectionString) {
    }

    private void ADO_Connection() {
        try {
            conn?.Close();
        }
        catch (Exception) {
            // ignored
        }

        conn?.Dispose();
        conn = new HanaConnection(ConnectionString??ConnectionController.ConnectionString);
        conn.Open();

        if (conn.State != ConnectionState.Open)
            throw new Exception($"Cannot connect to database {ConnectionController.Database}");

        Conn = conn;
    }


    public override bool         InTransaction      => transaction != null;
    public override DatabaseType DatabaseType       => DatabaseType.HANA;
    public override void         BeginTransaction() => transaction = conn.BeginTransaction();

    public override void CommitTransaction() => transaction.Commit();

    public override void RollbackTransaction() {
        try {
            transaction?.Rollback();
        }
        catch {
            //ignore
        }
    }

    public override void DisposeTransaction() {
        transaction.Dispose();
        transaction = null;
    }

    public override DataTable GetDataTable(string hanaStr, Parameters parameters = null, CommandType commandType = CommandType.Text) {
        var dt = new DataTable();
        try {
            CheckConnection();

            using var hanaDa = new HanaDataAdapter(hanaStr, conn) {
                SelectCommand = {
                    Transaction = transaction,
                    CommandType = commandType
                }
            };
            LoadParameters(parameters, hanaDa.SelectCommand);
            hanaDa.Fill(dt);

            return dt;
        }
        catch (Exception ex) {
            dt.Dispose();
            throw new Exception($"Error executing query: \n{hanaStr}\n{ex.Message}");
        }
    }


    public override void ExecuteReader(string query, Parameters parameters, CommandType commandType, Action<IDataReader> action) {
        bool           withTransaction = true;
        HanaDataReader reader          = null;
        try {
            if (transaction == null) {
                CheckConnection();
                BeginTransaction();
                withTransaction = false;
            }

            using var cmdMain = new HanaCommand(query, conn) {
                Transaction = transaction,
                CommandType = commandType
            };
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
        }
    }

    protected override void GetValuesExecution(string query, Parameters parameters, CommandType commandType, Action<IDataReader> readerAction) {
        HanaDataReader dr              = null;
        bool           withTransaction = true;
        try {
            if (transaction == null) {
                CheckConnection();
                BeginTransaction();
                withTransaction = false;
            }

            using var cmdMain = new HanaCommand(query, conn) {
                Transaction = transaction,
                CommandType = commandType
            };
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
            throw new Exception($"Error executing query: \n{query}\n{ex.Message}");
        }
        finally {
            GetValuesClose(dr);
        }
    }
    protected override    bool    IsHanaDecimal(object   readData) => readData is HanaDecimal;
    protected override decimal ReadHanaDecimal(object readData) => ((HanaDecimal)readData).ToDecimal();

    public override int Execute(string query, Parameters parameters = null, CommandType commandType = CommandType.Text, bool scopeIdentity = false, int? timeout = null) {
        bool withTransaction = true;
        try {
            if (transaction == null) {
                CheckConnection();
                BeginTransaction();
                withTransaction = false;
            }


            using var cmdMain = new HanaCommand(query, conn) {
                Transaction = transaction,
                CommandType = commandType
            };
            if (scopeIdentity && commandType == CommandType.StoredProcedure)
                cmdMain.Parameters.Add("ReturnValue", SqlDbType.Int).Direction = ParameterDirection.Output;
            if (timeout.HasValue)
                cmdMain.CommandTimeout = timeout.Value;
            LoadParameters(parameters, cmdMain);
            int i = !scopeIdentity || commandType == CommandType.StoredProcedure ? cmdMain.ExecuteNonQuery() : Convert.ToInt32(cmdMain.ExecuteScalar());
            if (scopeIdentity && commandType == CommandType.StoredProcedure)
                i = (int)cmdMain.Parameters["ReturnValue"].Value;
            LoadOutputParametersValues(parameters, cmdMain);
            if (!withTransaction)
                CommitTransaction();
            return i;
        }
        catch (Exception ex) {
            if (!withTransaction)
                RollbackTransaction();
            throw new Exception($"Error executing query: \n{query}\n{ex.Message}");
        }
    }

    public override void Dispose() {
        if (conn == null)
            return;
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

    public override void CreateCommonDatabase() {
        throw new NotImplementedException();
    }

    public override void ChangeDatabase(string dbName) {
        throw new NotImplementedException();
    }


    private static void LoadParameters(Parameters parameters, HanaCommand cmdMain) {
        if (parameters == null || !parameters.Any()) 
            return;
        foreach (var param in parameters) {
            var sqlParam = cmdMain.Parameters.Add(param.Name, param.Type);
            if (param.Size != 0)
                sqlParam.Size = param.Size;
            sqlParam.Value = param.Value ?? DBNull.Value;
            sqlParam.Direction = param.Direction;
        }
    }

    private void LoadOutputParametersValues(Parameters parameters, HanaCommand cmdMain) {
        if (parameters == null) 
            return;
        foreach (var parameter in parameters.Where(v => v.Direction is ParameterDirection.Output or ParameterDirection.InputOutput)) {
            parameter.Value = cmdMain.Parameters[parameter.Name].Value;
        }
    }
    public static decimal GetHanaDecimal(object value) => ((HanaDecimal)value).ToDecimal();
    public static HanaCommand ConvertToCommand(Procedure proc) {
        var cm = new HanaCommand(proc.Name){CommandType = CommandType.StoredProcedure};
        proc.Parameters.ForEach(value => {
            var parameter = cm.Parameters.Add(value.Name, value.Type);
            if (value.Size > 0)
                parameter.Size = value.Size;
            parameter.Direction = value.Direction;
            parameter.Value     = value.Value ?? DBNull.Value;
        });
        return cm;
    }
}