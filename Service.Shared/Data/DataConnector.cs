using System;
using System.Data;
using System.Text;
using ConnectionController = Service.Shared.Company.ConnectionController;

namespace Service.Shared.Data;

public abstract class DataConnector : IDisposable {
    private string connectionString;
    protected DataConnector(string connectionString = null) => ConnectionString = connectionString;

    public string ConnectionString {
        get => connectionString;
        set {
            if (Conn is { State: ConnectionState.Open })
                Conn.Close();
            connectionString = value;
        }
    }

    public static DataConnector GetConnector(string connectionString = null) =>
        ConnectionController.DatabaseType == DatabaseType.SQL ? new SQLDataConnector(connectionString) : new HANADataConnector(connectionString);

    public          IDbConnection Conn          { get; set; }
    public abstract bool          InTransaction { get; }
    public abstract DatabaseType  DatabaseType  { get; }

    public abstract void BeginTransaction();
    public abstract void CommitTransaction();
    public abstract void RollbackTransaction();
    public abstract void DisposeTransaction();

    public T GetValue<T>(Procedure procedure) =>
        GetValue<T>(procedure.Name, procedure.Parameters, CommandType.StoredProcedure);

    public T GetValue<T>(string query, Parameter parameter, CommandType commandType = CommandType.Text) {
        var returnValue = default(T);
        GetValuesExecution(query, [parameter], commandType, dr => returnValue = ReadValue<T>(dr[0]));
        return returnValue;
    }

    public T GetValue<T>(string query, Parameters parameters = null, CommandType commandType = CommandType.Text) {
        var returnValue = default(T);
        GetValuesExecution(query, parameters, commandType, dr => returnValue = ReadValue<T>(dr[0]));
        return returnValue;
    }

    public Tuple<T1, T2> GetValue<T1, T2>(Procedure procedure) => GetValue<T1, T2>(procedure.Name, procedure.Parameters, CommandType.StoredProcedure);

    public Tuple<T1, T2> GetValue<T1, T2>(string query, Parameter parameter, CommandType commandType = CommandType.Text) {
        var returnValue = new Tuple<T1, T2>(default, default);
        GetValuesExecution(query, [parameter], commandType,
            dr => returnValue = new Tuple<T1, T2>(ReadValue<T1>(dr[0]), ReadValue<T2>(dr[1])));
        return returnValue;
    }

    public Tuple<T1, T2> GetValue<T1, T2>(string query, Parameters parameters = null, CommandType commandType = CommandType.Text) {
        var returnValue = new Tuple<T1, T2>(default, default);
        GetValuesExecution(query, parameters, commandType,
            dr => returnValue = new Tuple<T1, T2>(ReadValue<T1>(dr[0]), ReadValue<T2>(dr[1])));
        return returnValue;
    }

    public Tuple<T1, T2, T3> GetValue<T1, T2, T3>(Procedure procedure) => GetValue<T1, T2, T3>(procedure.Name, procedure.Parameters, CommandType.StoredProcedure);


    public Tuple<T1, T2, T3> GetValue<T1, T2, T3>(string query, Parameter parameter, CommandType commandType = CommandType.Text) => GetValue<T1, T2, T3>(query, [parameter], commandType);

    public Tuple<T1, T2, T3> GetValue<T1, T2, T3>(string query, Parameters parameters = null, CommandType commandType = CommandType.Text) {
        var returnValue = new Tuple<T1, T2, T3>(default, default, default);
        GetValuesExecution(query, parameters, commandType,
            dr => returnValue =
                new Tuple<T1, T2, T3>(ReadValue<T1>(dr[0]), ReadValue<T2>(dr[1]), ReadValue<T3>(dr[2])));
        return returnValue;
    }

