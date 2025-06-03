using System.Data;
using Adapters.Windows.SBO.Helpers;
using Adapters.Windows.SBO.Services;
using Core.DTOs;
using Core.Interfaces;
using Core.Models;
using Microsoft.Data.SqlClient;

namespace Adapters.Windows.SBO.Repositories;

public class SboItemRepository(SboDatabaseService dbService, SboCompany sboCompany, ISettings settings) {
    public async Task<IEnumerable<Item>> ScanItemBarCodeAsync(string scanCode, bool item = false) {
        string query;
        query = !item
            ? """
              SELECT T0."ItemCode", T1."ItemName", T2."Father", T1."U_LW_BOX_NUM" "BoxNumber"
              FROM OBCD T0
                       INNER JOIN OITM T1 ON T0."ItemCode" = T1."ItemCode"
              left outer join ITT1 T2 on T2."Code" = T0."ItemCode"
              WHERE T0."BcdCode" = @ScanCode
              """
            : """
              SELECT T0."ItemCode", T1."ItemName", T2."Father", T1."U_LW_BOX_NUM" "BoxNumber"
              FROM OITM T1
                       left outer JOIN OBCD T0 ON T0."ItemCode" = T1."ItemCode"
              left outer join ITT1 T2 on T2."Code" = T0."ItemCode"
              WHERE T1."ItemCode" = @ScanCode or T0."BcdCode" = @ScanCode
              """;

        var parameters = new[] {
            new SqlParameter("@ScanCode", SqlDbType.NVarChar, 50) { Value = scanCode }
        };

        return await dbService.QueryAsync(query, parameters, reader => {
            var item = new Item(reader.GetString(0));
            if (!reader.IsDBNull(1))
                item.Name = reader.GetString(1);
            if (!reader.IsDBNull(2))
                item.Father = reader.GetString(2);
            if (!reader.IsDBNull(3))
                item.BoxNumber = reader.GetInt32(3);
            return item;
        });
    }

    public async Task<IEnumerable<ItemCheckResponse>> ItemCheckAsync(string? itemCode, string? barcode) {
        var response = new List<ItemCheckResponse>();
        if (string.IsNullOrWhiteSpace(itemCode) && string.IsNullOrWhiteSpace(barcode))
            return response;

        var data = new List<ItemCheckResponse>();
        if (!string.IsNullOrWhiteSpace(itemCode)) {
            const string query =
                """select "ItemCode", "ItemName", "BuyUnitMsr" , COALESCE("NumInBuy", 1)  "NumInBuy", "PurPackMsr" , COALESCE("PurPackUn", 1) "PurPackUn" from OITM where "ItemCode" = @ItemCode""";
            var parameters = new[] {
                new SqlParameter("@ItemCode", SqlDbType.NVarChar, 50) { Value = itemCode }
            };
            var items = await dbService.QueryAsync(query, parameters, reader => new ItemCheckResponse {
                ItemCode   = reader.GetString(0),
                ItemName   = !reader.IsDBNull(1) ? reader.GetString(1) : "",
                BuyUnitMsr = !reader.IsDBNull(2) ? reader.GetString(2) : "",
                NumInBuy   = (int)reader.GetDecimal(3),
                PurPackMsr = !reader.IsDBNull(4) ? reader.GetString(4) : "",
                PurPackUn  = (int)reader.GetDecimal(5)
            });
            data.AddRange(items);
        }
        else if (!string.IsNullOrWhiteSpace(barcode)) {
            const string query =
                """
                select T0."ItemCode"
                     , T1."ItemName"
                     , T1."BuyUnitMsr"
                     , COALESCE(T1."NumInBuy", 1)  "NumInBuy"
                     , T1."PurPackMsr"
                     , COALESCE(T1."PurPackUn", 1) "PurPackUn"
                from OBCD T0
                         inner join OITM T1 on T1."ItemCode" = T0."ItemCode"
                where T0."BcdCode" = @ScanCode
                """;
            var parameters = new[] {
                new SqlParameter("@ScanCode", SqlDbType.NVarChar, 255) { Value = barcode }
            };
            var items = await dbService.QueryAsync(query, parameters, reader => new ItemCheckResponse {
                ItemCode   = reader.GetString(0),
                ItemName   = !reader.IsDBNull(1) ? reader.GetString(1) : "",
                BuyUnitMsr = !reader.IsDBNull(2) ? reader.GetString(2) : "",
                NumInBuy   = (int)reader.GetDecimal(3),
                PurPackMsr = !reader.IsDBNull(4) ? reader.GetString(4) : "",
                PurPackUn  = (int)reader.GetDecimal(5)
            });
            data.AddRange(items);
        }
        else {
            throw new ArgumentException("Either itemCode or barcode must be provided.");
        }

        const string barcodeQuery = """select "BcdCode" from OBCD where "ItemCode" = @ItemCode""";
        foreach (var item in data) {
            var barcodeParameters = new[] {
                new SqlParameter("@ItemCode", SqlDbType.NVarChar, 50) { Value = item.ItemCode }
            };
            var barcodes = await dbService.QueryAsync(barcodeQuery, barcodeParameters, reader => reader.GetString(0));
            item.Barcodes.AddRange(barcodes);
            response.Add(item);
        }

        return response;
    }

    public async Task<IEnumerable<ItemStockResponse>> ItemStockAsync(string itemCode, string whsCode) {
        const string query = """
                             select T1."BinCode", T0."OnHandQty"
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

        return await dbService.QueryAsync(query, parameters, reader => new ItemStockResponse {
            BinCode  = reader.GetString(0),
            Quantity = Convert.ToInt32(reader[1])
        });
    }

    public Task<UpdateItemBarCodeResponse> UpdateItemBarCode(UpdateBarCodeRequest request) {
        using var update = new ItemBarCodeUpdate(dbService, sboCompany, request.ItemCode, request.AddBarcodes, request.RemoveBarcodes);
        return Task.FromResult(update.Execute());
    }
}