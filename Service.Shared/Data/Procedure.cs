using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using Sap.Data.Hana;
using SAPbobsCOM;
using Service.Shared.Company;
using Service.Shared.Utils;

namespace Service.Shared.Data; 

/// <summary>
/// This is used to easily create an sql string to execute stored procedure using
/// the SAPbobsCOM.Recordset object.
/// </summary>
/// <example>
///   <para>In this example I'm executing the stored procedure
/// UpdatePricesLog</para>
///   <code lang="C#"><![CDATA[
/// try {
///     Procedure proc = new Procedure("UpdatePricesLog", 
///        new Parameter("RUNID", SqlDbType.Int, runid), 
///        new Parameter("RUNID2", SqlDbType.Int, Runid2), 
///        new Parameter("ItemCode", itemcode), 
///        new Parameter("ActionType", 'UpdPrice'), 
///        new Parameter("Result", result), 
///        new Parameter("ResultCode", 0), 
///        new Parameter("ResultMsg", ""), 
///        new Parameter("UserSign", SqlDbType.Int, ConnectionController.Company.UserSignature));
///     rs.DoQuery(proc.Query());
/// }
/// catch (Exception) {
/// }]]></code>
///   <para></para>
/// </example>
public class Procedure {
    /// <summary>
    /// Gets or sets the procedure name
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Gets or sets the procedure parameters list
    /// </summary>
    public Parameters Parameters { get; set; } = new();


    /// <summary>
    /// Helps to easily find a parameter in the parameters collection
    /// </summary>
    /// <param name="name">Parameter name</param>
    /// <example>
    ///   <para>In this example I have a parameters collection called myParams and I want to
    /// change the ItemCode variable value to "ABC"</para>
    ///   <para></para>
    ///   <code lang="C#"><![CDATA[myParams.Parameters["ItemCode"].Value = "ABC";]]></code>
    ///   <para></para>
    /// </example>
    public Parameter this[string name] => Parameters.FirstOrDefault(p => p.Name == name);

    /// <summary>
    /// Initializes a new instance of the <see cref="Procedure" /> class. 
    /// </summary>
    /// <param name="name">Procedure name</param>
    /// <param name="parameters">Parameters array</param>
    /// <remarks></remarks>
    public Procedure(string name, params Parameter[] parameters) {
        Name = name;
        if (parameters.Length > 0)
            Parameters.AddRange(parameters);
    }

    /// <summary>
    /// Function that takes the procedure name and parameters and builds a string that can be used with the
    /// SAPbobsCOM.Recordset object.
    /// </summary>
    /// <returns></returns>
    /// <remarks></remarks>
    public override string ToString() {
        var sb     = new StringBuilder();
        var dbType = ConnectionController.DatabaseType;
        var outParams = Parameters.Where(v =>
                v.Direction is ParameterDirection.Output or ParameterDirection.InputOutput)
            .ToArray();
        if (outParams.Length > 0) {
            if (dbType == DatabaseType.HANA)
                sb.Append("do begin ");
            foreach (var outParam in outParams) {
                sb.Append($"declare {(dbType == DatabaseType.SQL ? "@" : "")}{outParam.Name} ");
                switch (outParam.Type) {
                    case SqlDbType.DateTime when dbType == DatabaseType.HANA:
                        sb.Append("timestamp");
                        break;
                    case SqlDbType.Float or SqlDbType.Decimal or SqlDbType.Money:
                        switch (dbType) {
                            case DatabaseType.SQL:
                                sb.Append("numeric(19, 6)");
                                break;
                            case DatabaseType.HANA:
                                sb.Append("decimal(21, 6)");
                                break;
                        }

                        break;
                    default:
                        sb.Append(outParam.Type.ToString());
                        if (outParam.Type is SqlDbType.NVarChar or SqlDbType.VarChar or SqlDbType.Char)
                            sb.Append($"({outParam.Size})");
                        break;
                }

                sb.Append(";\n");
            }
        }

        sb.Append(dbType == DatabaseType.SQL ? "exec" : "call");
        sb.Append($" {Name} ");
        if (dbType == DatabaseType.HANA)
            sb.Append("(");
        for (int i = 0; i < Parameters.Count; i++) {
            var p = Parameters[i];
            if (i > 0) sb.Append(", ");
            bool isString = p.Type is SqlDbType.NVarChar or SqlDbType.NText && p.Value != null;
            switch (dbType) {
                case DatabaseType.HANA:
                    sb.Append($"{p.Name} => ");
                    if (isString) sb.Append("n");
                    break;
                default:
                    sb.Append($"@{p.Name} = ");
                    if (isString) sb.Append("N");
                    break;
            }

            if (p.Value != null && p.Direction == ParameterDirection.Input) {
                AppendValue();
            }
            else if (p.Direction is ParameterDirection.Output or ParameterDirection.InputOutput) {
                sb.Append($"{(dbType == DatabaseType.SQL ? "@" : ":")}{p.Name}");
            }
            else {
                sb.Append("NULL");
            }

            void AppendValue() {
                switch (p.Type) {
                    case SqlDbType.NVarChar or SqlDbType.VarChar or SqlDbType.Char or SqlDbType.NText:
                        sb.Append($"'{p.Value.ToString().ToQuery()}'");
                        break;
                    case SqlDbType.DateTime:
                        sb.Append($"'{(DateTime)p.Value:yyyy-MM-ddTHH:mm:ss}'");
                        break;
                    case SqlDbType.Float:
                        switch (p.Value) {
                            case int intValue:
                                sb.Append(intValue);
                                break;
                            case double doubleValue:
                                sb.Append(doubleValue.ToParseValue());
                                break;
                            case decimal decimalValue:
                                sb.Append(decimalValue.ToParseValue());
                                break;
                        }

                        break;
                    default:
                        sb.Append(p.Value);
                        break;
                }
            }
        }

        if (dbType == DatabaseType.HANA)
            sb.Append(")");
        if (outParams.Length > 0) {
            sb.Append("; ");
            sb.Append("select ");
            for (var i = 0; i < outParams.Length; i++) {
                if (i > 0)
                    sb.Append(", ");
                sb.Append($"{(dbType == DatabaseType.SQL ? "@" : ":")}{outParams[i].Name} \"{outParams[i].Name}\"");
            }

            if (dbType == DatabaseType.HANA)
                sb.Append("from DUMMY; end");
        }

        return sb.ToString();
    }