    public Tuple<T1, T2, T3, T4> GetValue<T1, T2, T3, T4>(Procedure procedure) => GetValue<T1, T2, T3, T4>(procedure.Name, procedure.Parameters, CommandType.StoredProcedure);
    public Tuple<T1, T2, T3, T4> GetValue<T1, T2, T3, T4>(string query, Parameter parameter, CommandType commandType = CommandType.Text) => GetValue<T1, T2, T3, T4>(query, [parameter], commandType);

    public Tuple<T1, T2, T3, T4> GetValue<T1, T2, T3, T4>(string query, Parameters parameters = null, CommandType commandType = CommandType.Text) {
        var returnValue = new Tuple<T1, T2, T3, T4>(default, default, default, default);
        GetValuesExecution(query, parameters, commandType,
            dr => returnValue = new Tuple<T1, T2, T3, T4>(ReadValue<T1>(dr[0]), ReadValue<T2>(dr[1]), ReadValue<T3>(dr[2]), ReadValue<T4>(dr[3])));
        return returnValue;
    }

    public Tuple<T1, T2, T3, T4, T5> GetValue<T1, T2, T3, T4, T5>(Procedure procedure) =>
        GetValue<T1, T2, T3, T4, T5>(procedure.Name, procedure.Parameters, CommandType.StoredProcedure);

    public Tuple<T1, T2, T3, T4, T5> GetValue<T1, T2, T3, T4, T5>(string query, Parameters parameters = null, CommandType commandType = CommandType.Text) {
        var returnValue = new Tuple<T1, T2, T3, T4, T5>(default, default, default, default, default);
        GetValuesExecution(query, parameters, commandType,
            dr => returnValue = new Tuple<T1, T2, T3, T4, T5>(ReadValue<T1>(dr[0]), ReadValue<T2>(dr[1]), ReadValue<T3>(dr[2]), ReadValue<T4>(dr[3]), ReadValue<T5>(dr[4])));
        return returnValue;
    }

    public Tuple<T1, T2, T3, T4, T5, T6> GetValue<T1, T2, T3, T4, T5, T6>(Procedure procedure) =>
        GetValue<T1, T2, T3, T4, T5, T6>(procedure.Name, procedure.Parameters, CommandType.StoredProcedure);

    public Tuple<T1, T2, T3, T4, T5, T6> GetValue<T1, T2, T3, T4, T5, T6>(string query, Parameters parameters = null, CommandType commandType = CommandType.Text) {
        var returnValue = new Tuple<T1, T2, T3, T4, T5, T6>(default, default, default, default, default, default);
        GetValuesExecution(query, parameters, commandType,
            dr => returnValue = new Tuple<T1, T2, T3, T4, T5, T6>(ReadValue<T1>(dr[0]), ReadValue<T2>(dr[1]), ReadValue<T3>(dr[2]), ReadValue<T4>(dr[3]), ReadValue<T5>(dr[4]),
                ReadValue<T6>(dr[5])));
        return returnValue;
    }

    public Tuple<T1, T2, T3, T4, T5, T6, T7> GetValue<T1, T2, T3, T4, T5, T6, T7>(Procedure procedure) =>
        GetValue<T1, T2, T3, T4, T5, T6, T7>(procedure.Name, procedure.Parameters, CommandType.StoredProcedure);

    public Tuple<T1, T2, T3, T4, T5, T6, T7> GetValue<T1, T2, T3, T4, T5, T6, T7>(string query, Parameters parameters = null, CommandType commandType = CommandType.Text) {
        var returnValue =
            new Tuple<T1, T2, T3, T4, T5, T6, T7>(default, default, default, default, default, default, default);
        GetValuesExecution(query, parameters, commandType,
            dr => returnValue = new Tuple<T1, T2, T3, T4, T5, T6, T7>(ReadValue<T1>(dr[0]), ReadValue<T2>(dr[1]), ReadValue<T3>(dr[2]), ReadValue<T4>(dr[3]), ReadValue<T5>(dr[4]),
                ReadValue<T6>(dr[5]), ReadValue<T7>(dr[6])));
        return returnValue;
    }

