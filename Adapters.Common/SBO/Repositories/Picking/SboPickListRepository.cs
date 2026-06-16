using System.Data;
using System.Text;
using Adapters.Common.SBO.Services;
using Core.DTOs.PickList;
using Core.Interfaces;
using Microsoft.Data.SqlClient;

namespace Adapters.Common.SBO.Repositories;

public class SboPickListRepository(SboDatabaseService dbService, ISettings settings) {
    public async Task<IEnumerable<PickingDocumentResponse>> GetPickLists(PickListsRequest request, string warehouse) {
        var pickPackOnly = settings.Filters.PickPackOnly;
        var sb = new StringBuilder(
            """
            SELECT
                PICKS."AbsEntry",
                PICKS."PickDate",
                COALESCE(Cast(PICKS."Remarks" as varchar(8000)), '') AS "Remarks",
                PICKS."Status" "Status",
                   STRING_AGG(CASE WHEN PKL1."BaseObject" = 17 THEN T2."Ref1" END, ',')          AS "SalesOrders",
                   STRING_AGG(CASE WHEN PKL1."BaseObject" = 13 THEN T2."Ref1" END, ',')          AS "Invoices",
                   STRING_AGG(CASE WHEN PKL1."BaseObject" = 1250000001 THEN T2."Ref1" END, ',')  AS "Transfers",
                COALESCE(SUM(PKL1."RelQtty" + PKL1."PickQtty"), 0) AS "Quantity",
                COALESCE(SUM(PKL1."RelQtty"), 0) AS "OpenQuantity",
                COALESCE(SUM(CASE WHEN PKL1."PickStatus" = 'Y' THEN PKL1."PickQtty" ELSE 0 END), 0) AS "UpdateQuantity"
            """);

        if (pickPackOnly is not null) {
            sb.Append($", Max(Case When {pickPackOnly.Query} Then 1 Else 0 End) \"PickPackOnly\" ");
        }

        sb.Append("""
                  FROM OPKL PICKS
                  LEFT JOIN PKL1 ON PKL1."AbsEntry" = PICKS."AbsEntry"
                  inner join OILM T2 on T2."TransType" = PKL1."BaseObject" and T2.DocEntry = PKL1."OrderEntry" and T2."DocLineNum" = PKL1."OrderLine"
                  """);

        if (pickPackOnly is not null) {
            sb.Append(""" left outer join OCRD on OCRD."CardCode" = T2."BPCardCode" """);
        }

        sb.Append("""
                  WHERE T2."LocCode" = @WhsCode
                  AND PICKS."Status" IN ('R', 'P', 'D', 'Y')
                  """);

        var parameters = new List<SqlParameter> {
            new("@WhsCode", SqlDbType.NVarChar, 8) { Value = warehouse }
        };

        if (request.ID.HasValue) {
            sb.AppendLine(" AND PICKS.\"AbsEntry\" = @AbsEntry");
            parameters.Add(new SqlParameter("@AbsEntry", SqlDbType.Int) { Value = request.ID.Value });
        }

        if (request.Date.HasValue) {
            sb.AppendLine(" AND DATEDIFF(day, PICKS.\"PickDate\", @Date) = 0");
            parameters.Add(new SqlParameter("@Date", SqlDbType.DateTime) { Value = request.Date.Value });
        }

        if (request.Statuses?.Length > 0) {
            string statusPlaceholders = string.Join(", ", request.Statuses.Select((_, i) => $"@Status{i}"));
            sb.AppendLine($" AND PICKS.\"Status\" IN ({statusPlaceholders})");

            for (int i = 0; i < request.Statuses.Length; i++) {
                parameters.Add(new SqlParameter($"@Status{i}", SqlDbType.Char, 1) { Value = (char)request.Statuses[i] });
            }
        }

        sb.AppendLine(""" GROUP BY PICKS."AbsEntry", PICKS."PickDate", Cast(PICKS."Remarks" as varchar(8000)), PICKS."Status" """);

        sb.AppendLine(""" ORDER BY PICKS."AbsEntry" DESC""");

        var sqlParams = parameters.ToArray();

        var response = await dbService.QueryAsync(sb.ToString(), sqlParams, reader =>
        {
            var document = new PickingDocumentResponse {
                Entry = reader.GetInt32(0),
                Date = reader.GetDateTime(1),
                Remarks = reader.IsDBNull(2) ? null : reader.GetString(2),
                Status = SboPickingRepositoryHelpers.ConvertStatus(reader.IsDBNull(3) ? null : reader.GetString(3)),
                SalesOrders = reader.IsDBNull(4) ? null : SboPickingRepositoryHelpers.StringAggregateDistinct(reader.GetString(4)),
                Invoices = reader.IsDBNull(5) ? null : SboPickingRepositoryHelpers.StringAggregateDistinct(reader.GetString(5)),
                Transfers = reader.IsDBNull(6) ? null : SboPickingRepositoryHelpers.StringAggregateDistinct(reader.GetString(6)),
                Quantity = reader.GetDecimal(7),
                OpenQuantity = reader.GetDecimal(8),
                UpdateQuantity = reader.GetDecimal(9),
                PickPackOnly = pickPackOnly is not null && Convert.ToBoolean(reader["PickPackOnly"])
            };

            return document;
        });

        return response;
    }

    public async Task<Dictionary<int, bool>> GetPickListStatuses(int[] absEntries) {
        if (absEntries.Length == 0) {
            return new Dictionary<int, bool>();
        }

        string placeholders = string.Join(", ", absEntries.Select((_, i) => $"@AbsEntry{i}"));
        string query = $"""
                        SELECT
                            OPKL."AbsEntry",
                            CASE WHEN OPKL."Status" IN ('R', 'P', 'D') THEN 1 ELSE 0 END AS "IsOpen"
                        FROM OPKL
                        WHERE OPKL."AbsEntry" IN ({placeholders})
                        """;

        var sqlParams = new List<SqlParameter>();
        for (int i = 0; i < absEntries.Length; i++) {
            sqlParams.Add(new SqlParameter($"@AbsEntry{i}", SqlDbType.Int) { Value = absEntries[i] });
        }

        var results = await dbService.QueryAsync(query, sqlParams.ToArray(), reader => new {
            AbsEntry = reader.GetInt32(0),
            IsOpen = reader.GetInt32(1) == 1
        });

        // Create a dictionary with all entries, defaulting to false for missing ones
        var statusDict = new Dictionary<int, bool>();
        foreach (int entry in absEntries) {
            statusDict[entry] = false;
        }

        // Update with actual results from database
        foreach (var result in results) {
            statusDict[result.AbsEntry] = result.IsOpen;
        }

        return statusDict;
    }
}
