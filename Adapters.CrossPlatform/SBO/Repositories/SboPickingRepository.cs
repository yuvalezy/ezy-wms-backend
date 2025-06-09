using System.Data;
using System.Text;
using Adapters.CrossPlatform.SBO.Helpers;
using Adapters.CrossPlatform.SBO.Services;
using Core.DTOs.Items;
using Core.DTOs.PickList;
using Core.Entities;
using Core.Enums;
using Core.Interfaces;
using Microsoft.Data.SqlClient;

namespace Adapters.CrossPlatform.SBO.Repositories;

public class SboPickingRepository(SboDatabaseService dbService, SboCompany sboCompany, ISettings settings) {
    public async Task<IEnumerable<PickingDocumentResponse>> GetPickLists(PickListsRequest request, string warehouse) {
        var sb = new StringBuilder(
            """

            SELECT 
                PICKS."AbsEntry",
                PICKS."PickDate",
                COALESCE(Cast(PICKS."Remarks" as varchar(8000)), '') AS "Remarks",
                PICKS."Status" "Status",
                (SELECT COUNT(*) FROM PKL1 WHERE PKL1."AbsEntry" = PICKS."AbsEntry" AND PKL1."BaseObject" = 17) AS "SalesOrders",
                (SELECT COUNT(*) FROM PKL1 WHERE PKL1."AbsEntry" = PICKS."AbsEntry" AND PKL1."BaseObject" = 13) AS "Invoices",
                (SELECT COUNT(*) FROM PKL1 WHERE PKL1."AbsEntry" = PICKS."AbsEntry" AND PKL1."BaseObject" = 67) AS "Transfers",
                COALESCE(SUM(PKL1."RelQtty" + PKL1."PickQtty"), 0) AS "Quantity",
                COALESCE(SUM(PKL1."RelQtty"), 0) AS "OpenQuantity",
                COALESCE(SUM(CASE WHEN PKL1."PickStatus" = 'Y' THEN PKL1."PickQtty" ELSE 0 END), 0) AS "UpdateQuantity"
            FROM OPKL PICKS
            LEFT JOIN PKL1 ON PKL1."AbsEntry" = PICKS."AbsEntry"
            inner join OILM T2 on T2."TransType" = PKL1."BaseObject" and T2.DocEntry = PKL1."OrderEntry" and T2."DocLineNum" = PKL1."OrderLine"
            WHERE T2."LocCode" = @WhsCode 
            AND PICKS."Status" IN ('R', 'P', 'D')
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
            var statusPlaceholders = string.Join(", ", request.Statuses.Select((_, i) => $"@Status{i}"));
            sb.AppendLine($" AND PICKS.\"Status\" IN ({statusPlaceholders})");

            for (int i = 0; i < request.Statuses.Length; i++) {
                parameters.Add(new SqlParameter($"@Status{i}", SqlDbType.Char, 1) { Value = (char)request.Statuses[i] });
            }
        }

        sb.AppendLine("""
                       GROUP BY PICKS."AbsEntry", PICKS."PickDate", Cast(PICKS."Remarks" as varchar(8000)), PICKS."Status"
                      ORDER BY PICKS."AbsEntry" DESC
                      """);

        var sqlParams = parameters.ToArray();

        return await dbService.QueryAsync(sb.ToString(), sqlParams, reader => {
            var document = new PickingDocumentResponse();
            document.Entry          = reader.GetInt32(0);
            document.Date           = reader.GetDateTime(1);
            document.Remarks        = reader.IsDBNull(2) ? null : reader.GetString(2);
            document.Status         = ConvertStatus(reader.IsDBNull(3) ? null : reader.GetString(3));
            document.SalesOrders    = reader.GetInt32(4);
            document.Invoices       = reader.GetInt32(5);
            document.Transfers      = reader.GetInt32(6);
            document.Quantity       = (int)reader.GetDecimal(7);
            document.OpenQuantity   = (int)reader.GetDecimal(8);
            document.UpdateQuantity = (int)reader.GetDecimal(9);
            return document;
        });
    }

    public async Task<IEnumerable<PickingDetailResponse>> GetPickingDetails(Dictionary<string, object> parameters) {
        string query =
            """
            SELECT
                T0."BaseObject" AS "Type",
                T0."OrderEntry" AS "Entry",
                COALESCE(T2."DocNum", T3."DocNum", T4."DocNum") AS "DocNum",
                COALESCE(T2."DocDate", T3."DocDate", T4."DocDate") AS "DocDate",
                COALESCE(T2."CardCode", T3."CardCode", T4."CardCode") AS "CardCode",
                COALESCE(T2."CardName", T3."CardName", T4."CardName") AS "CardName",
                SUM(T0."RelQtty" + T0."PickQtty") AS "TotalItems",
                SUM(T0."RelQtty") AS "TotalOpenItems",
                T0."PickEntry"
            FROM PKL1 T0
            LEFT JOIN ORDR T2 
                ON T2."DocEntry" = T0."OrderEntry" AND T2."ObjType" = T0."BaseObject"
            LEFT JOIN OINV T3 
                ON T3."DocEntry" = T0."OrderEntry" AND T3."ObjType" = T0."BaseObject"
            LEFT JOIN OWTQ T4 
                ON T4."DocEntry" = T0."OrderEntry" AND T4."ObjType" = T0."BaseObject"
            WHERE 
                T0."AbsEntry" = @AbsEntry

            """;

        if (parameters.ContainsKey("@Type")) {
            query += " AND T0.\"BaseObject\" = @Type";
        }

        if (parameters.ContainsKey("@Entry")) {
            query += " AND T0.\"OrderEntry\" = @Entry";
        }

        query += """
                  GROUP BY 
                     T0."BaseObject", 
                     T0."OrderEntry", 
                     T2."DocNum", T3."DocNum", T4."DocNum", 
                     T2."DocDate", T3."DocDate", T4."DocDate", 
                     T2."CardCode", T3."CardCode", T4."CardCode", 
                     T2."CardName", T3."CardName", T4."CardName",
                     T0."PickEntry"
                 """;

        query += " ORDER BY T0.\"BaseObject\", T0.\"OrderEntry\"";

        var sqlParams = ConvertToSqlParameters(parameters);

        return await dbService.QueryAsync(query, sqlParams, reader => {
            var detail = new PickingDetailResponse {
                Type           = reader.GetInt32(0),
                Entry          = reader.GetInt32(1),
                Number         = reader.GetInt32(2),
                Date           = reader.GetDateTime(3),
                CardCode       = reader.IsDBNull(4) ? string.Empty : reader.GetString(4),
                CardName       = reader.IsDBNull(5) ? string.Empty : reader.GetString(5),
                TotalItems     = (int)reader.GetDecimal(6),
                TotalOpenItems = (int)reader.GetDecimal(7),
                PickEntry      = reader.GetInt32(8),
            };
            return detail;
        });
    }

    public async Task<IEnumerable<PickingDetailItemResponse>> GetPickingDetailItems(Dictionary<string, object> parameters) {
        const string query =
            """
            SELECT 
                T2."ItemCode",
                OITM."ItemName",
                SUM(PKL1."RelQtty" + PKL1."PickQtty") AS "Quantity",
                SUM(PKL1."PickQtty") AS "Picked",
                SUM(PKL1."RelQtty") AS "OpenQuantity",
                COALESCE(OITM."NumInBuy", 1) AS "NumInBuy",
                COALESCE(OITM."BuyUnitMsr", '') AS "BuyUnitMsr",
                COALESCE(OITM."PurPackUn", 1) AS "PurPackUn",
                COALESCE(OITM."PurPackMsr", '') AS "PurPackMsr"
            FROM PKL1
            INNER JOIN OILM T2 
                ON T2."TransType" = PKL1."BaseObject" 
                AND T2."DocEntry" = PKL1."OrderEntry" 
                AND T2."DocLineNum" = PKL1."OrderLine"
            INNER JOIN OITM 
                ON OITM."ItemCode" = T2."ItemCode"
            WHERE 
                PKL1."AbsEntry" = @AbsEntry
                AND PKL1."BaseObject" = @Type
                AND PKL1."OrderEntry" = @Entry
            Group by 
                T2."ItemCode",
                OITM."ItemName",
            	OITM."NumInBuy",
            	OITM."BuyUnitMsr",
            	OITM."PurPackUn",
            	OITM."PurPackMsr"
            ORDER BY 
                CASE WHEN SUM(PKL1."RelQtty") = 0 THEN 1 ELSE 0 END,
                T2."ItemCode";
            """;

        var sqlParams = ConvertToSqlParameters(parameters);

        return await dbService.QueryAsync(query, sqlParams, reader => {
            var item = new PickingDetailItemResponse {
                ItemCode     = reader.GetString(0),
                ItemName     = reader.IsDBNull(1) ? string.Empty : reader.GetString(1),
                Quantity     = (int)reader.GetDecimal(2),
                Picked       = (int)reader.GetDecimal(3),
                OpenQuantity = (int)reader.GetDecimal(4),
                NumInBuy     = (int)reader.GetDecimal(5),
                BuyUnitMsr   = reader.IsDBNull(6) ? string.Empty : reader.GetString(6),
                PurPackUn    = (int)reader.GetDecimal(7),
                PurPackMsr   = reader.IsDBNull(8) ? string.Empty : reader.GetString(8)
            };
            return item;
        });
    }

    public async Task<IEnumerable<ItemBinLocationResponseQuantity>> GetPickingDetailItemsBins(Dictionary<string, object> parameters) {
        string query =
            """
            select DISTINCT T2."ItemCode",
            T3."BinAbs" "BinEntry",
            T4."BinCode",
            T3."OnHandQty"
            from PKL1 T1
            inner join OILM T2 on T2.TransType = T1.BaseObject and T2.DocEntry = T1.OrderEntry and T2.DocLineNum = T1.OrderLine
            inner join OIBQ T3 on T3."ItemCode" = T2."ItemCode" and T3."WhsCode" = T2."LocCode"
            inner join OBIN T4 on T4."AbsEntry" = T3."BinAbs"
            where T1."AbsEntry" = @AbsEntry
            and T1."BaseObject" = @Type
            and T1."OrderEntry" = @Entry
            and T3."OnHandQty" > 0
            """;

        if (parameters.ContainsKey("@BinEntry")) {
            query += " AND T3.\"BinAbs\" = @BinEntry";
        }

        query += " ORDER BY T2.\"ItemCode\", T4.\"BinCode\"";

        var sqlParams = ConvertToSqlParameters(parameters);

        return await dbService.QueryAsync(query, sqlParams, reader => new ItemBinLocationResponseQuantity {
            ItemCode = reader.GetString(0),
            Entry    = reader.GetInt32(1),
            Code     = reader.GetString(2),
            Quantity = (int)reader.GetDecimal(3)
        });
    }

    public async Task<PickingValidationResult[]> ValidatePickingAddItem(PickListAddItemRequest request, Guid userId) {
        const string query =
            """
            SELECT PKL1."PickEntry",
            CASE 
                WHEN OPKL."Status" = 'C' THEN -6  -- Closed document
                WHEN T2."ItemCode" <> @ItemCode THEN -2  -- Wrong item
                WHEN PKL1."PickQtty" >= PKL1."RelQtty" THEN -3  -- Already picked
                WHEN @Quantity > (PKL1."RelQtty" - PKL1."PickQtty") THEN -4  -- Too much quantity
                ELSE 0  -- OK
            END AS "ValidationResult",
            PKL1."RelQtty" - PKL1."PickQtty" "OpenQuantity", COALESCE(T3."OnHandQty", 0) "OnHandQty"
            FROM OPKL
            INNER JOIN PKL1 ON PKL1."AbsEntry" = OPKL."AbsEntry"
            inner join OILM T2 on T2."TransType" = PKL1."BaseObject" and T2.DocEntry = PKL1."OrderEntry" and T2."DocLineNum" = PKL1."OrderLine"
            left outer join OIBQ T3 on T3."BinAbs" = @BinEntry and T3."ItemCode" = @ItemCode
            WHERE OPKL."AbsEntry" = @ID
            AND PKL1."BaseObject" = @SourceType
            AND PKL1."OrderEntry" = @SourceEntry
            AND T2."ItemCode" = @ItemCode
            order by 2 desc, 3 desc
            """;

        var sqlParams = new[] {
            new SqlParameter("@ID", SqlDbType.Int) { Value                = request.ID },
            new SqlParameter("@SourceType", SqlDbType.Int) { Value        = request.Type },
            new SqlParameter("@SourceEntry", SqlDbType.Int) { Value       = request.Entry },
            new SqlParameter("@ItemCode", SqlDbType.NVarChar, 50) { Value = request.ItemCode },
            new SqlParameter("@Quantity", SqlDbType.Int) { Value          = request.Quantity },
            new SqlParameter("@BinEntry", SqlDbType.Int) { Value          = request.BinEntry },
        };

        var result = await dbService.QueryAsync(query, sqlParams, reader => {
            int returnValue = reader.GetInt32(1);
            return new PickingValidationResult {
                PickEntry    = reader.IsDBNull(0) ? null : reader.GetInt32(0),
                ReturnValue  = returnValue,
                OpenQuantity = (int)reader.GetDecimal(2),
                BinOnHand    = (int)reader.GetDecimal(3),
                ErrorMessage = returnValue switch {
                    -2 => "Wrong item code",
                    -3 => "Item already fully picked",
                    -4 => "Quantity exceeds open quantity",
                    -6 => "Document is closed",
                    _  => null
                },
                IsValid = returnValue == 0,
            };
        });

        return result.ToArray();
    }

    public Task<ProcessPickListResult> ProcessPickList(int absEntry, string warehouse, List<PickList> data) {
        using var update = new PickingUpdate(absEntry, data, dbService, sboCompany, settings.Filters.PickReady);
        var result = new ProcessPickListResult {
            Success        = true,
            DocumentNumber = absEntry,
        };
        try {
            update.Execute();
        }
        catch (Exception e) {
            result.ErrorMessage = e.Message;
            result.Success      = false;
        }

        return Task.FromResult(result);
    }

    public async Task<Dictionary<int, bool>> GetPickListStatuses(int[] absEntries) {
        if (absEntries == null || absEntries.Length == 0) {
            return new Dictionary<int, bool>();
        }

        var placeholders = string.Join(", ", absEntries.Select((_, i) => $"@AbsEntry{i}"));
        var query = $"""
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
            IsOpen   = reader.GetInt32(1) == 1
        });

        // Create a dictionary with all entries, defaulting to false for missing ones
        var statusDict = new Dictionary<int, bool>();
        foreach (var entry in absEntries) {
            statusDict[entry] = false;
        }

        // Update with actual results from database
        foreach (var result in results) {
            statusDict[result.AbsEntry] = result.IsOpen;
        }

        return statusDict;
    }

    private static ObjectStatus ConvertStatus(string? status) {
        return status?.ToUpper() switch {
            "Y" => ObjectStatus.Closed,
            "N" => ObjectStatus.Open,
            "C" => ObjectStatus.Cancelled,
            "P" => ObjectStatus.InProgress,
            _   => ObjectStatus.Open
        };
    }

    private static SqlParameter[] ConvertToSqlParameters(Dictionary<string, object> parameters) {
        return parameters.Select(p => {
            var param = new SqlParameter(p.Key, p.Value ?? DBNull.Value);

            switch (p.Key) {
                // Set specific types for known parameters
                case "@WhsCode":
                    param.SqlDbType = SqlDbType.NVarChar;
                    param.Size      = 8;
                    break;
                case "@ItemCode":
                    param.SqlDbType = SqlDbType.NVarChar;
                    param.Size      = 50;
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
}