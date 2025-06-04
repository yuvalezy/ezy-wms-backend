using System.Data;
using Adapters.Windows.SBO.Services;
using Core.DTOs;
using Core.Enums;
using Core.Models;
using Microsoft.Data.SqlClient;

namespace Adapters.Windows.SBO.Repositories;

public class SboPickingRepository(SboDatabaseService dbService) {
    
    public async Task<IEnumerable<PickingDocument>> GetPickLists(Dictionary<string, object> parameters, string whereClause) {
        var query = $"""

                     SELECT 
                         PICKS."AbsEntry",
                         PICKS."PickDate",
                         COALESCE(PICKS."Remarks", '') AS "Remarks",
                         PICKS."Status",
                         (SELECT COUNT(*) FROM PKL1 WHERE PKL1."AbsEntry" = PICKS."AbsEntry" AND PKL1."OrderType" = 17) AS "SalesOrders",
                         (SELECT COUNT(*) FROM PKL1 WHERE PKL1."AbsEntry" = PICKS."AbsEntry" AND PKL1."OrderType" = 13) AS "Invoices",
                         (SELECT COUNT(*) FROM PKL1 WHERE PKL1."AbsEntry" = PICKS."AbsEntry" AND PKL1."OrderType" = 67) AS "Transfers",
                         COALESCE(SUM(PKL1."RelQtty"), 0) AS "Quantity",
                         COALESCE(SUM(PKL1."RelQtty" - PKL1."PickQtty"), 0) AS "OpenQuantity",
                         COALESCE(SUM(CASE WHEN PKL1."Status" = 'Y' THEN PKL1."PickQtty" ELSE 0 END), 0) AS "UpdateQuantity"
                     FROM OPKL PICKS
                     LEFT JOIN PKL1 ON PKL1."AbsEntry" = PICKS."AbsEntry"
                     WHERE PICKS."WhsCode" = @WhsCode
                     {whereClause}
                     GROUP BY PICKS."AbsEntry", PICKS."PickDate", PICKS."Remarks", PICKS."Status"
                     ORDER BY PICKS."AbsEntry" DESC
                     """;

        var sqlParams = ConvertToSqlParameters(parameters);
        
        return await dbService.QueryAsync(query, sqlParams, reader => new PickingDocument {
            Entry = reader.GetInt32(0),
            Date = reader.GetDateTime(1),
            Remarks = reader.IsDBNull(2) ? null : reader.GetString(2),
            Status = ConvertStatus(reader.IsDBNull(3) ? null : reader.GetString(3)),
            SalesOrders = reader.GetInt32(4),
            Invoices = reader.GetInt32(5),
            Transfers = reader.GetInt32(6),
            Quantity = reader.GetInt32(7),
            OpenQuantity = reader.GetInt32(8),
            UpdateQuantity = reader.GetInt32(9)
        });
    }
    
    public async Task<IEnumerable<PickingDetail>> GetPickingDetails(Dictionary<string, object> parameters) {
        var query = @"
SELECT DISTINCT
    PKL1.""OrderType"" AS ""Type"",
    PKL1.""OrderEntry"" AS ""Entry"",
    T0.""DocNum"",
    T0.""DocDate"",
    T0.""CardCode"",
    T0.""CardName"",
    (SELECT COUNT(*) FROM PKL1 T1 WHERE T1.""AbsEntry"" = PKL1.""AbsEntry"" AND T1.""OrderType"" = PKL1.""OrderType"" AND T1.""OrderEntry"" = PKL1.""OrderEntry"") AS ""TotalItems"",
    (SELECT COUNT(*) FROM PKL1 T1 WHERE T1.""AbsEntry"" = PKL1.""AbsEntry"" AND T1.""OrderType"" = PKL1.""OrderType"" AND T1.""OrderEntry"" = PKL1.""OrderEntry"" AND T1.""PickQtty"" < T1.""RelQtty"") AS ""TotalOpenItems""
FROM PKL1
INNER JOIN ODLN T0 ON T0.""DocEntry"" = PKL1.""OrderEntry"" AND PKL1.""OrderType"" = 15
WHERE PKL1.""AbsEntry"" = @AbsEntry";

        if (parameters.ContainsKey("@Type")) {
            query += " AND PKL1.\"OrderType\" = @Type";
        }
        
        if (parameters.ContainsKey("@Entry")) {
            query += " AND PKL1.\"OrderEntry\" = @Entry";
        }
        
        query += " ORDER BY PKL1.\"OrderType\", PKL1.\"OrderEntry\"";
        
        var sqlParams = ConvertToSqlParameters(parameters);
        
        return await dbService.QueryAsync(query, sqlParams, reader => new PickingDetail {
            Type = reader.GetInt32(0),
            Entry = reader.GetInt32(1),
            Number = reader.GetInt32(2),
            Date = reader.GetDateTime(3),
            CardCode = reader.IsDBNull(4) ? string.Empty : reader.GetString(4),
            CardName = reader.IsDBNull(5) ? string.Empty : reader.GetString(5),
            TotalItems = reader.GetInt32(6),
            TotalOpenItems = reader.GetInt32(7)
        });
    }
    
