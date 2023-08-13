using System.Data;
using System.Text.RegularExpressions;
using Service.Shared.Company;
using Service.Shared.Utils;

namespace Service.Shared.Data; 

public class Query {
    /// <summary>
    /// Gets or sets the Query String
    /// </summary>
    public string String { get; set; }

    /// <summary>
    /// Gets or sets the Query parameters list
    /// </summary>
    public Parameters Parameters { get; set; } = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="Query" /> class. 
    /// </summary>
    /// <param name="string">Query string</param>
    /// <param name="parameters">Parameters array</param>
    /// <remarks></remarks>
    public Query(string @string, params Parameter[] parameters) {
        String = @string;
        if (parameters.Length > 0)
            Parameters.AddRange(parameters);
    }

    /// <summary>
    /// Function that takes the query string and parameters and builds a string that can be used with the
    /// SAPbobsCOM.Recordset object.
    /// </summary>
    /// <returns></returns>
    /// <remarks></remarks>
    public override string ToString() {
        string value  = String;
        var    dbType = ConnectionController.DatabaseType;
        foreach (var p in Parameters)
            if (p.Value != null) {
                value = p.Type switch {
                    SqlDbType.NVarChar or SqlDbType.VarChar or SqlDbType.Char or SqlDbType.NText or SqlDbType.DateTime => dbType switch {
                        DatabaseType.HANA => Regex.Replace(value, $":{p.Name}", $"n'{p.Value.ToString().ToQuery()}'"),
                        _                 => Regex.Replace(value, $"@{p.Name}", $"N'{p.Value.ToString().ToQuery()}'")
                    },
                    _ => dbType switch {
                        DatabaseType.HANA => Regex.Replace(value, $":{p.Name}", p.Value.ToString()),
                        _                 => Regex.Replace(value, $"@{p.Name}", p.Value.ToString())
                    }
                };
            }
            else {
                value = dbType switch {
                    DatabaseType.HANA => Regex.Replace(value, $":{p.Name}", "NULL"),
                    _                 => Regex.Replace(value, $"@{p.Name}", "NULL")
                };
            }

        return value;
    }
}