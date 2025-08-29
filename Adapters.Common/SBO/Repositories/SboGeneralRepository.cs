using System.Data;
using System.Text;
using Adapters.Common.SBO.Enums;
using Adapters.Common.SBO.Services;
using Adapters.Common.Utils;
using Core.DTOs.Items;
using Core.Interfaces;
using Core.Models;
using Core.Models.Settings;
using Microsoft.Data.SqlClient;

namespace Adapters.Common.SBO.Repositories;

public class SboGeneralRepository(SboDatabaseService dbService, ISettings settings) {
    private readonly Filters filters = settings.Filters;

    public async Task<bool> ValidateUserDefinedFieldAsync(string table, string field) {
        const string query = """select 1 from CUFD where "TableID" = @TableID and "AliasID" = @AliasID""";
        var parameters = new[] {
            new SqlParameter("@TableID", SqlDbType.NVarChar, 50) { Value = table },
            new SqlParameter("@AliasID", SqlDbType.NVarChar, 50) { Value = field }
        };
        return await dbService.QuerySingleAsync(query, parameters, _ => true);
    }

    public async Task<string?> GetCompanyNameAsync() {
        const string query = "SELECT COALESCE(\"PrintHeadr\", \"CompnyName\") FROM OADM";

        return await dbService.QuerySingleAsync(query, null, reader => reader.GetString(0));
    }

    public async Task<IEnumerable<WarehouseResponse>> GetWarehousesAsync(string[]? filter) {
        string          query      = "select \"WhsCode\", \"WhsName\", \"BinActivat\", \"DftBinAbs\" from OWHS";
        SqlParameter[]? parameters = null;

        if (filter?.Length > 0) {
            query      += SqlQueryUtils.BuildInClause("WhsCode", "WhsCode", filter.Length);
            parameters =  SqlQueryUtils.BuildInParameters("WhsCode", filter, parameters);
        }

        return await dbService.QueryAsync(query, parameters, reader => new WarehouseResponse(
            reader.GetString(0),
            reader.GetString(1),
            reader.GetString(2) == "Y",
            reader[3] != DBNull.Value ? reader.GetInt32(3) : null
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

    private Dictionary<int, string> BinCodes = new();
    public async Task<string?> GetBinCodeAsync(int binEntry) {
        if (BinCodes.TryGetValue(binEntry, out string? binCode))
            return binCode;
        
        const string query = """select "BinCode" from OBIN where "AbsEntry" = @BinEntry""";

        var parameters = new[] {
            new SqlParameter("@BinEntry", SqlDbType.Int) { Value = binEntry }
        };

        binCode = await dbService.QuerySingleAsync(query, parameters, reader => reader.GetString(0));
        if (!string.IsNullOrWhiteSpace(binCode)) {
            BinCodes[binEntry] = binCode;
        }
        return binCode;
    }

    public async Task<IEnumerable<BinContentResponse>> BinCheckAsync(int binEntry) {
        var (query, customFields) = BuildBinCheckQuery();
        
        var parameters = new[] {
            new SqlParameter("@AbsEntry", SqlDbType.Int) { Value = binEntry }
        };

        return await dbService.QueryAsync(query, parameters, reader => MapBinContentResponse(reader, customFields));
    }

    private (string query, List<CustomField> customFields) BuildBinCheckQuery() {
        var queryBuilder = new StringBuilder();
        queryBuilder.Append("""
                           select T1."ItemCode" as "ItemCode", OITM."ItemName" as "ItemName", T1."OnHandQty" as "OnHand", 
                           COALESCE(OITM."NumInBuy", 1) as "NumInBuy", OITM."BuyUnitMsr" as "BuyUnitMsr",
                           COALESCE(OITM."PurPackUn", 1) as "PurPackUn", OITM."PurPackMsr" as "PurPackMsr", T3."BinCode" as "BinCode"
                           """);

        var customFields = GetCustomFields();
        CustomFieldsHelper.AppendCustomFieldsToQuery(queryBuilder, customFields);

        queryBuilder.Append("""
                           from OIBQ T1 
                           inner join OITM on OITM."ItemCode" = T1."ItemCode"
                           inner join OBIN T3 on T3."AbsEntry" = T1."BinAbs" and T3."WhsCode" = T1."WhsCode"
                           where T1."BinAbs" = @AbsEntry and T1."OnHandQty" <> 0
                           order by 1
                           """);

        return (queryBuilder.ToString(), customFields);
    }

    private List<CustomField> GetCustomFields() => CustomFieldsHelper.GetCustomFields(settings, "Items");

    private BinContentResponse MapBinContentResponse(IDataReader reader, List<CustomField> customFields) {
        var response = new BinContentResponse {
            OnHand = Convert.ToDouble(reader["OnHand"]),
            BinCode = reader["BinCode"] as string ?? string.Empty
        };
        ItemResponseHelper.PopulateItemResponse(reader, response);
        CustomFieldsHelper.ReadCustomFields(reader, customFields, response);
        return response;
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
}