    public async Task<IEnumerable<PickingDetailItem>> GetPickingDetailItems(Dictionary<string, object> parameters) {
        var query = @"
SELECT 
    PKL1.""ItemCode"",
    OITM.""ItemName"",
    PKL1.""RelQtty"" AS ""Quantity"",
    PKL1.""PickQtty"" AS ""Picked"",
    (PKL1.""RelQtty"" - PKL1.""PickQtty"") AS ""OpenQuantity"",
    COALESCE(OITM.""NumInBuy"", 1) AS ""NumInBuy"",
    COALESCE(OITM.""BuyUnitMsr"", '') AS ""BuyUnitMsr"",
    COALESCE(OITM.""PurPackUn"", 1) AS ""PurPackUn"",
    COALESCE(OITM.""PurPackMsr"", '') AS ""PurPackMsr""
FROM PKL1
INNER JOIN OITM ON OITM.""ItemCode"" = PKL1.""ItemCode""
WHERE PKL1.""AbsEntry"" = @AbsEntry
    AND PKL1.""OrderType"" = @Type
    AND PKL1.""OrderEntry"" = @Entry
ORDER BY PKL1.""PickEntry""";
        
        var sqlParams = ConvertToSqlParameters(parameters);
        
        return await dbService.QueryAsync(query, sqlParams, reader => new PickingDetailItem {
            ItemCode = reader.GetString(0),
            ItemName = reader.IsDBNull(1) ? string.Empty : reader.GetString(1),
            Quantity = reader.GetInt32(2),
            Picked = reader.GetInt32(3),
            OpenQuantity = reader.GetInt32(4),
            NumInBuy = reader.GetInt32(5),
            BuyUnitMsr = reader.IsDBNull(6) ? string.Empty : reader.GetString(6),
            PurPackUn = reader.GetInt32(7),
            PurPackMsr = reader.IsDBNull(8) ? string.Empty : reader.GetString(8)
        });
    }
    
    public async Task<IEnumerable<ItemBinLocationQuantity>> GetPickingDetailItemsBins(Dictionary<string, object> parameters) {
        var query = @"
SELECT 
    OIBQ.""BinAbs"" AS ""BinEntry"",
    OBIN.""BinCode"",
    OIBQ.""ItemCode"",
    OIBQ.""OnHandQty"" AS ""Quantity""
FROM OIBQ
INNER JOIN OBIN ON OBIN.""AbsEntry"" = OIBQ.""BinAbs""
INNER JOIN PKL1 ON PKL1.""ItemCode"" = OIBQ.""ItemCode""
WHERE PKL1.""AbsEntry"" = @AbsEntry
    AND PKL1.""OrderType"" = @Type
    AND PKL1.""OrderEntry"" = @Entry
    AND OIBQ.""OnHandQty"" > 0";
        
        if (parameters.ContainsKey("@BinEntry")) {
            query += " AND OIBQ.\"BinAbs\" = @BinEntry";
        }
        
        query += " ORDER BY OIBQ.\"ItemCode\", OBIN.\"BinCode\"";
        
        var sqlParams = ConvertToSqlParameters(parameters);
        
        return await dbService.QueryAsync(query, sqlParams, reader => new ItemBinLocationQuantity {
            Entry = reader.GetInt32(0),
            Code = reader.GetString(1),
            ItemCode = reader.GetString(2),
            Quantity = reader.GetInt32(3)
        });
    }
    
    public async Task<PickingValidationResult> ValidatePickingAddItem(PickListAddItemRequest request, Guid userId) {
        var query = @"
SELECT 
    PKL1.""PickEntry"",
    CASE 
        WHEN OPKL.""Status"" = 'C' THEN -6  -- Closed document
        WHEN PKL1.""ItemCode"" <> @ItemCode THEN -2  -- Wrong item
        WHEN PKL1.""PickQtty"" >= PKL1.""RelQtty"" THEN -3  -- Already picked
        WHEN @Quantity > (PKL1.""RelQtty"" - PKL1.""PickQtty"") THEN -4  -- Too much quantity
        ELSE 0  -- OK
    END AS ""ValidationResult""
FROM OPKL
INNER JOIN PKL1 ON PKL1.""AbsEntry"" = OPKL.""AbsEntry""
WHERE OPKL.""AbsEntry"" = @ID
    AND PKL1.""OrderType"" = @SourceType
    AND PKL1.""OrderEntry"" = @SourceEntry
    AND PKL1.""ItemCode"" = @ItemCode";
        
        var sqlParams = new[] {
            new SqlParameter("@ID", SqlDbType.Int) { Value = request.ID },
            new SqlParameter("@SourceType", SqlDbType.Int) { Value = request.Type },
            new SqlParameter("@SourceEntry", SqlDbType.Int) { Value = request.Entry },
            new SqlParameter("@ItemCode", SqlDbType.NVarChar, 50) { Value = request.ItemCode },
            new SqlParameter("@Quantity", SqlDbType.Int) { Value = request.Quantity }
        };
        
        var result = await dbService.QuerySingleAsync(query, sqlParams, reader => new {
            PickEntry = reader.IsDBNull(0) ? (int?)null : reader.GetInt32(0),
            ReturnValue = reader.GetInt32(1)
        });
        
        if (result == null) {
            return new PickingValidationResult {
                IsValid = false,
                ReturnValue = -6,
                ErrorMessage = "Pick entry not found"
            };
        }
        
        return new PickingValidationResult {
            PickEntry = result.PickEntry,
            ReturnValue = result.ReturnValue,
            IsValid = result.ReturnValue == 0,
            ErrorMessage = result.ReturnValue switch {
                -2 => "Wrong item code",
                -3 => "Item already fully picked",
                -4 => "Quantity exceeds available amount",
                -6 => "Document is closed",
                _ => null
            }
        };
    }
    
    public async Task AddPickingItem(PickListAddItemRequest request, Guid employeeId, int pickEntry) {
        var query = @"
UPDATE PKL1 
SET ""PickQtty"" = ""PickQtty"" + @Quantity,
    ""Status"" = CASE WHEN ""PickQtty"" + @Quantity >= ""RelQtty"" THEN 'Y' ELSE 'P' END
WHERE ""AbsEntry"" = @AbsEntry 
    AND ""PickEntry"" = @PickEntry";
        
        var sqlParams = new[] {
            new SqlParameter("@AbsEntry", SqlDbType.Int) { Value = request.ID },
            new SqlParameter("@PickEntry", SqlDbType.Int) { Value = pickEntry },
            new SqlParameter("@Quantity", SqlDbType.Int) { Value = request.Quantity }
        };
        
        await dbService.ExecuteAsync(query, sqlParams);
    }
    
    public async Task<ProcessPickListResult> ProcessPickList(int absEntry, string warehouse) {
        try {
            // This would typically call a stored procedure or create documents in SAP B1
            // For now, we'll simulate the process
            var query = @"
UPDATE OPKL 
SET ""Status"" = 'Y',
    ""CloseDate"" = GETDATE()
WHERE ""AbsEntry"" = @AbsEntry
    AND ""WhsCode"" = @WhsCode
    AND ""Status"" = 'N'";
            
            var sqlParams = new[] {
                new SqlParameter("@AbsEntry", SqlDbType.Int) { Value = absEntry },
                new SqlParameter("@WhsCode", SqlDbType.NVarChar, 8) { Value = warehouse }
            };
            
            var rowsAffected = await dbService.ExecuteAsync(query, sqlParams);
            
            return new ProcessPickListResult {
                Success = rowsAffected > 0,
                DocumentNumber = absEntry, // In real implementation, this would be the created document number
                ErrorMessage = rowsAffected == 0 ? "No rows were updated. Document may already be processed." : null
            };
        }
        catch (Exception ex) {
            return new ProcessPickListResult {
                Success = false,
                ErrorMessage = ex.Message
            };
        }
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
        return parameters.Select(p => {
            var param = new SqlParameter(p.Key, p.Value ?? DBNull.Value);
            
            // Set specific types for known parameters
            if (p.Key == "@WhsCode") {
                param.SqlDbType = SqlDbType.NVarChar;
                param.Size = 8;
            }
            else if (p.Key == "@ItemCode") {
                param.SqlDbType = SqlDbType.NVarChar;
                param.Size = 50;
            }
            else if (p.Key.Contains("Entry") || p.Key.Contains("Type") || p.Key == "@ID") {
                param.SqlDbType = SqlDbType.Int;
            }
            else if (p.Key == "@Date") {
                param.SqlDbType = SqlDbType.DateTime;
            }
            
            return param;
        }).ToArray();
    }
}