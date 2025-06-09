using Microsoft.Data.SqlClient;

namespace Adapters.Windows.Utils;

public static class SqlQueryUtils {
    public static string BuildInClause(string columnName, string parameterPrefix, int valueCount) {
        string[] paramNames = Enumerable.Range(1, valueCount)
            .Select(i => $"@{parameterPrefix}{i}")
            .ToArray();

        return $" where \"{columnName}\" in ({string.Join(", ", paramNames)})";
    }

    public static SqlParameter[]? BuildInParameters(string parameterPrefix, string[] values, SqlParameter[]? parameters) {
        if (parameters == null) {
            parameters = new SqlParameter[values.Length];
        } else {
            Array.Resize(ref parameters, parameters.Length + values.Length);
        }
        for (int i = 0; i < values.Length; i++) {
            parameters[i + (parameters.Length - values.Length)] = new SqlParameter($"@{parameterPrefix}{i + 1}", values[i]);
        }

        return parameters;
    }
}