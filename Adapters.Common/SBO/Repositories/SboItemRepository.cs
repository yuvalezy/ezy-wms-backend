using System.Data;
using System.Text;
using Adapters.Common.SBO.Services;
using Core.DTOs.Items;
using Core.Interfaces;
using Core.Models.Settings;
using Microsoft.Data.SqlClient;

namespace Adapters.Common.SBO.Repositories;

public class SboItemRepository(SboDatabaseService dbService, ISettings settings) {
    public async Task<IEnumerable<ItemInfoResponse>> ScanItemBarCodeAsync(string scanCode, bool item = false) {
        string query = !item
            ? """
              SELECT T0."ItemCode", T1."ItemName", T2."Father"
              FROM OBCD T0
                       INNER JOIN OITM T1 ON T0."ItemCode" = T1."ItemCode"
              left outer join ITT1 T2 on T2."Code" = T0."ItemCode"
              WHERE T0."BcdCode" = @ScanCode
              """
            : """
              SELECT T0."ItemCode", T1."ItemName", T2."Father"
              FROM OITM T1
                       left outer JOIN OBCD T0 ON T0."ItemCode" = T1."ItemCode"
              left outer join ITT1 T2 on T2."Code" = T0."ItemCode"
              WHERE T1."ItemCode" = @ScanCode or T0."BcdCode" = @ScanCode
              """;

        var parameters = new[] {
            new SqlParameter("@ScanCode", SqlDbType.NVarChar, 50) { Value = scanCode }
        };

        return await dbService.QueryAsync(query, parameters, reader => {
            var item = new ItemInfoResponse(reader.GetString(0));
            if (!reader.IsDBNull(1))
                item.Name = reader.GetString(1);
            if (!reader.IsDBNull(2))
                item.Father = reader.GetString(2);
            return item;
        });
    }

    public async Task<IEnumerable<ItemCheckResponse>> ItemCheckAsync(string? itemCode, string? barcode) {
        var response = new List<ItemCheckResponse>();
        if (string.IsNullOrWhiteSpace(itemCode) && string.IsNullOrWhiteSpace(barcode))
            return response;

        var data = new List<ItemCheckResponse>();
        if (!string.IsNullOrWhiteSpace(itemCode)) {
            data.AddRange(await QueryItemsByItemCode(itemCode));
        }
        else if (!string.IsNullOrWhiteSpace(barcode)) {
            data.AddRange(await QueryItemsByBarcode(barcode));
        }
        else {
            throw new ArgumentException("Either itemCode or barcode must be provided.");
        }

        await FetchBarcodesForItems(data, response);
        return response;
    }

    private async Task<IEnumerable<ItemCheckResponse>> QueryItemsByItemCode(string itemCode) {
        var (query, customFields) = BuildItemQuery(""" from OITM where "ItemCode" = @ItemCode""");

        var parameters = new[] {
            new SqlParameter("@ItemCode", SqlDbType.NVarChar, 50) { Value = itemCode }
        };

        return await dbService.QueryAsync(query, parameters, reader => MapItemCheckResponse(reader, customFields));
    }

    private async Task<IEnumerable<ItemCheckResponse>> QueryItemsByBarcode(string barcode) {
        var (query, customFields) = BuildItemQueryWithBarcode();

        var parameters = new[] {
            new SqlParameter("@ScanCode", SqlDbType.NVarChar, 255) { Value = barcode }
        };

        return await dbService.QueryAsync(query, parameters, reader => MapItemCheckResponse(reader, customFields));
    }

    private (string query, List<CustomField> customFields) BuildItemQuery(string fromClause) {
        var queryBuilder = new StringBuilder();
        queryBuilder.Append("""select "ItemCode", "ItemName", "BuyUnitMsr" , COALESCE("NumInBuy", 1)  "NumInBuy", "PurPackMsr" , COALESCE("PurPackUn", 1) "PurPackUn" """);

        var customFields = GetCustomFields();
        AppendCustomFieldsToQuery(queryBuilder, customFields);

        queryBuilder.Append(fromClause);

        return (queryBuilder.ToString(), customFields);
    }

    private (string query, List<CustomField> customFields) BuildItemQueryWithBarcode() {
        var queryBuilder = new StringBuilder();
        queryBuilder.Append("""
                            select OBCD."ItemCode"
                                 , OITM."ItemName"
                                 , OITM."BuyUnitMsr"
                                 , COALESCE(OITM."NumInBuy", 1)  "NumInBuy"
                                 , OITM."PurPackMsr"
                                 , COALESCE(OITM."PurPackUn", 1) "PurPackUn"
                            """);

        var customFields = GetCustomFields();
        AppendCustomFieldsToQuery(queryBuilder, customFields);

        queryBuilder.Append("""
                            from OBCD 
                                     inner join OITM on OITM."ItemCode" = OBCD."ItemCode"
                            where OBCD."BcdCode" = @ScanCode
                            """);

        return (queryBuilder.ToString(), customFields);
    }

