using System.Data;
using System.Text;
using Adapters.CrossPlatform.Enums;
using Adapters.CrossPlatform.SBO.Helpers;
using Adapters.CrossPlatform.SBO.Services;
using Adapters.CrossPlatform.Utils;
using Core.DTOs.Items;
using Core.DTOs.Transfer;
using Core.Enums;
using Core.Interfaces;
using Core.Models;
using Core.Models.Settings;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;

namespace Adapters.CrossPlatform.SBO.Repositories;

public class SboGeneralRepository(SboDatabaseService dbService, ISettings settings, SboCompany sboCompany, ILoggerFactory loggerFactory) {
    private readonly Filters filters = settings.Filters;

    public async Task<string?> GetCompanyNameAsync() {
        const string query = "SELECT COALESCE(\"PrintHeadr\", \"CompnyName\") FROM OADM";

        return await dbService.QuerySingleAsync(query, null, reader => reader.GetString(0));
    }

    public async Task<IEnumerable<WarehouseResponse>> GetWarehousesAsync(string[]? filter) {
        string          query      = "select \"WhsCode\", \"WhsName\", \"BinActivat\" from OWHS";
        SqlParameter[]? parameters = null;

        if (filter?.Length > 0) {
            query      += SqlQueryUtils.BuildInClause("WhsCode", "WhsCode", filter.Length);
            parameters =  SqlQueryUtils.BuildInParameters("WhsCode", filter, parameters);
        }

        return await dbService.QueryAsync(query, parameters, reader => new WarehouseResponse(
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

    public async Task<IEnumerable<ExternalValue<string>>> GetVendorsAsync() {
        var sb = new StringBuilder("""select "CardCode", "CardName" from OCRD where "CardType" = 'S' """);
        if (!string.IsNullOrWhiteSpace(filters.Vendors))
            sb.Append($"and {filters.Vendors}");

        return await dbService.QueryAsync(sb.ToString(), null, reader => new ExternalValue<string> {
            Id   = reader.GetString(0),
            Name = reader.GetString(1)
        });
    }
    public async Task<ExternalValue<string>?> GetVendorAsync(string cardCode) {
        const string query = """select "CardCode", "CardName" from OCRD where "CardCode" = @CardCode and "CardType" = 'S'""";
        return await dbService.QuerySingleAsync(query, [new SqlParameter("@CardCode", SqlDbType.NVarChar, 50){Value = cardCode}], reader => new ExternalValue<string> {
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

    public async Task<BinLocationResponse?> ScanBinLocationAsync(string bin) {
        const string query = $"select \"AbsEntry\", \"BinCode\" from OBIN where \"BinCode\" = @BinCode";

        var parameters = new[] {
            new SqlParameter("@BinCode", bin)
        };

        return await dbService.QuerySingleAsync(query, parameters, reader => new BinLocationResponse {
            Entry = reader.GetInt32(0),
            Code  = reader.GetString(1)
        });
    }

    public async Task<string?> GetBinCodeAsync(int binEntry) {
        const string query = """select "BinCode" from OBIN where "AbsEntry" = @BinEntry""";

        var parameters = new[] {
            new SqlParameter("@BinEntry", SqlDbType.Int) { Value = binEntry }
        };

        return await dbService.QuerySingleAsync(query, parameters, reader => reader.GetString(0));
    }

    public async Task<IEnumerable<BinContentResponse>> BinCheckAsync(int binEntry) {
        const string query = """
                             select T1."ItemCode", T2."ItemName", T1."OnHandQty" "OnHand", 
                             COALESCE(T2."NumInBuy", 1) "NumInBuy", T2."BuyUnitMsr",
                             COALESCE(T2."PurPackUn", 1) "PurPackUn", T2."PurPackMsr", T3."BinCode"
                             from OIBQ T1 
                             inner join OITM T2 on T2."ItemCode" = T1."ItemCode"
                             inner join OBIN T3 on T3."AbsEntry" = T1."BinAbs" and T3."WhsCode" = T1."WhsCode"
                             where T1."BinAbs" = @AbsEntry and T1."OnHandQty" <> 0
                             order by 1
                             """;
        var parameters = new[] {
            new SqlParameter("@AbsEntry", SqlDbType.Int) { Value = binEntry }
        };

        return await dbService.QueryAsync(query, parameters, reader => new BinContentResponse {
            ItemCode   = reader.GetString(0),
            ItemName   = reader.IsDBNull(1) ? string.Empty : reader.GetString(1),
            OnHand     = Convert.ToInt32(reader[2]),
            NumInBuy   = Convert.ToInt32(reader[3]),
            BuyUnitMsr = reader.IsDBNull(4) ? string.Empty : reader.GetString(4),
            PurPackUn  = Convert.ToInt32(reader[5]),
            PurPackMsr = reader.IsDBNull(6) ? string.Empty : reader.GetString(6),
            BinCode    = reader.GetString(7)
        });
    }

    public async Task<int> GetSeries(ObjectTypes objectType) => await GetSeries(((int)objectType).ToString());

    public async Task<int> GetSeries(string objectCode) {
        const string query =
            """
            select top 1 T1."Series"
            from OFPR T0
                     inner join NNM1 T1 on T1."ObjectCode" = @ObjectCode and T1."Indicator" = T0."Indicator"
            where (T1."LastNum" is null or T1."LastNum" >= "NextNumber")
            and T0."F_RefDate" <= @Date and T0."T_RefDate" >= @Date
            """;
        var parameters = new[] {
            new SqlParameter("@ObjectCode", SqlDbType.NVarChar, 50) { Value = objectCode },
            new SqlParameter("@Date", SqlDbType.DateTime) { Value           = DateTime.UtcNow }
        };
        return await dbService.QuerySingleAsync(query, parameters, reader => reader.GetInt32(0));
    }


    public async Task<ProcessTransferResponse> ProcessTransfer(int transferNumber, string whsCode, string? comments, Dictionary<string, TransferCreationDataResponse> data) {
        int       series           = await GetSeries(ObjectTypes.oStockTransfer);
        using var transferCreation = new TransferCreation(sboCompany, transferNumber, whsCode, comments, series, data, loggerFactory);
        try {
            return transferCreation.Execute();
        }
        catch (Exception e) {
            return new ProcessTransferResponse {
                Success      = false,
                Status       = ResponseStatus.Error,
                ErrorMessage = e.Message
            };
        }
    }

}