    public void                  Execute()                      => ToString().ExecuteQuery();
    public Recordset             ExecuteRecordset()             => ToString().ExecuteRecordset();
    public T                     ExecuteValue<T>()              => ToString().ExecuteQueryValue<T>();
    public Tuple<T1, T2>         ExecuteValue<T1, T2>()         => ToString().ExecuteQueryValue<T1, T2>();
    public Tuple<T1, T2, T3>     ExecuteValue<T1, T2, T3>()     => ToString().ExecuteQueryValue<T1, T2, T3>();
    public Tuple<T1, T2, T3, T4> ExecuteValue<T1, T2, T3, T4>() => ToString().ExecuteQueryValue<T1, T2, T3, T4>();

    public Tuple<T1, T2, T3, T4, T5> ExecuteValue<T1, T2, T3, T4, T5>() =>
        ToString().ExecuteQueryValue<T1, T2, T3, T4, T5>();

    public Tuple<T1, T2, T3, T4, T5, T6> ExecuteValue<T1, T2, T3, T4, T5, T6>() =>
        ToString().ExecuteQueryValue<T1, T2, T3, T4, T5, T6>();

    public Tuple<T1, T2, T3, T4, T5, T6, T7> ExecuteValue<T1, T2, T3, T4, T5, T6, T7>() =>
        ToString().ExecuteQueryValue<T1, T2, T3, T4, T5, T6, T7>();

    public List<T>                ExecuteList<T>()              => ToString().ExecuteQueryList<T>();
    public List<(T1, T2)>         ExecuteList<T1, T2>()         => ToString().ExecuteQueryList<T1, T2>();
    public List<(T1, T2, T3)>     ExecuteList<T1, T2, T3>()     => ToString().ExecuteQueryList<T1, T2, T3>();
    public List<(T1, T2, T3, T4)> ExecuteList<T1, T2, T3, T4>() => ToString().ExecuteQueryList<T1, T2, T3, T4>();

    public List<(T1, T2, T3, T4, T5)> ExecuteList<T1, T2, T3, T4, T5>() =>
        ToString().ExecuteQueryList<T1, T2, T3, T4, T5>();

    public List<(T1, T2, T3, T4, T5, T6)> ExecuteList<T1, T2, T3, T4, T5, T6>() =>
        ToString().ExecuteQueryList<T1, T2, T3, T4, T5, T6>();

    public List<(T1, T2, T3, T4, T5, T6, T7)> ExecuteList<T1, T2, T3, T4, T5, T6, T7>() =>
        ToString().ExecuteQueryList<T1, T2, T3, T4, T5, T6, T7>();

    public List<T> ExecuteReader<T>() where T : class    => ToString().ExecuteQueryReader<T>();
    public T       ExecuteReaderRow<T>() where T : class => ToString().ExecuteQueryReaderRow<T>();

    public SqlCommand ConvertToSqlCommand() {
        var cm = new SqlCommand(Name) { CommandType = CommandType.StoredProcedure };
        Parameters.ForEach(value => {
            var parameter = cm.Parameters.Add($"@{value.Name}", value.Type);
            if (value.Size > 0)
                parameter.Size = value.Size;
            parameter.Direction = value.Direction;
            parameter.Value     = value.Value ?? DBNull.Value;
        });
        return cm;
    }

    public HanaCommand ConvertToHanaCommand() {
        var cm = new HanaCommand(Name){CommandType = CommandType.StoredProcedure};
        Parameters.ForEach(value => {
            var parameter = cm.Parameters.Add(value.Name, value.Type);
            if (value.Size > 0)
                parameter.Size = value.Size;
            parameter.Direction = value.Direction;
            parameter.Value     = value.Value ?? DBNull.Value;
        });
        return cm;
    }
}