    private List<CustomField> GetCustomFields() => settings.CustomFields?.TryGetValue("Items", out var itemCustomFields) == true ? itemCustomFields.ToList() : [];

    private void AppendCustomFieldsToQuery(StringBuilder queryBuilder, List<CustomField> customFields) {
        foreach (var field in customFields) {
            queryBuilder.Append($", ({field.Query}) as \"{field.Key}\"");
        }
    }

    private ItemCheckResponse MapItemCheckResponse(IDataReader reader, List<CustomField> customFields) {
        var item = new ItemCheckResponse {
            ItemCode   = reader.GetString(0),
            ItemName   = !reader.IsDBNull(1) ? reader.GetString(1) : "",
            BuyUnitMsr = !reader.IsDBNull(2) ? reader.GetString(2) : "",
            NumInBuy   = (int)reader.GetDecimal(3),
            PurPackMsr = !reader.IsDBNull(4) ? reader.GetString(4) : "",
            PurPackUn  = (int)reader.GetDecimal(5)
        };

        ReadCustomFields(reader, customFields, item);

        return item;
    }

    private void ReadCustomFields(IDataReader reader, List<CustomField> customFields, ItemCheckResponse item) {
        int fieldIndex = 6;
        foreach (var field in customFields) {
            if (!reader.IsDBNull(fieldIndex)) {
                object value = field.Type switch {
                    CustomFieldType.Text   => reader.GetString(fieldIndex),
                    CustomFieldType.Number => reader.GetValue(fieldIndex),
                    CustomFieldType.Date   => reader.GetDateTime(fieldIndex),
                    _                      => reader.GetValue(fieldIndex)
                };
                item.CustomFields[field.Key] = value;
            }

            fieldIndex++;
        }
    }

    private async Task FetchBarcodesForItems(List<ItemCheckResponse> data, List<ItemCheckResponse> response) {
        const string barcodeQuery = """select "BcdCode" from OBCD where "ItemCode" = @ItemCode""";

        foreach (var item in data) {
            var barcodeParameters = new[] {
                new SqlParameter("@ItemCode", SqlDbType.NVarChar, 50) { Value = item.ItemCode }
            };
            var barcodes = await dbService.QueryAsync(barcodeQuery, barcodeParameters, reader => reader.GetString(0));
            item.Barcodes.AddRange(barcodes);
            response.Add(item);
        }
    }

    public async Task<IEnumerable<ItemBinStockResponse>> ItemBinStockAsync(string itemCode, string whsCode) {
        const string query = """
                             select T1."BinCode", T0."OnHandQty", T1."AbsEntry"
                             from OIBQ T0
                                      inner join OBIN T1 on T1."AbsEntry" = T0."BinAbs"
                             where T0."ItemCode" = @ItemCode
                               and T0."WhsCode" = @WhsCode
                               and T0."OnHandQty" > 0
                             order by 1
                             """;
        var parameters = new[] {
            new SqlParameter("@ItemCode", SqlDbType.NVarChar, 50) { Value = itemCode },
            new SqlParameter("@WhsCode", SqlDbType.NVarChar, 8) { Value   = whsCode }
        };

        return await dbService.QueryAsync(query, parameters, reader => new ItemBinStockResponse {
            BinCode  = reader.GetString(0),
            Quantity = Convert.ToInt32(reader[1]),
            BinEntry = reader.GetInt32(2),
        });
    }

    public async Task<Dictionary<string, ItemWarehouseStockResponse>> ItemsWarehouseStockAsync(string warehouse, string[] items) {
        if (items.Length == 0) {
            return new();
        }

        var parameters = new List<SqlParameter> { new("@WhsCode", SqlDbType.NVarChar, 8) { Value = warehouse } };
        var sb = new StringBuilder("""
                                   select T0."ItemCode", T0."ItemName",
                                          COALESCE(T1."OnHand", 0)        "OnHand",
                                          COALESCE(T0."NumInBuy", 1)      "NumInBuy",
                                          T0."BuyUnitMsr",
                                          COALESCE(T0."PurPackUn", 1)     "PurPackUn",
                                          T0."PurPackMsr"
                                   from OITM T0
                                   inner join OITW T1 on T1."ItemCode" = T0."ItemCode" and T1."whsCode" = @WhsCode
                                   where T0."ItemCode" in (
                                   """);
        for (int i = 0; i < items.Length; i++) {
            if (i > 0) {
                sb.Append(", ");
            }

            sb.Append($"@ItemCode{i}");
            parameters.Add(new($"@ItemCode{i}", SqlDbType.NVarChar, 50) { Value = items[i] });
        }

        sb.Append(")");
        var response = new Dictionary<string, ItemWarehouseStockResponse>();
        await dbService.QueryAsync(sb.ToString(), parameters.ToArray(),
            reader => {
                var value = new ItemWarehouseStockResponse {
                    ItemCode   = reader.GetString(0),
                    ItemName   = !reader.IsDBNull(1) ? reader.GetString(1) : "",
                    Stock      = (int)reader.GetDecimal(2),
                    NumInBuy   = (int)reader.GetDecimal(3),
                    BuyUnitMsr = !reader.IsDBNull(4) ? reader.GetString(4) : "",
                    PurPackUn  = (int)reader.GetDecimal(5),
                    PurPackMsr = !reader.IsDBNull(6) ? reader.GetString(6) : "",
                };
                response.Add(value.ItemCode, value);
                return value;
            });
        return response;
    }

