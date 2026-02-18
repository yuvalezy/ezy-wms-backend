using System.Data;
using System.Text;
using Adapters.Common.SBO.Enums;
using Adapters.Common.SBO.Services;
using Adapters.Common.Utils;
using Core.DTOs.Items;
using Core.DTOs.Settings;
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

    private readonly Dictionary<int, string> binCodes = new();
    public async Task<string?> GetBinCodeAsync(int binEntry) {
        if (binCodes.TryGetValue(binEntry, out string? binCode))
            return binCode;
        
        const string query = """select "BinCode" from OBIN where "AbsEntry" = @BinEntry""";

        var parameters = new[] {
            new SqlParameter("@BinEntry", SqlDbType.Int) { Value = binEntry }
        };

        binCode = await dbService.QuerySingleAsync(query, parameters, reader => reader.GetString(0));
        if (!string.IsNullOrWhiteSpace(binCode)) {
            binCodes[binEntry] = binCode;
        }
        return binCode;
    }

    private const int MaxBatchSize = 500;

    public async Task<Dictionary<int, IEnumerable<BinContentResponse>>> BulkBinCheckAsync(int[] binEntries) {
        var result = new Dictionary<int, IEnumerable<BinContentResponse>>();
        if (binEntries.Length == 0) return result;

        var customFields = GetCustomFields();
        var grouped = new Dictionary<int, List<BinContentResponse>>();

        foreach (var batch in binEntries.Chunk(MaxBatchSize)) {
            var inParams = string.Join(", ", batch.Select((_, i) => $"@AbsEntry{i}"));

            var queryBuilder = new StringBuilder();
            queryBuilder.Append("""
                               select T1."BinAbs" as "BinAbs", T1."ItemCode" as "ItemCode", OITM."ItemName" as "ItemName", T1."OnHandQty" as "OnHand",
                               COALESCE(OITM."NumInBuy", 1) as "NumInBuy", OITM."BuyUnitMsr" as "BuyUnitMsr",
                               COALESCE(OITM."PurPackUn", 1) as "PurPackUn", OITM."PurPackMsr" as "PurPackMsr", T3."BinCode" as "BinCode",
                               OITM."PurFactor1", OITM."PurFactor2", OITM."PurFactor3", OITM."PurFactor4"
                               """);
            CustomFieldsHelper.AppendCustomFieldsToQuery(queryBuilder, customFields);
            queryBuilder.Append($"""
                               from OIBQ T1
                               inner join OITM on OITM."ItemCode" = T1."ItemCode"
                               inner join OBIN T3 on T3."AbsEntry" = T1."BinAbs" and T3."WhsCode" = T1."WhsCode"
                               where T1."BinAbs" in ({inParams}) and T1."OnHandQty" <> 0
                               order by T1."ItemCode"
                               """);

            var parameters = new SqlParameter[batch.Length];
            for (int i = 0; i < batch.Length; i++) {
                parameters[i] = new SqlParameter($"@AbsEntry{i}", SqlDbType.Int) { Value = batch[i] };
            }

            await dbService.QueryAsync(queryBuilder.ToString(), parameters, reader => {
                int binAbs = reader.GetInt32(reader.GetOrdinal("BinAbs"));
                var response = new BinContentResponse {
                    OnHand = Convert.ToDouble(reader["OnHand"]),
                    BinCode = reader["BinCode"] as string ?? string.Empty
                };
                ItemResponseHelper.PopulateItemResponse(reader, response);
                CustomFieldsHelper.ReadCustomFields(reader, customFields, response);

                if (!grouped.TryGetValue(binAbs, out var list)) {
                    list = new List<BinContentResponse>();
                    grouped[binAbs] = list;
                }
                list.Add(response);
                return response;
            });
        }

        foreach (var kvp in grouped) {
            result[kvp.Key] = kvp.Value;
        }

        foreach (var entry in binEntries) {
            result.TryAdd(entry, Enumerable.Empty<BinContentResponse>());
        }

        return result;
    }

    public async Task<Dictionary<int, string>> BulkGetBinCodesAsync(int[] binEntries) {
        var result = new Dictionary<int, string>();
        if (binEntries.Length == 0) return result;

        foreach (var batch in binEntries.Chunk(MaxBatchSize)) {
            var inParams = string.Join(", ", batch.Select((_, i) => $"@BinEntry{i}"));
            var query = $"""select "AbsEntry", "BinCode" from OBIN where "AbsEntry" in ({inParams})""";

            var parameters = new SqlParameter[batch.Length];
            for (int i = 0; i < batch.Length; i++) {
                parameters[i] = new SqlParameter($"@BinEntry{i}", SqlDbType.Int) { Value = batch[i] };
            }

            await dbService.QueryAsync(query, parameters, reader => {
                result[reader.GetInt32(0)] = reader.GetString(1);
                return true;
            });
        }

        return result;
    }

    public async Task<IEnumerable<BinContentResponse>> BinCheckAsync(int binEntry) {
        var (query, customFields) = BuildBinCheckQuery();
        
        var parameters = new[] {
            new SqlParameter("@AbsEntry", SqlDbType.Int) { Value = binEntry }
        };

        return await dbService.QueryAsync(query, parameters, reader => MapBinContentResponse(reader, customFields));
    }

    private (string query, CustomField[] customFields) BuildBinCheckQuery() {
        var queryBuilder = new StringBuilder();
        queryBuilder.Append("""
                           select T1."ItemCode" as "ItemCode", OITM."ItemName" as "ItemName", T1."OnHandQty" as "OnHand", 
                           COALESCE(OITM."NumInBuy", 1) as "NumInBuy", OITM."BuyUnitMsr" as "BuyUnitMsr",
                           COALESCE(OITM."PurPackUn", 1) as "PurPackUn", OITM."PurPackMsr" as "PurPackMsr", T3."BinCode" as "BinCode",
                           OITM."PurFactor1", OITM."PurFactor2", OITM."PurFactor3", OITM."PurFactor4"
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

    private CustomField[] GetCustomFields() => CustomFieldsHelper.GetCustomFields(settings, "Items");

    private BinContentResponse MapBinContentResponse(IDataReader reader, CustomField[] customFields) {
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

    public async Task<IEnumerable<ExternalSystemUserResponse>> GetExternalSystemUsersAsync() {
        const string query = """
            SELECT USER_CODE AS "UserId", COALESCE(U_NAME, USER_CODE) AS "UserName"
            FROM OUSR
            ORDER BY 2
            """;
        return await dbService.QueryAsync(query, null, reader => new ExternalSystemUserResponse {
            UserId = reader.GetString(0),
            UserName = reader.GetString(1)
        });
    }
}