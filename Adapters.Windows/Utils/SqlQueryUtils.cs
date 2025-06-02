namespace Adapters.Windows.Utils;

public static class SqlQueryUtils {
    public static string BuildInClause(string columnName, string parameterPrefix, int valueCount) {
        string[] paramNames = Enumerable.Range(1, valueCount)
            .Select(i => $"@{parameterPrefix}{i}")
            .ToArray();

        return $" where \"{columnName}\" in ({string.Join(", ", paramNames)})";
    }

    public static object BuildInParameters(string parameterPrefix, string[] values) {
        var parameters = new Dictionary<string, object>();
        for (int i = 0; i < values.Length; i++) {
            parameters[$"{parameterPrefix}{i + 1}"] = values[i];
        }

        return parameters;
    }
}