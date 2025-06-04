using System.Data;
using System.Text;
using Adapters.Windows.SBO.Helpers;
using Adapters.Windows.SBO.Services;
using Adapters.Windows.Utils;
using Core.DTOs;
using Core.Interfaces;
using Core.Models;
using Core.Models.Settings;
using Microsoft.Data.SqlClient;

namespace Adapters.Windows.SBO.Repositories;

public class SboGeneralRepository(SboDatabaseService dbService, ISettings settings, SboCompany sboCompany) {
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
            ItemName   = reader.IsDBNull(1) ? string.Empty : reader.GetString(1),
            OnHand     = Convert.ToInt32(reader[2]),
            NumInBuy   = Convert.ToInt32(reader[3]),
            BuyUnitMsr = reader.IsDBNull(4) ? string.Empty : reader.GetString(4),
            PurPackUn   = Convert.ToInt32(reader[5]),
            PurPackMsr = reader.IsDBNull(6) ? string.Empty : reader.GetString(6)
        });
    }

    public ProcessTransferResponse ProcessTransfer(Guid transferId, string whsCode, string? comments, Dictionary<string, TransferCreationData> data) {
        using var transferCreation = new TransferCreation(dbService, sboCompany, transferId, whsCode, comments, data);
        return transferCreation.Execute();
        //todo send alert to sap
//     private void ProcessTransferSendAlert(int id, List<string> sendTo, TransferCreation creation) {
//         try {
//             using var alert = new Alert();
//             alert.Subject = string.Format(ErrorMessages.WMSTransactionAlert, id);
//             var transactionColumn = new AlertColumn(ErrorMessages.WMSTransaction);
//             var transferColumn    = new AlertColumn(ErrorMessages.InventoryTransfer, true);
//             alert.Columns.AddRange([transactionColumn, transferColumn]);
//             transactionColumn.Values.Add(new AlertValue(id.ToString()));
//             transferColumn.Values.Add(new AlertValue(creation.Number.ToString(), "67", creation.Entry.ToString()));
//
//             alert.Send(sendTo);
//         }
//         catch (Exception e) {
//             //todo log error handler
//         }
//     }
    }
}