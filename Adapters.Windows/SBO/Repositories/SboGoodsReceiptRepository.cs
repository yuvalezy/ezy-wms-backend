using System.Data;
using System.Reflection.Metadata;
using System.Text;
using Adapters.Windows.SBO.Helpers;
using Adapters.Windows.SBO.Services;
using Adapters.Windows.Utils;
using Core.DTOs;
using Core.Enums;
using Core.Models;
using Microsoft.Data.SqlClient;
using SAPbobsCOM;

namespace Adapters.Windows.SBO.Repositories;

public class SboGoodsReceiptRepository(SboDatabaseService dbService, SboCompany sboCompany) {
    public async Task<GoodsReceiptValidationResult> ValidateGoodsReceiptAddItem(GoodsReceiptAddItemRequest request, Guid userId) {
        // For now, return a simple validation result
        // This would typically validate against SAP B1 purchase orders, etc.
        return await Task.FromResult(new GoodsReceiptValidationResult {
            IsValid      = true,
            ErrorMessage = null,
            ReturnValue  = 0
        });
    }

    public async Task<ProcessGoodsReceiptResult> ProcessGoodsReceipt(int number, string warehouse, Dictionary<string, List<GoodsReceiptCreationData>> data) {
        // Get series for Goods Receipt PO
        int series = await dbService.QuerySingleAsync(
            "SELECT DfltSeries FROM ONNM WHERE ObjectCode = '20'",
            Array.Empty<SqlParameter>(),
            reader => reader.GetInt32(0)
        );

        using var creation = new GoodsReceiptCreation(sboCompany, number, warehouse, series, data);
        return await Task.FromResult(creation.Execute());
    }

    // Report queries
    public async Task<IEnumerable<dynamic>> GetGoodsReceiptAllReport(int number) {
        const string query = @"
            SELECT 
                ItemCode,
                ItemName,
                SUM(ScannedQuantity) as ScannedQuantity,
                SUM(ReceivedQuantity) as ReceivedQuantity,
                COUNT(*) as LineCount
            FROM GoodsReceiptReportView
            WHERE Number = @Number
            GROUP BY ItemCode, ItemName";

        var sqlParams = new[] {
            new SqlParameter("@Number", SqlDbType.Int) { Value = number }
        };

        return await dbService.QueryAsync(query, sqlParams, reader => new {
            ItemCode         = reader.GetString(0),
            ItemName         = reader.GetString(1),
            ScannedQuantity  = reader.GetDecimal(2),
            ReceivedQuantity = reader.GetDecimal(3),
            LineCount        = reader.GetInt32(4)
        });
    }

    public async Task<IEnumerable<dynamic>> GetGoodsReceiptAllReportDetails(int number, string itemCode) {
        const string query = @"
            SELECT 
                LineID,
                BarCode,
                Date,
                Quantity,
                Comments,
                Status
            FROM GoodsReceiptLineView
            WHERE Number = @Number AND ItemCode = @ItemCode";

        var sqlParams = new[] {
            new SqlParameter("@Number", SqlDbType.Int) { Value            = number },
            new SqlParameter("@ItemCode", SqlDbType.NVarChar, 50) { Value = itemCode }
        };

        return await dbService.QueryAsync(query, sqlParams, reader => new {
            LineID   = reader.GetGuid(0),
            BarCode  = reader.GetString(1),
            Date     = reader.GetDateTime(2),
            Quantity = reader.GetDecimal(3),
            Comments = reader.IsDBNull(4) ? null : reader.GetString(4),
            Status   = reader.GetString(5)
        });
    }

    public async Task<IEnumerable<dynamic>> GetGoodsReceiptVSExitReport(int number) {
        const string query = @"
            SELECT 
                ItemCode,
                ItemName,
                ReceivedQuantity,
                ExitQuantity,
                ReceivedQuantity - ExitQuantity as Variance
            FROM GoodsReceiptVSExitView
            WHERE Number = @Number";

        var sqlParams = new[] {
            new SqlParameter("@Number", SqlDbType.Int) { Value = number }
        };

        return await dbService.QueryAsync(query, sqlParams, reader => new {
            ItemCode         = reader.GetString(0),
            ItemName         = reader.GetString(1),
            ReceivedQuantity = reader.GetDecimal(2),
            ExitQuantity     = reader.GetDecimal(3),
            Variance         = reader.GetDecimal(4)
        });
    }

