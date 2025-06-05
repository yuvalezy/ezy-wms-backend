using System.Data;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using Adapters.Windows.SBO.Services;
using Core.DTOs;
using Core.Enums;
using Core.Models;
using Microsoft.Data.SqlClient;

namespace Adapters.Windows.SBO.Repositories;

public class SboPickingRepository(SboDatabaseService dbService) {
    public async Task<IEnumerable<PickingDocument>> GetPickLists(PickListsRequest request, string warehouse) {
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
            var document = new PickingDocument();
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

    public async Task<IEnumerable<PickingDetail>> GetPickingDetails(Dictionary<string, object> parameters) {
        var query = @"
SELECT DISTINCT
    PKL1.""BaseObject"" AS ""Type"",
    PKL1.""OrderEntry"" AS ""Entry"",
    T0.""DocNum"",
    T0.""DocDate"",
    T0.""CardCode"",
    T0.""CardName"",
    (SELECT COUNT(*) FROM PKL1 T1 WHERE T1.""AbsEntry"" = PKL1.""AbsEntry"" AND T1.""BaseObject"" = PKL1.""BaseObject"" AND T1.""OrderEntry"" = PKL1.""OrderEntry"") AS ""TotalItems"",
    (SELECT COUNT(*) FROM PKL1 T1 WHERE T1.""AbsEntry"" = PKL1.""AbsEntry"" AND T1.""BaseObject"" = PKL1.""BaseObject"" AND T1.""OrderEntry"" = PKL1.""OrderEntry"" AND T1.""PickQtty"" < T1.""RelQtty"") AS ""TotalOpenItems""
FROM PKL1
INNER JOIN ODLN T0 ON T0.""DocEntry"" = PKL1.""OrderEntry"" AND PKL1.""BaseObject"" = 15
WHERE PKL1.""AbsEntry"" = @AbsEntry";

        if (parameters.ContainsKey("@Type")) {
            query += " AND PKL1.\"BaseObject\" = @Type";
        }

        if (parameters.ContainsKey("@Entry")) {
            query += " AND PKL1.\"OrderEntry\" = @Entry";
        }

        query += " ORDER BY PKL1.\"BaseObject\", PKL1.\"OrderEntry\"";

        var sqlParams = ConvertToSqlParameters(parameters);

        return await dbService.QueryAsync(query, sqlParams, reader => new PickingDetail {
            Type           = reader.GetInt32(0),
            Entry          = reader.GetInt32(1),
            Number         = reader.GetInt32(2),
            Date           = reader.GetDateTime(3),
            CardCode       = reader.IsDBNull(4) ? string.Empty : reader.GetString(4),
            CardName       = reader.IsDBNull(5) ? string.Empty : reader.GetString(5),
            TotalItems     = reader.GetInt32(6),
            TotalOpenItems = reader.GetInt32(7)
        });
    }

    public async Task<IEnumerable<PickingDetailItem>> GetPickingDetailItems(Dictionary<string, object> parameters) {
        const string query =
            """

            SELECT 
                T2."ItemCode",
                OITM."ItemName",
                PKL1."RelQtty" AS "Quantity",
                PKL1."PickQtty" AS "Picked",
                (PKL1."RelQtty" - PKL1."PickQtty") AS "OpenQuantity",
                COALESCE(OITM."NumInBuy", 1) AS "NumInBuy",
                COALESCE(OITM."BuyUnitMsr", '') AS "BuyUnitMsr",
                COALESCE(OITM."PurPackUn", 1) AS "PurPackUn",
                COALESCE(OITM."PurPackMsr", '') AS "PurPackMsr"
            FROM PKL1
            inner join OILM T2 on T2."TransType" = PKL1."BaseObject" and T2.DocEntry = PKL1."OrderEntry" and T2."DocLineNum" = PKL1."OrderLine"
            INNER JOIN OITM ON OITM."ItemCode" = T2."ItemCode"
            WHERE PKL1."AbsEntry" = @AbsEntry
                AND PKL1."BaseObject" = @Type
                AND PKL1."OrderEntry" = @Entry
            ORDER BY PKL1."PickEntry"
            """;

        var sqlParams = ConvertToSqlParameters(parameters);

        return await dbService.QueryAsync(query, sqlParams, reader => new PickingDetailItem {
            ItemCode     = reader.GetString(0),
            ItemName     = reader.IsDBNull(1) ? string.Empty : reader.GetString(1),
            Quantity     = reader.GetInt32(2),
            Picked       = reader.GetInt32(3),
            OpenQuantity = reader.GetInt32(4),
            NumInBuy     = reader.GetInt32(5),
            BuyUnitMsr   = reader.IsDBNull(6) ? string.Empty : reader.GetString(6),
            PurPackUn    = reader.GetInt32(7),
            PurPackMsr   = reader.IsDBNull(8) ? string.Empty : reader.GetString(8)
        });
    }

    public async Task<IEnumerable<ItemBinLocationQuantity>> GetPickingDetailItemsBins(Dictionary<string, object> parameters) {
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

        query += " ORDER BY OIBQ.\"ItemCode\", OBIN.\"BinCode\"";

        var sqlParams = ConvertToSqlParameters(parameters);

        return await dbService.QueryAsync(query, sqlParams, reader => new ItemBinLocationQuantity {
            Entry    = reader.GetInt32(0),
            Code     = reader.GetString(1),
            ItemCode = reader.GetString(2),
            Quantity = reader.GetInt32(3)
        });
    }

    public async Task<PickingValidationResult> ValidatePickingAddItem(PickListAddItemRequest request, Guid userId) {
        const string query =
            """

            SELECT 
                PKL1."PickEntry",
                CASE 
                    WHEN OPKL."Status" = 'C' THEN -6  -- Closed document
                    WHEN T2."ItemCode" <> @ItemCode THEN -2  -- Wrong item
                    WHEN PKL1."PickQtty" >= PKL1."RelQtty" THEN -3  -- Already picked
                    WHEN @Quantity > (PKL1."RelQtty" - PKL1."PickQtty") THEN -4  -- Too much quantity
                    ELSE 0  -- OK
                END AS "ValidationResult"
            FROM OPKL
            INNER JOIN PKL1 ON PKL1."AbsEntry" = OPKL."AbsEntry"
            inner join OILM T2 on T2."TransType" = PKL1."BaseObject" and T2.DocEntry = PKL1."OrderEntry" and T2."DocLineNum" = PKL1."OrderLine"
            WHERE OPKL."AbsEntry" = @ID
                AND PKL1."BaseObject" = @SourceType
                AND PKL1."OrderEntry" = @SourceEntry
                AND T2."ItemCode" = @ItemCode
            """;

        var sqlParams = new[] {
            new SqlParameter("@ID", SqlDbType.Int) { Value                = request.ID },
            new SqlParameter("@SourceType", SqlDbType.Int) { Value        = request.Type },
            new SqlParameter("@SourceEntry", SqlDbType.Int) { Value       = request.Entry },
            new SqlParameter("@ItemCode", SqlDbType.NVarChar, 50) { Value = request.ItemCode },
            new SqlParameter("@Quantity", SqlDbType.Int) { Value          = request.Quantity }
        };

        var result = await dbService.QuerySingleAsync(query, sqlParams, reader => new {
            PickEntry   = reader.IsDBNull(0) ? (int?)null : reader.GetInt32(0),
            ReturnValue = reader.GetInt32(1)
        });

        if (result == null) {
            return new PickingValidationResult {
                IsValid      = false,
                ReturnValue  = -6,
                ErrorMessage = "Pick entry not found"
            };
        }

        return new PickingValidationResult {
            PickEntry   = result.PickEntry,
            ReturnValue = result.ReturnValue,
            IsValid     = result.ReturnValue == 0,
            ErrorMessage = result.ReturnValue switch {
                -2 => "Wrong item code",
                -3 => "Item already fully picked",
                -4 => "Quantity exceeds available amount",
                -6 => "Document is closed",
                _  => null
            }
        };
    }

    public async Task AddPickingItem(PickListAddItemRequest request, Guid employeeId, int pickEntry) {
        throw new NotImplementedException("Not implemented!");
//         var query = @"
// UPDATE PKL1 
// SET ""PickQtty"" = ""PickQtty"" + @Quantity,
//     ""Status"" = CASE WHEN ""PickQtty"" + @Quantity >= ""RelQtty"" THEN 'Y' ELSE 'P' END
// WHERE ""AbsEntry"" = @AbsEntry 
//     AND ""PickEntry"" = @PickEntry";
//
//         var sqlParams = new[] {
//             new SqlParameter("@AbsEntry", SqlDbType.Int) { Value  = request.ID },
//             new SqlParameter("@PickEntry", SqlDbType.Int) { Value = pickEntry },
//             new SqlParameter("@Quantity", SqlDbType.Int) { Value  = request.Quantity }
//         };
//
//         await dbService.ExecuteAsync(query, sqlParams);
    }

    public async Task<ProcessPickListResult> ProcessPickList(int absEntry, string warehouse) {
        throw new Exception("Not implemented!");
//         try {
//             // This would typically call a stored procedure or create documents in SAP B1
//             // For now, we'll simulate the process
//             var query = @"
// UPDATE OPKL 
// SET ""Status"" = 'Y',
//     ""CloseDate"" = GETDATE()
// WHERE ""AbsEntry"" = @AbsEntry
//     AND ""WhsCode"" = @WhsCode
//     AND ""Status"" = 'N'";
//
//             var sqlParams = new[] {
//                 new SqlParameter("@AbsEntry", SqlDbType.Int) { Value        = absEntry },
//                 new SqlParameter("@WhsCode", SqlDbType.NVarChar, 8) { Value = warehouse }
//             };
//
//             var rowsAffected = await dbService.ExecuteAsync(query, sqlParams);
//
//             return new ProcessPickListResult {
//                 Success        = rowsAffected > 0,
//                 DocumentNumber = absEntry, // In real implementation, this would be the created document number
//                 ErrorMessage   = rowsAffected == 0 ? "No rows were updated. Document may already be processed." : null
//             };
//         }
//         catch (Exception ex) {
//             return new ProcessPickListResult {
//                 Success      = false,
//                 ErrorMessage = ex.Message
//             };
//         }
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