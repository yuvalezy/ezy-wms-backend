using System.Data;
using Core.Enums;
using Microsoft.Data.SqlClient;

namespace Adapters.Common.SBO.Repositories;

internal static class SboPickingRepositoryHelpers {
    internal static ObjectStatus ConvertStatus(string? status) {
        return status?.ToUpper() switch {
            "Y" => ObjectStatus.Closed,
            "N" => ObjectStatus.Open,
            "C" => ObjectStatus.Cancelled,
            "P" => ObjectStatus.InProgress,
            _ => ObjectStatus.Open
        };
    }

    internal static SqlParameter[] ConvertToSqlParameters(Dictionary<string, object> parameters) {
        return parameters.Select(p =>
        {
            var param = new SqlParameter(p.Key, p.Value ?? DBNull.Value);

            switch (p.Key) {
                // Set specific types for known parameters
                case "@WhsCode":
                    param.SqlDbType = SqlDbType.NVarChar;
                    param.Size = 8;
                    break;
                case "@ItemCode":
                    param.SqlDbType = SqlDbType.NVarChar;
                    param.Size = 50;
                    break;
                default: {
                    if (p.Key.Contains("Entry") || p.Key.Contains("Type") || p.Key == "@ID") {
                        param.SqlDbType = SqlDbType.Int;
                    }
                    else if (p.Key == "@Date") {
                        param.SqlDbType = SqlDbType.DateTime;
                    }

                    break;
                }
            }

            return param;
        }).ToArray();
    }

    internal static string StringAggregateDistinct(string value) =>
        value.Length == 0 ? value : value.Split(',').Distinct().Aggregate((a, b) => a + ", " + b);
}
