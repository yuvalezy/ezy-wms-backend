using System.Data;
using System.Text;
using Adapters.Common.SBO.Services;
using Adapters.Common.Utils;
using Core.DTOs.Items;
using Core.DTOs.PickList;
using Core.Enums;
using Core.Interfaces;
using Core.Models.Settings;
using Microsoft.Data.SqlClient;

namespace Adapters.Common.SBO.Repositories;

public class SboPickingRepository(SboDatabaseService dbService, ISettings settings) {
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
                Status = ConvertStatus(reader.IsDBNull(3) ? null : reader.GetString(3)),
                SalesOrders = reader.IsDBNull(4) ? null : StringAggregateDistinct(reader.GetString(4)),
                Invoices = reader.IsDBNull(5) ? null : StringAggregateDistinct(reader.GetString(5)),
                Transfers = reader.IsDBNull(6) ? null : StringAggregateDistinct(reader.GetString(6)),
                Quantity = (int)reader.GetDecimal(7),
                OpenQuantity = (int)reader.GetDecimal(8),
                UpdateQuantity = (int)reader.GetDecimal(9),
                PickPackOnly = pickPackOnly is not null && Convert.ToBoolean(reader["PickPackOnly"])
            };

            return document;
        });

        return response;
    }

    private string StringAggregateDistinct(string value) => value.Length == 0 ? value : value.Split(',').Distinct().Aggregate((a, b) => a + ", " + b);

    public async Task<IEnumerable<PickingDetailResponse>> GetPickingDetails(Dictionary<string, object> parameters) {
        var sb = new StringBuilder(
            """
            SELECT
                T0."BaseObject" AS "Type",
                T0."OrderEntry" AS "Entry",
                COALESCE(ORDR."DocNum", OINV."DocNum", OWTQ."DocNum") AS "DocNum",
                COALESCE(ORDR."DocDate", OINV."DocDate", OWTQ."DocDate") AS "DocDate",
                COALESCE(ORDR."CardCode", OINV."CardCode", OWTQ."CardCode") AS "CardCode",
                COALESCE(ORDR."CardName", OINV."CardName", OWTQ."CardName") AS "CardName",
                SUM(T0."RelQtty" + T0."PickQtty") AS "TotalItems",
                SUM(T0."RelQtty") AS "TotalOpenItems",
                T0."PickEntry"
            """);

        if (settings.CustomFields?["PickingDetails"] != null) {
            CustomFieldsHelper.AppendCustomFieldsToQuery(sb, settings.CustomFields["PickingDetails"]);
        }

        sb.Append(
            """
            FROM PKL1 T0
            LEFT JOIN ORDR ON ORDR."DocEntry" = T0."OrderEntry" AND ORDR."ObjType" = T0."BaseObject"
            LEFT JOIN OINV ON OINV."DocEntry" = T0."OrderEntry" AND OINV."ObjType" = T0."BaseObject"
            LEFT JOIN OWTQ ON OWTQ."DocEntry" = T0."OrderEntry" AND OWTQ."ObjType" = T0."BaseObject"
            WHERE T0."AbsEntry" = @AbsEntry
            """);

        if (parameters.ContainsKey("@Type")) {
            sb.Append(" AND T0.\"BaseObject\" = @Type");
        }

        if (parameters.ContainsKey("@Entry")) {
            sb.Append(" AND T0.\"OrderEntry\" = @Entry");
        }

        sb.Append("""
                   GROUP BY 
                      T0."BaseObject", 
                      T0."OrderEntry", 
                      ORDR."DocNum", OINV."DocNum", OWTQ."DocNum", 
                      ORDR."DocDate", OINV."DocDate", OWTQ."DocDate", 
                      ORDR."CardCode", OINV."CardCode", OWTQ."CardCode", 
                      ORDR."CardName", OINV."CardName", OWTQ."CardName",
                      T0."PickEntry"
                  """);
        
        if (settings.CustomFields?["PickingDetails"] != null) {
            CustomFieldsHelper.AppendCustomFieldsToGroupBy(sb, settings.CustomFields["PickingDetails"]);
        }


        sb.Append(" ORDER BY T0.\"BaseObject\", T0.\"OrderEntry\"");

        var sqlParams = ConvertToSqlParameters(parameters);

        return await dbService.QueryAsync(sb.ToString(), sqlParams, reader =>
        {
            var detail = new PickingDetailResponse {
                Type = reader.GetInt32(0),
                Entry = reader.GetInt32(1),
                Number = reader.GetInt32(2),
                Date = reader.GetDateTime(3),
                CardCode = reader.IsDBNull(4) ? string.Empty : reader.GetString(4),
                CardName = reader.IsDBNull(5) ? string.Empty : reader.GetString(5),
                TotalItems = (int)reader.GetDecimal(6),
                TotalOpenItems = (int)reader.GetDecimal(7),
                PickEntry = reader.GetInt32(8),
            };

            if (settings.CustomFields?["PickingDetails"] != null) {
                CustomFieldsHelper.ReadCustomFields(reader, settings.CustomFields["PickingDetails"], detail);
            }

            return detail;
        });
    }

    public async Task<IEnumerable<PickingDetailItemResponse>> GetPickingDetailItems(Dictionary<string, object> parameters) {
        (string query, var customFields) = BuildPickingDetailItemsQuery();
        var sqlParams = ConvertToSqlParameters(parameters);

        return await dbService.QueryAsync(query, sqlParams, reader =>
        {
            var item = new PickingDetailItemResponse {
                Quantity = (int)reader.GetDecimal("Quantity"),
                Picked = (int)reader.GetDecimal("Picked"),
                OpenQuantity = (int)reader.GetDecimal("OpenQuantity")
            };

            ItemResponseHelper.PopulateItemResponse(reader, item);
            CustomFieldsHelper.ReadCustomFields(reader, customFields, item);
            return item;
        });
    }

    private (string query, CustomField[] customFields) BuildPickingDetailItemsQuery() {
        var queryBuilder = new StringBuilder();
        queryBuilder.Append("""
                            SELECT 
                                COALESCE(T3."ItemCode", T4."ItemCode", T5."ItemCode") as "ItemCode",
                                OITM."ItemName" as "ItemName",
                                SUM(PKL1."RelQtty" + PKL1."PickQtty") AS "Quantity",
                                SUM(PKL1."PickQtty") AS "Picked",
                                SUM(PKL1."RelQtty") AS "OpenQuantity",
                                COALESCE(OITM."NumInBuy", 1) AS "NumInBuy",
                                OITM."BuyUnitMsr" as "BuyUnitMsr",
                                COALESCE(OITM."PurPackUn", 1) AS "PurPackUn",
                                OITM."PurPackMsr" as "PurPackMsr"
                            """);

        var customFields = CustomFieldsHelper.GetCustomFields(settings, "Items");
        CustomFieldsHelper.AppendCustomFieldsToQuery(queryBuilder, customFields);

        queryBuilder.Append("""
                            FROM PKL1
                            left outer join RDR1 T3 on T3."DocEntry" = PKL1."OrderEntry" and T3."LineNum" = PKL1."OrderLine" and T3."ObjType" = PKL1."BaseObject"
                            left outer join INV1 T4 on T4."DocEntry" = PKL1."OrderEntry" and T4."LineNum" = PKL1."OrderLine" and T4."ObjType" = PKL1."BaseObject"
                            left outer join WTQ1 T5 on T5."DocEntry" = PKL1."OrderEntry" and T5."LineNum" = PKL1."OrderLine" and T5."ObjType" = PKL1."BaseObject"
                            INNER JOIN OITM ON OITM."ItemCode" = COALESCE(T3."ItemCode", T4."ItemCode", T5."ItemCode")
                            WHERE PKL1."AbsEntry" = @AbsEntry AND PKL1."BaseObject" = @Type AND PKL1."OrderEntry" = @Entry
                            Group by T5."ItemCode", T4."ItemCode", T3."ItemCode", OITM."ItemName", OITM."NumInBuy", OITM."BuyUnitMsr", OITM."PurPackUn", OITM."PurPackMsr"
                            """);

        // Add custom fields to GROUP BY clause
        CustomFieldsHelper.AppendCustomFieldsToGroupBy(queryBuilder, customFields);

        queryBuilder.Append("""
                             ORDER BY 
                                CASE WHEN SUM(PKL1."RelQtty") = 0 THEN 1 ELSE 0 END,
                                "ItemCode"
                            """);

        return (queryBuilder.ToString(), customFields);
    }

    public async Task<IEnumerable<ItemBinLocationResponseQuantity>> GetPickingDetailItemsBins(Dictionary<string, object> parameters) {
        string query =
        """
        select DISTINCT T2."ItemCode",
        T3."BinAbs" "BinEntry",
        T4."BinCode",
        T3."OnHandQty" - COALESCE(T5."BinQty", 0) "OnHandQty"
        from PKL1 T1
        inner join OILM T2 on T2.TransType = T1.BaseObject and T2.DocEntry = T1.OrderEntry and T2.DocLineNum = T1.OrderLine
        inner join OIBQ T3 on T3."ItemCode" = T2."ItemCode" and T3."WhsCode" = T2."LocCode"
        inner join OBIN T4 on T4."AbsEntry" = T3."BinAbs"
        left outer join (
        	select T2."BinAbs", T3."ItemCode", Sum(T2."PickQtty") "BinQty"
        	from PKL1 T0
        	inner join OPKL T1 on T1."AbsEntry" = T0."AbsEntry" and T1."Status" = 'P'
        	inner join PKL2 T2 on T2."AbsEntry" = T0."AbsEntry" and T2."PickEntry" = T0."PickEntry"
            inner join OILM T3 on T3.TransType = T0.BaseObject and T3.DocEntry = T0.OrderEntry and T3.DocLineNum = T0.OrderLine
        	Group By T2."BinAbs", T3."ItemCode"
        ) T5 on T5."BinAbs" = T3."BinAbs" and T5."ItemCode" = T3."ItemCode"
        where T1."AbsEntry" = @AbsEntry
        and T1."BaseObject" = @Type
        and T1."OrderEntry" = @Entry
        and T3."OnHandQty" - COALESCE(T5."BinQty", 0) > 0
        """;

        if (parameters.ContainsKey("@BinEntry")) {
            query += " AND T3.\"BinAbs\" = @BinEntry";
        }

        query += " ORDER BY T2.\"ItemCode\", T4.\"BinCode\"";

        var sqlParams = ConvertToSqlParameters(parameters);

        return await dbService.QueryAsync(query, sqlParams, reader => new ItemBinLocationResponseQuantity {
            ItemCode = reader.GetString(0),
            Entry = reader.GetInt32(1),
            Code = reader.GetString(2),
            Quantity = (int)reader.GetDecimal(3)
        });
    }

    public async Task<IEnumerable<PickingSelectionResponse>> GetPickingSelection(int absEntry) {
        const string query =
        """
        select T2."ItemCode",
               T3."BinAbs",
               Sum(T3."PickQtty") "Quantity",
                T4."NumInBuy",
                T4.PurPackUn,
                T4."CodeBars"
        from PKL1 T1
                 inner join OILM T2 on T2.TransType = T1.BaseObject and T2.DocEntry = T1.OrderEntry and T2.DocLineNum = T1.OrderLine
                 inner join PKL2 T3 on T3.AbsEntry = T1.AbsEntry and T3.PickEntry = T1.PickEntry
                inner join OITM T4 on T4."ItemCode" = T2."ItemCode"
        where T1."AbsEntry" = @AbsEntry
        GROUP BY T2."ItemCode", T3."BinAbs", T4."NumInBuy", T4.PurPackUn, T4."CodeBars"
        """;

        return await dbService.QueryAsync(query, [new SqlParameter("@AbsEntry", SqlDbType.Int) { Value = absEntry }],
            reader => new PickingSelectionResponse {
                ItemCode = (string)reader["ItemCode"],
                CodeBars = (string)reader["CodeBars"],
                BinEntry = (int)reader["BinAbs"],
                Quantity = Convert.ToDecimal(reader["Quantity"]),
                NumInBuy = Convert.ToDecimal(reader["NumInBuy"]),
                PackUn = Convert.ToDecimal(reader["PurPackUn"])
            });
    }

    public async Task<PickingValidationResult[]> ValidatePickingAddItem(PickListAddItemRequest request) {
        string? pickPackOnly = settings.Filters.PickPackOnly?.Query;

        var sb = new StringBuilder(
            """
            SELECT PKL1."PickEntry",
            CASE 
                WHEN OPKL."Status" = 'C' THEN -6  -- Closed document
                WHEN T2."ItemCode" <> @ItemCode THEN -2  -- Wrong item
                WHEN PKL1."RelQtty" = 0 THEN -3  -- Already picked
                WHEN @Quantity > PKL1."RelQtty" THEN -4  -- Too much quantity
                ELSE 0  -- OK
            END AS "ValidationResult",
            PKL1."RelQtty" "OpenQuantity", COALESCE(T3."OnHandQty", 0) - COALESCE(T5."BinQty", 0) "OnHandQty",
            T6."OnHand"
            """);

        if (!string.IsNullOrWhiteSpace(pickPackOnly)) {
            sb.Append($", Case When {pickPackOnly} Then 1 Else 0 End \"PickPackOnly\" ");
        }

        sb.Append("""
                  FROM OPKL
                  INNER JOIN PKL1 ON PKL1."AbsEntry" = OPKL."AbsEntry"
                  inner join OILM T2 on T2."TransType" = PKL1."BaseObject" and T2.DocEntry = PKL1."OrderEntry" and T2."DocLineNum" = PKL1."OrderLine"
                  left outer join OIBQ T3 on T3."BinAbs" = @BinEntry and T3."ItemCode" = @ItemCode
                  left outer join (
                      select T2."BinAbs", T3."ItemCode", Sum(T2."PickQtty") "BinQty"
                      from PKL1 T0
                      inner join OPKL T1 on T1."AbsEntry" = T0."AbsEntry" and T1."Status" = 'P'
                      inner join PKL2 T2 on T2."AbsEntry" = T0."AbsEntry" and T2."PickEntry" = T0."PickEntry"
                      inner join OILM T3 on T3.TransType = T0.BaseObject and T3.DocEntry = T0.OrderEntry and T3.DocLineNum = T0.OrderLine
                      where T2."BinAbs" = @BinEntry
                      Group By T2."BinAbs", T3."ItemCode"
                  ) T5 on T5."BinAbs" = T3."BinAbs" and T5."ItemCode" = T2."ItemCode"
                  """);

        if (!string.IsNullOrWhiteSpace(pickPackOnly)) {
            sb.Append("left outer join OCRD on OCRD.\"CardCode\" = T2.\"BPCardCode\" ");
        }

        sb.Append("""
                  left outer join OITW T6 on T6."ItemCode" = T2."ItemCode" and T6."WhsCode" = T2."LocCode"
                  WHERE OPKL."AbsEntry" = @ID
                  AND PKL1."BaseObject" = @SourceType
                  AND PKL1."OrderEntry" = @SourceEntry
                  AND T2."ItemCode" = @ItemCode
                  order by 2 desc, 3 desc
                  """);

        string query = sb.ToString();

        var sqlParams = new[] {
            new SqlParameter("@ID", SqlDbType.Int) { Value = request.ID },
            new SqlParameter("@SourceType", SqlDbType.Int) { Value = request.Type },
            new SqlParameter("@SourceEntry", SqlDbType.Int) { Value = request.Entry },
            new SqlParameter("@ItemCode", SqlDbType.NVarChar, 50) { Value = request.ItemCode },
            new SqlParameter("@Quantity", SqlDbType.Int) { Value = request.Quantity },
            new SqlParameter("@BinEntry", SqlDbType.Int) { Value = request.BinEntry ?? -1 },
        };

        var result = await dbService.QueryAsync(query, sqlParams, reader =>
        {
            int returnValue = reader.GetInt32(1);
            if (!string.IsNullOrWhiteSpace(pickPackOnly) && Convert.ToBoolean(reader["PickPackOnly"]) && request.Unit != UnitType.Pack) {
                returnValue = -5;
            }

            return new PickingValidationResult {
                PickEntry = reader.IsDBNull(0) ? null : reader.GetInt32(0),
                ReturnValue = returnValue,
                OpenQuantity = (int)reader.GetDecimal(2),
                BinOnHand = (int)reader.GetDecimal(3),
                OnHand = (int)reader.GetDecimal(4),
                ErrorMessage = returnValue switch {
                    -2 => "Wrong item code",
                    -3 => "Item already fully picked",
                    -4 => "Quantity exceeds open quantity",
                    -5 => "Customer is marked as pick pack only",
                    -6 => "Document is closed",
                    _ => null
                },
                IsValid = returnValue == 0,
            };
        });

        return result.ToArray();
    }

    public async Task<bool> ValidatePickingAddPackage(int absEntry, IEnumerable<PickListValidateAddPackageRequest> values) {
        const string query =
        """
        select T0."RelQtty", T2."LocCode", T2."ItemCode"
        from PKL1 T0
                 INNER JOIN OILM T2 ON T2."TransType" = T0."BaseObject" AND T2."DocEntry" = T0."OrderEntry" AND T2."DocLineNum" = T0."OrderLine"
        where T0."AbsEntry" = @AbsEntry
        """;

        var sqlParams = new[] {
            new SqlParameter("@AbsEntry", SqlDbType.Int) { Value = absEntry }
        };

        var result = await dbService.QueryAsync(query, sqlParams, reader => new {
            Quantity = Convert.ToDecimal(reader[0]),
            WhsCode = reader.GetString(1),
            ItemCode = reader.GetString(2)
        });

        foreach (var value in values) {
            var check = result.FirstOrDefault(x => x.ItemCode == value.ItemCode && x.WhsCode == value.WhsCode);
            if (check == null) {
                return false;
            }

            if (check.Quantity < value.Quantity) {
                return false;
            }
        }

        return true;
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

    private static ObjectStatus ConvertStatus(string? status) {
        return status?.ToUpper() switch {
            "Y" => ObjectStatus.Closed,
            "N" => ObjectStatus.Open,
            "C" => ObjectStatus.Cancelled,
            "P" => ObjectStatus.InProgress,
            _ => ObjectStatus.Open
        };
    }

    private static SqlParameter[] ConvertToSqlParameters(Dictionary<string, object> parameters) {
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

    public async Task<PickListClosureInfo> GetPickListClosureInfo(int absEntry) {
        // Query to get pick list closure information and any follow-up documents
        // This joins PKL1 (pick list lines) with:
        // - DLN1: Delivery note lines (ObjType 15)
        // - PCH1: Purchase invoice lines for returns (ObjType 18) 
        // - WTR1: Inventory transfer lines (ObjType 67)
        // The joins use PickIdNo field which links delivery/return/transfer lines back to the pick list
        // OILM and OBTL are used to get bin location information from inventory transactions
        const string query =
        """
        select T0.PickEntry, T0.PickStatus, 
        COALESCE(T1.DocEntry, T2.DocEntry, T3.DocEntry) DocEntry, 
        COALESCE(T1.ObjType, T2.ObjType, T3.ObjType) ObjType, 
        COALESCE(T1.LineNum, T2.LineNum, T3.ObjType) LineNum,
        COALESCE(T1.DocDate, T2.DocDate, T3.DocDate) DocDate,  
        COALESCE(T1.ItemCode, T2.ItemCode, T3.ItemCode) ItemCode, 
        COALESCE(T1.InvQty, T2.InvQty, T3.InvQty) InvQty, 
        T11."BinAbs", T11."Quantity" "BinQty"
        from PKL1 T0
        left outer join DLN1 T1 on T1.BaseEntry = T0.OrderEntry and T1.BaseType = T0.BaseObject and T1.BaseLine = T0.OrderLine and T1.PickIdNo = T0.AbsEntry
        left outer join PCH1 T2 on T2.BaseEntry = T0.OrderEntry and T2.BaseType = T0.BaseObject and T2.BaseLine = T0.OrderLine and T2.PickIdNo = T0.AbsEntry
        left outer join WTR1 T3 on T3.BaseEntry = T0.OrderEntry and T3.BaseType = T0.BaseObject and T3.BaseLine = T0.OrderLine and T3.PickIdNo = T0.AbsEntry
        left outer join OILM T10 on T10.DocEntry = COALESCE(T1.DocEntry, T2.DocEntry, T3.DocEntry) and T10.TransType = COALESCE(T1.ObjType, T2.ObjType, T3.ObjType) and T10.DocLineNum = COALESCE(T1.LineNum, T2.LineNum, T3.ObjType) and T10.AccumType = 1 and T10.ActionType = 2
        left outer join OBTL T11 on T11.MessageID = T10.MessageID
        where T0."AbsEntry" = @AbsEntry
        """;

        using var dt = await dbService.GetDataTableAsync(query, [new SqlParameter("@AbsEntry", SqlDbType.Int) { Value = absEntry }]);
        if (dt.Rows.Count == 0) {
            throw new Exception("Pick list data not found");
        }

        // Check if pick list is closed (Status = 'C')
        bool isClosed = (string)dt.Rows[0]["PickStatus"] == "C";
        var info = new PickListClosureInfo {
            IsClosed = isClosed,
        };

        // Determine closure reason based on whether follow-up documents exist
        if (isClosed) {
            info.ClosureReason = dt.Rows[0]["DocEntry"] != DBNull.Value ? PickListClosureReasonType.FollowUpDocument : PickListClosureReasonType.Closed;
        }

        // Dictionary to group follow-up documents by PickEntry
        var control = new Dictionary<int, FollowUpDocumentInfo>();

        foreach (DataRow dr in dt.Rows) {
            // Skip rows without follow-up documents
            if (dr["DocEntry"] == DBNull.Value) continue;

            int pickEntry = (int)dr["PickEntry"];
            int docEntry = (int)dr["DocEntry"];
            int objType = Convert.ToInt32(dr["ObjType"]);
            int lineNum = (int)dr["LineNum"];
            var docDate = (DateTime)dr["DocDate"];
            string itemCode = (string)dr["ItemCode"];
            int invQty = Convert.ToInt32(dr["InvQty"]);
            int? binEntry = null;
            int? binQty = null;

            // Get bin information from OBTL if available
            if (dr["BinAbs"] != DBNull.Value) {
                binEntry = (int)dr["BinAbs"];
                binQty = Convert.ToInt32(dr["BinQty"]);
            }

            // Group items by PickEntry and document
            if (!control.TryGetValue(pickEntry, out var documentInfo)) {
                documentInfo = new FollowUpDocumentInfo {
                    PickEntry = pickEntry,
                    DocumentType = objType, // 15=Delivery, 18=Return, 67=Transfer
                    DocumentEntry = docEntry,
                    DocumentNumber = lineNum, // Using LineNum as DocumentNumber (should be DocNum)
                    DocumentDate = docDate,
                };

                control.Add(pickEntry, documentInfo);
                info.FollowUpDocuments.Add(documentInfo);
            }

            // Add item details to the follow-up document
            documentInfo.Items.Add(new FollowUpDocumentItem {
                ItemCode = itemCode,
                Quantity = binQty ?? invQty, // Use bin quantity if available, otherwise invoice quantity
                BinEntry = binEntry
            });
        }

        return info;
    }
}