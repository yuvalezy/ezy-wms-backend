using System.Data;
using System.Text;
using Adapters.Windows.SBO.Services;
using Adapters.Windows.Utils;
using Core.Interfaces;
using Core.Models;
using Core.Models.Settings;
using Microsoft.Data.SqlClient;

namespace Adapters.Windows.SBO.Repositories;

public class SboGeneralRepository(SboDatabaseService dbService, ISettings settings) {
    private readonly Filters filters = settings.Filters;

    public async Task<string?> GetCompanyNameAsync() {
        const string query = "SELECT COALESCE(\"PrintHeadr\", \"CompnyName\") FROM OADM";

        return await dbService.QuerySingleAsync(query, null, reader => reader.GetString(0));
    }

    public async Task<IEnumerable<Warehouse>> GetWarehousesAsync(string[]? filter) {
        string          query      = "select \"WhsCode\", \"WhsName\", \"BinActivat\" from OWHS";
        SqlParameter[]? parameters = null;

        if (filter?.Length > 0) {
            query      += SqlQueryUtils.BuildInClause("WhsCode", "WhsCode", filter.Length);
            parameters =  SqlQueryUtils.BuildInParameters("WhsCode", filter, parameters);
        }

        return await dbService.QueryAsync(query, parameters, reader => new Warehouse(
            reader.GetString(0),
            reader.GetString(1),
            reader.GetString(2) == "Y"
        ));
    }

    public async Task<(int itemCount, int binCount)> GetItemAndBinCountAsync(string warehouse) {
        const string query =
            """
            select (select Count(1) from OITW where "WhsCode" = @WhsCode and "OnHand" > 0)                                                 "ItemCheck",
                   (select Count(1) from OBIN where "WhsCode" = @WhsCode)                                                                  "BinCheck"
            order by 1, 2
            """;

        var parameters = new[] {
            new SqlParameter("@WhsCode", warehouse)
        };

        return await dbService.QuerySingleAsync(query, parameters, reader => (
            reader.GetInt32(0),
            reader.GetInt32(1)
        ));
    }

    public async Task<IEnumerable<ExternalValue>> GetVendorsAsync() {
        var sb = new StringBuilder("""select "CardCode", "CardName" from OCRD where "CardType" = 'S' """);
        if (!string.IsNullOrWhiteSpace(filters.Vendors))
            sb.Append($"and {filters.Vendors}");

        return await dbService.QueryAsync(sb.ToString(), null, reader => new ExternalValue {
            Id   = reader.GetString(0),
            Name = reader.GetString(1)
        });
    }

    public async Task<bool> ValidateVendorsAsync(string id) {
        var sb = new StringBuilder("""select 1 from OCRD where "CardCode" = @CardCode and "CardType" = 'S' """);
        if (!string.IsNullOrWhiteSpace(filters.Vendors))
            sb.Append($"and {filters.Vendors}");

        var parameters = new[] {
            new SqlParameter("@CardCode", id)
        };

        bool result = await dbService.QuerySingleAsync(sb.ToString(), parameters, _ => true);
        return result;
    }

    public async Task<BinLocation?> ScanBinLocationAsync(string bin) {
        const string query = $"select \"AbsEntry\", \"BinCode\" from OBIN where \"BinCode\" = @BinCode";

        var parameters = new[] {
            new SqlParameter("@BinCode", bin)
        };

        return await dbService.QuerySingleAsync(query, parameters, reader => new BinLocation {
            Entry = reader.GetInt32(0),
            Code  = reader.GetString(1)
        });
    }

    public async Task<IEnumerable<Item>> ScanItemBarCodeAsync(string scanCode, bool item = false) {
        string query;
        if (!item) {
            query = """
                    SELECT T0."ItemCode", T1."ItemName", T2."Father", T1."U_LW_BOX_NUM" "BoxNumber"
                    FROM OBCD T0
                             INNER JOIN OITM T1 ON T0."ItemCode" = T1."ItemCode"
                    left outer join ITT1 T2 on T2."Code" = T0."ItemCode"
                    WHERE T0."BcdCode" = @ScanCode
                    """;
        }
        else {
            query = """
                    SELECT T0."ItemCode", T1."ItemName", T2."Father", T1."U_LW_BOX_NUM" "BoxNumber"
                    FROM OITM T1
                             left outer JOIN OBCD T0 ON T0."ItemCode" = T1."ItemCode"
                    left outer join ITT1 T2 on T2."Code" = T0."ItemCode"
                    WHERE T1."ItemCode" = @ScanCode or T0."BcdCode" = @ScanCode
                    """;
        }

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

    public async Task<IEnumerable<ItemCheckResponse>> ItemCheckAsync(string itemCode, string barcode) {
        var response = new List<ItemCheckResponse>();
        if (string.IsNullOrWhiteSpace(itemCode) && string.IsNullOrWhiteSpace(barcode))
            return response;

        var data = new List<ItemCheckResponse>();
        if (!string.IsNullOrWhiteSpace(barcode)) {
            const string query = """
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
                ItemName   = reader.GetString(1),
                BuyUnitMsr = reader.GetString(2),
                NumInBuy   = reader.GetInt32(3),
                PurPackMsr = reader.GetString(4),
                PurPackUn  = reader.GetInt32(5)
            });
            data.AddRange(items);
        }
        else {
            const string query = """select "ItemCode", "ItemName", "BuyUnitMsr" , COALESCE("NumInBuy", 1)  "NumInBuy", "PurPackMsr" , COALESCE("PurPackUn", 1) "PurPackUn" from OITM where "ItemCode" = @ItemCode""";
            var parameters = new[] {
                new SqlParameter("@ItemCode", SqlDbType.NVarChar, 50) { Value = itemCode }
            };
            var items = await dbService.QueryAsync(query, parameters, reader => new ItemCheckResponse {
                ItemCode   = reader.GetString(0),
                ItemName   = reader.GetString(1),
                BuyUnitMsr = reader.GetString(2),
                NumInBuy   = reader.GetInt32(3),
                PurPackMsr = reader.GetString(4),
                PurPackUn  = reader.GetInt32(5)
            });
            data.AddRange(items);
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

    public async Task<IEnumerable<BinContent>> BinCheckAsync(int binEntry) {
        const string query = """
                             select T1."ItemCode", T2."ItemName", T1."OnHandQty" "OnHand", 
                             COALESCE(T2."NumInBuy", 1) "NumInBuy", T2."BuyUnitMsr",
                             COALESCE(T2."PurPackUn", 1) "PurPackUn", T2."PurPackMsr"
                             from OIBQ T1 
                             inner join OITM T2 on T2."ItemCode" = T1."ItemCode"
                             where T1."BinAbs" = @AbsEntry and T1."OnHandQty" <> 0
                             order by 1
                             """;
        var parameters = new[] {
            new SqlParameter("@AbsEntry", SqlDbType.Int) { Value = binEntry }
        };

        return await dbService.QueryAsync(query, parameters, reader => new BinContent {
            ItemCode   = reader.GetString(0),
            ItemName   = reader.GetString(1),
            OnHand     = reader.GetDouble(2),
            NumInBuy   = reader.GetInt32(3),
            BuyUnitMsr = reader.GetString(4),
            PurPackUn  = reader.GetInt32(5),
            PurPackMsr = reader.GetString(6)
        });
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
            new SqlParameter("@WhsCode", SqlDbType.NVarChar, 8) { Value = whsCode }
        };

        return await dbService.QueryAsync(query, parameters, reader => new ItemStockResponse {
            BinCode  = reader.GetString(0),
            Quantity = reader.GetInt32(1)
        });
    }
}