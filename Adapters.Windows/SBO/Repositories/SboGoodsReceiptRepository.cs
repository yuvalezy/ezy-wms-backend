using System.Data;
using System.Text;
using Adapters.Windows.SBO.Helpers;
using Adapters.Windows.SBO.Services;
using Adapters.Windows.Utils;
using Core.DTOs;
using Core.Enums;
using Core.Models;
using Microsoft.Data.SqlClient;

namespace Adapters.Windows.SBO.Repositories;

public class SboGoodsReceiptRepository(SboDatabaseService dbService, SboCompany sboCompany) {
    private readonly SourceDocumentRetrieval _sourceDocumentRetrieval = new(dbService);
    public async Task<GoodsReceiptValidationResult> ValidateGoodsReceiptAddItem(GoodsReceiptAddItemRequest request, string warehouse, List<ObjectKey> specificDocuments) {
        var response = new GoodsReceiptValidationResult {
            IsValid      = true,
            ErrorMessage = null,
            ReturnValue  = 0
        };
        const string checkItem =
            """
            select Case
                When @BarCode <> T0.CodeBars and T3.BcdCode is null Then -2
                When T0.PrchseItem = 'N' Then -5
                Else 0 End ValidationMessage
            from OITM T0
                left outer join OBCD T3 on T3.ItemCode = T0.ItemCode and T3.BcdCode = @BarCode
            """;
        int? result = await dbService.QuerySingleAsync<int?>(checkItem, [
            new SqlParameter("@ItemCode", SqlDbType.NVarChar, 50) { Value = request.ItemCode }
        ], reader => reader.GetInt32(0));

        switch (result) {
            case null:
                response.ReturnValue  = -1;
                response.IsValid      = false;
                response.ErrorMessage = "Item not found";
                return await Task.FromResult(response);
            case -2:
                response.ReturnValue  = -2;
                response.IsValid      = false;
                response.ErrorMessage = "Invalid barcode";
                break;
            case -5:
                response.ReturnValue  = -5;
                response.IsValid      = false;
                response.ErrorMessage = "Item is not for purchase";
                break;
        }

        if (specificDocuments.Count == 0)
            return await Task.FromResult(response);

        var parameters = new List<SqlParameter> {
            new("@ItemCode", SqlDbType.NVarChar, 50) { Value = request.ItemCode },
            new("@WhsCode", SqlDbType.NVarChar, 8) { Value   = warehouse }
        };
        var sb = new StringBuilder(" select 1 from ( ");

        for (int i = 0; i < specificDocuments.Count; i++) {
            if (i > 0) sb.Append(" union ");
            sb.Append($"select @ObjType{i} \"ObjType\", @DocEntry{i} \"DocEntry\"");
            parameters.Add(new SqlParameter($"@ObjType{i}", SqlDbType.Int) { Value  = specificDocuments[i].Type });
            parameters.Add(new SqlParameter($"@DocEntry{i}", SqlDbType.Int) { Value = specificDocuments[i].Entry });
        }

        sb.Append("""
                  ) X0
                       left outer join POR1 X1 on X1."DocEntry" = X0."DocEntry" and X1."ObjType" = X0."ObjType" and X1."ItemCode" = @ItemCode and X1."WhsCode" = @WhsCode
                       left outer join PCH1 X2 on X2."DocEntry" = X0."DocEntry" and X2."ObjType" = X0."ObjType" and X2."ItemCode" = @ItemCode and X2."WhsCode" = @WhsCode
                       left outer join PDN1 X3 on X3."DocEntry" = X0."DocEntry" and X3."ObjType" = X0."ObjType" and X3."ItemCode" = @ItemCode and X3."WhsCode" = @WhsCode
                  where X1."LineNum" is not null
                     or X2."LineNum" is not null
                     or X3."LineNum" is not null
                  """);

        int validateDocuments = await dbService.QuerySingleAsync(sb.ToString(), parameters.ToArray(), reader => reader.GetInt32(0));
        if (validateDocuments != 1) {
            response.ReturnValue  = -6;
            response.IsValid      = false;
            response.ErrorMessage = "Item was not found in any of the source documents";
        }

        return await Task.FromResult(response);
    }

    public async Task<IEnumerable<GoodsReceiptAddItemSourceDocument>> AddItemSourceDocuments(
        GoodsReceiptAddItemRequest request,
        string                     warehouse,
        GoodsReceiptType           type,
        string?                    cardCode,
        List<ObjectKey>            specificDocuments) {
        
        return await _sourceDocumentRetrieval.GetAllSourceDocuments(
            request.ItemCode,
            warehouse,
            request.Unit,
            type,
            cardCode,
            specificDocuments);
    }

    public async Task<ProcessGoodsReceiptResult> ProcessGoodsReceipt(int number, string warehouse, Dictionary<string, List<GoodsReceiptCreationData>> data, int series) {
        using var creation = new GoodsReceiptCreation(sboCompany, number, warehouse, series, data);
        return await Task.FromResult(creation.Execute());
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
                    throw new ApiErrorException(-1, new { objectType, documentEntry, documentNumber, docStatus });
                }

                documents.First(v => v.ObjectType == objectType && v.DocumentNumber == documentNumber).DocumentEntry = documentEntry;
            });
    }
}