    private  T ReadValue<T>(object readData) {
        if (readData is T data)
            return data;

        if (readData is null || readData == DBNull.Value)
            return default;

        var readValueType = ReadValueType.Default;
        var type          = typeof(T);
        if (type == typeof(bool))
            readValueType = ReadValueType.Boolean;
        else if (type.IsEnum)
            readValueType = ReadValueType.Enum;
        else if (IsHanaDecimal(readData))
            readValueType = ReadValueType.HanaDecimal;
        try {
            return readValueType switch {
                ReadValueType.Boolean => (T)Convert.ChangeType(readData.ToString().ToLower() is "y" or "1" or "true",
                    type),
                ReadValueType.Enum        => (T)readData,
                ReadValueType.HanaDecimal => (T)Convert.ChangeType(ReadHanaDecimal(readData), type),
                _                         => (T)Convert.ChangeType(readData, type)
            };
        }
        catch (InvalidCastException) {
            return default;
        }
    }
    /* Cannot Use Sap.Data.Hana.HanaDecimal, it will cause an exception if it's not installed in an SQL environment */
    protected virtual bool    IsHanaDecimal(object   readData) => false;
    protected virtual decimal ReadHanaDecimal(object readData) => 0;
    /* Cannot Use Sap.Data.Hana.HanaDecimal, it will cause an exception if it's not installed in an SQL environment */

    private enum ReadValueType {
        Default,
        Boolean,
        Enum,
        HanaDecimal
    }

    protected static void GetValuesClose(IDataReader dr) {
        if (dr == null)
            return;
        if (!dr.IsClosed)
            dr.Close();
        dr.Dispose();
    }

    public void Execute(Procedure procedure) => Execute(procedure.Name, procedure.Parameters, CommandType.StoredProcedure);

    public int Execute(string query, Parameter parameter, CommandType commandType = CommandType.Text, bool scopeIdentity = false, int? timeout = null) =>
        Execute(query, new Parameters(parameter), commandType, scopeIdentity, timeout);

    public abstract int  Execute(string              query, Parameters          parameters = null, CommandType commandType = CommandType.Text, bool scopeIdentity = false, int? timeout = null);
    public          void ExecuteReader(Procedure     proc,  Action<IDataReader> action) => ExecuteReader(proc.Name, proc.Parameters, CommandType.StoredProcedure, action);
    public          void ExecuteReader(string        query, Action<IDataReader> action) => ExecuteReader(query, null, CommandType.Text, action);
    public          void ExecuteReader(StringBuilder sb,    Action<IDataReader> action) => ExecuteReader(sb.ToString(), null, CommandType.Text, action);
    public          void ExecuteReader(string        query, Parameter           parameter,  Action<IDataReader> action) => ExecuteReader(query, [parameter], CommandType.Text, action);
    public          void ExecuteReader(StringBuilder sb,    Parameter           parameter,  Action<IDataReader> action) => ExecuteReader(sb.ToString(), [parameter], CommandType.Text, action);
    public          void ExecuteReader(string        query, Parameters          parameters, Action<IDataReader> action) => ExecuteReader(query, parameters, CommandType.Text, action);
    public abstract void ExecuteReader(string        query, Parameters          parameters, CommandType         commandType, Action<IDataReader> action);

    public DataTable GetDataTable(string query, Parameter parameter, CommandType commandType = CommandType.Text) => GetDataTable(query, new Parameters(parameter), commandType);
    public abstract DataTable GetDataTable(string query, Parameters parameters = null, CommandType commandType = CommandType.Text);
    protected abstract void GetValuesExecution(string query, Parameters parameters, CommandType commandType, Action<IDataReader> readerAction);
    public abstract void Dispose();
    public abstract void CheckConnection();
    public abstract void CreateCommonDatabase();
    public abstract void ChangeDatabase(string dbName);
}