    public async Task<IEnumerable<dynamic>> GetGoodsReceiptValidateProcess(int number) {
        const string query = @"
            SELECT 
                LineID,
                ItemCode,
                ItemName,
                BarCode,
                Quantity,
                IsValid,
                ValidationMessage
            FROM GoodsReceiptValidateView
            WHERE Number = @Number";

        var sqlParams = new[] {
            new SqlParameter("@Number", SqlDbType.Int) { Value = number }
        };

        return await dbService.QueryAsync(query, sqlParams, reader => new {
            LineID            = reader.GetGuid(0),
            ItemCode          = reader.GetString(1),
            ItemName          = reader.GetString(2),
            BarCode           = reader.GetString(3),
            Quantity          = reader.GetDecimal(4),
            IsValid           = reader.GetBoolean(5),
            ValidationMessage = reader.IsDBNull(6) ? null : reader.GetString(6)
        });
    }

    public async Task ValidateGoodsReceiptDocuments(string warehouse, GoodsReceiptType type, List<DocumentParameter> documents) {
        string documentsQuery = documents.Aggregate("",
            (a, b) => a + a.UnionQuery() +
                      $"""
                       select {b.ObjectType} "ObjType",
                       {b.DocumentNumber} "DocNum",
                       (select top 1 "DocEntry" from O{QueryHelper.ObjectTable(b.ObjectType)} where "DocNum" = {b.DocumentNumber} order by "DocEntry" desc) "DocEntry"
                       """);

        var queryBuilder = new StringBuilder();
        queryBuilder.AppendLine("select X0.\"ObjType\", X0.\"DocNum\", X0.\"DocEntry\",");
        queryBuilder.AppendLine("Case");
        queryBuilder.AppendLine("    When COALESCE(X1.\"DocStatus\", X2.\"DocStatus\") is null Then 'E'");
        queryBuilder.AppendLine($"    When X0.\"ObjType\" = 18 and X2.\"isIns\" = '{(type == GoodsReceiptType.SpecificOrders ? 'N' : 'Y')}' Then 'R'");
        queryBuilder.AppendLine("    When Sum(COALESCE(X3.\"Quantity\", X4.\"Quantity\", 0)) = 0 Then 'W'");
        queryBuilder.AppendLine("    When COALESCE(X1.\"DocStatus\", X2.\"DocStatus\") = 'O' Then 'O'");
        queryBuilder.AppendLine("    Else 'C' End \"DocStatus\"");
        queryBuilder.AppendLine($"from ({documentsQuery}) X0");
        queryBuilder.AppendLine($"left outer join {(type == GoodsReceiptType.SpecificOrders ? "OPOR" : "OPDN")} X1 on X1.\"DocEntry\" = X0.\"DocEntry\" and X1.\"ObjType\" = X0.\"ObjType\"");
        queryBuilder.AppendLine("left outer join OPCH X2 on X2.\"DocEntry\" = X0.\"DocEntry\" and X2.\"ObjType\" = X0.\"ObjType\"");
        queryBuilder.AppendLine($"left outer join {(type == GoodsReceiptType.SpecificOrders ? "POR1" : "PDN1")} X3 on X3.\"DocEntry\" = X1.\"DocEntry\" and X3.\"WhsCode\" = @WhsCode");
        queryBuilder.AppendLine("left outer join PCH1 X4 on X4.\"DocEntry\" = X2.\"DocEntry\" and X4.\"WhsCode\" = @WhsCode");
        queryBuilder.AppendLine("group by X0.\"ObjType\", X0.\"DocNum\", X0.\"DocEntry\", X1.\"DocStatus\", X2.\"DocStatus\", X2.\"isIns\"");

        await dbService.ExecuteReaderAsync(queryBuilder.ToString(),
            [new SqlParameter("@WhsCode", SqlDbType.NVarChar, 8) { Value = warehouse }],
            reader => {
                string docStatus      = reader.GetString(reader.GetOrdinal("DocStatus"));
                int    objectType     = reader.GetInt32(reader.GetOrdinal("ObjType"));
                int    documentNumber = reader.GetInt32(reader.GetOrdinal("DocNum"));
                int    documentEntry  = reader.IsDBNull(reader.GetOrdinal("DocEntry")) ? -1 : reader.GetInt32(reader.GetOrdinal("DocEntry"));
                if (docStatus != "O") {
                    throw new ApiErrorException(-1, new { objectType, documentEntry, documentNumber, docStatus});
                }

                documents.First(v => v.ObjectType == objectType && v.DocumentNumber == documentNumber).DocumentEntry = documentEntry;
            });
    }
}