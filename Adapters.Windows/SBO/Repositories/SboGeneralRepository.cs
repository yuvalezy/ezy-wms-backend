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
}