    public async Task<ValidateAddItemResult> GetItemValidationInfo(string itemCode, string barCode, string warehouse, int? binEntry, bool enableBin) {
        var result = new ValidateAddItemResult();

        // Validate item and get basic item info
        const string itemQuery =
            """
                SELECT T1."ItemCode", T1."ItemName", T1."CodeBars", T1."InvntItem", 
                       COALESCE(T1."NumInBuy", 1) as "NumInBuy", 
                       COALESCE(T1."PurPackUn", 1) as "PurPackUn",
                       T3."BcdCode",
                       T4."WhsCode"
                FROM OITM T1
                LEFT OUTER JOIN OBCD T3 ON T3."ItemCode" = T1."ItemCode" AND T3."BcdCode" = @BarCode
                LEFT OUTER JOIN OITW T4 ON T4."ItemCode" = T1."ItemCode" AND T4."WhsCode" = @WhsCode
                WHERE T1."ItemCode" = @ItemCode
            """;

        var itemParams = new[] {
            new SqlParameter("@ItemCode", SqlDbType.NVarChar, 50) { Value = itemCode },
            new SqlParameter("@BarCode", SqlDbType.NVarChar, 254) { Value = barCode },
            new SqlParameter("@WhsCode", SqlDbType.NVarChar, 8) { Value   = warehouse }
        };

        var itemData = await dbService.QuerySingleAsync(itemQuery, itemParams, reader => new ItemValidationResposne(
            reader.GetString(0),
            reader.IsDBNull(1) ? null : reader.GetString(1),
            reader.IsDBNull(2) ? null : reader.GetString(2),
            reader.GetString(3) == "Y",
            (int)reader.GetDecimal(4),
            (int)reader.GetDecimal(5),
            reader.IsDBNull(6) ? null : reader.GetString(6),
            reader.IsDBNull(7) ? null : reader.GetString(7)
        ));

        if (itemData == null) {
            result.IsValidItem = false;
            return result;
        }

        result.IsValidItem = true;
        result.NumInBuy    = itemData.NumInBuy;
        result.PurPackUn   = itemData.PurPackUn;

        // Validate barcode
        result.IsValidBarCode = barCode == itemData.MainBarcode || itemData.Barcode != null;

        // Check if it's an inventory item
        result.IsInventoryItem = itemData.StockItem;

        // Check if item exists in warehouse
        result.ItemExistsInWarehouse = !string.IsNullOrWhiteSpace(itemData.Warehouse);

        if (!binEntry.HasValue) {
            return result;
        }

        // Validate bin if provided
        const string binQuery =
            """
                SELECT T5."AbsEntry", T5."WhsCode", COALESCE(T7."OnHandQty", 0) as "OnHandQty", T5."BinCode"
                FROM OBIN T5
                LEFT OUTER JOIN OIBQ T7 ON T7."ItemCode" = @ItemCode AND T7."BinAbs" = @BinEntry
                WHERE T5."AbsEntry" = @BinEntry
            """;

        var binParams = new[] {
            new SqlParameter("@BinEntry", SqlDbType.Int) { Value          = binEntry.Value },
            new SqlParameter("@ItemCode", SqlDbType.NVarChar, 50) { Value = itemCode }
        };

        var binData = await dbService.QuerySingleAsync(binQuery, binParams, reader => new BinValidationResponse(reader.GetInt32(0), reader.GetString(1), reader.GetDecimal(2), reader[3].ToString()));

        if (binData == null) {
            result.BinExists = false;
        }
        else {
            result.BinExists             = true;
            result.BinBelongsToWarehouse = binData.Warehouse == warehouse;
            result.AvailableQuantity     = binData.Stock;
            result.BinCode               = binData.BinCode;
        }

        return result;
    }
}