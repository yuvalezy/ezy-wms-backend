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
        string                     cardCode,
        List<ObjectKey>            specificDocuments) {
        var response = new List<GoodsReceiptAddItemSourceDocument>();

        var entries = specificDocuments.Where(v => v.Type == 22).Select(v => v.Entry).ToArray();
        if (type is GoodsReceiptType.All or GoodsReceiptType.SpecificOrders && entries.Length > 0) {
            var parameters = new List<SqlParameter> {
                new("@WhsCode", SqlDbType.NVarChar, 8) { Value   = warehouse },
                new("@ItemCode", SqlDbType.NVarChar, 50) { Value = request.ItemCode },
                new("@Unit", SqlDbType.SmallInt) { Value    = (short)request.Unit }
            };
            var sb = new StringBuilder();
            sb.Append("""
                      select T0."ObjType", T0."DocEntry", T0."LineNum", T0."OpenInvQty"
                      from POR1 T0
                      inner join OPOR T1 on T1."DocEntry" = T0."DocEntry" and T1."DocStatus" = 'O'
                      """);
            if (type == GoodsReceiptType.All && !string.IsNullOrWhiteSpace(cardCode)) {
                sb.Append(" and T1.\"CardCode\" = @CardCode");
                parameters.Add(new SqlParameter("@CardCode", SqlDbType.NVarChar, 50) { Value = cardCode });
            }
            else {
                sb.Append(" and T0.\"DocEntry\" in (");
                for (int i = 0; i < entries.Length; i++) {
                    if (i > 0)
                        sb.Append(", ");
                    sb.Append($"@DocEntry{i}");
                    parameters.Add(new SqlParameter($"@DocEntry{i}", SqlDbType.Int) { Value = entries[i] });
                }

                sb.Append(") ");
            }

            sb.Append("""
                      where T0."ItemCode" = @ItemCode
                      and T0."LineStatus" = 'O'
                      and T0."WhsCode" = @WhsCode
                      and T0."OpenInvQty" > 0
                      and (@Unit != 0 and T0."UseBaseUn" = 'N' or @Unit = 0 and T0."UseBaseUn" = 'Y')
                      order by T1."CreateDate", T1.CreateTS;

                      """);

            var values = await dbService.QueryAsync(sb.ToString(), parameters.ToArray(), reader => {
                var sourceDocument = new GoodsReceiptAddItemSourceDocument {
                    Type     = reader.GetInt32(0),
                    Entry    = reader.GetInt32(1),
                    LineNum  = reader.GetInt32(2),
                    Quantity = (int)reader.GetDecimal(3)
                };
                response.Add(sourceDocument);
                return sourceDocument;
            });
            response.AddRange(values);
        }

        entries = specificDocuments.Where(v => v.Type == 20).Select(v => v.Entry).ToArray();
        if (type == GoodsReceiptType.SpecificReceipts && entries.Length > 0) {
            var parameters = new List<SqlParameter> {
                new("@WhsCode", SqlDbType.NVarChar, 8) { Value   = warehouse },
                new("@ItemCode", SqlDbType.NVarChar, 50) { Value = request.ItemCode },
                new("@Unit", SqlDbType.SmallInt) { Value    = (short)request.Unit }
            };
            var sb = new StringBuilder();
            sb.Append("""
                      select T0."ObjType", T0."DocEntry", T0."LineNum", T0."InvQty"
                      from PDN1 T0
                      inner join OPDN T1 on T1."DocEntry" = T0."DocEntry" and T1."CANCELED" not in ('C', 'Y')
                      """);
            sb.Append(" and T0.\"DocEntry\" in (");
            for (int i = 0; i < entries.Length; i++) {
                if (i > 0)
                    sb.Append(", ");
                sb.Append($"@DocEntry{i}");
                parameters.Add(new SqlParameter($"@DocEntry{i}", SqlDbType.Int) { Value = entries[i] });
            }

            sb.Append(") ");

            sb.Append("""
                      where T0."ItemCode" = @ItemCode
                      and T0."LineStatus" = 'O'
                      and T0."WhsCode" = @WhsCode
                      and T0."InvQty" > 0
                      and (@Unit != 0 and T0."UseBaseUn" = 'N' or @Unit = 0 and T0."UseBaseUn" = 'Y')
                      order by T1."CreateDate", T1.CreateTS;
                      """);

            var values = await dbService.QueryAsync(sb.ToString(), parameters.ToArray(), reader => {
                var sourceDocument = new GoodsReceiptAddItemSourceDocument {
                    Type     = reader.GetInt32(0),
                    Entry    = reader.GetInt32(1),
                    LineNum  = reader.GetInt32(2),
                    Quantity = (int)reader.GetDecimal(3)
                };
                response.Add(sourceDocument);
                return sourceDocument;
            });
            response.AddRange(values);
        }

        entries = specificDocuments.Where(v => v.Type == 18).Select(v => v.Entry).ToArray();
        {
            var parameters = new List<SqlParameter> {
                new("@WhsCode", SqlDbType.NVarChar, 8) { Value   = warehouse },
                new("@ItemCode", SqlDbType.NVarChar, 50) { Value = request.ItemCode },
                new("@Unit", SqlDbType.SmallInt) { Value    = (short)request.Unit },
                new("@Type", SqlDbType.SmallInt) { Value    = (short)type }
            };
            var sb = new StringBuilder();
            sb.Append("""
                      select T0."ObjType", T0."DocEntry", T0."LineNum", 
                             Case When @Type <> 2 Then T0."OpenInvQty" Else T0."InvQty" end
                      from PCH1 T0
                      inner join OPCH T1 on T1."DocEntry" = T0."DocEntry" and (
                               @Type <> 2 and T1."DocStatus" = 'O' and T1."isIns" = 'Y' and T0."LineStatus" = 'O'
                               or @Type = 2 and T1."CANCELED" not in ('C', 'Y') and T1."isIns" = 'N'
                      )
                      """);
            
            if (type == GoodsReceiptType.All && !string.IsNullOrWhiteSpace(cardCode)) {
                sb.Append(" and T1.\"CardCode\" = @CardCode");
                parameters.Add(new SqlParameter("@CardCode", SqlDbType.NVarChar, 50) { Value = cardCode });
            }
            else {
                sb.Append(" and T0.\"DocEntry\" in (");
                for (int i = 0; i < entries.Length; i++) {
                    if (i > 0)
                        sb.Append(", ");
                    sb.Append($"@DocEntry{i}");
                    parameters.Add(new SqlParameter($"@DocEntry{i}", SqlDbType.Int) { Value = entries[i] });
                }
            }

            sb.Append(") ");

            sb.Append("""
                      where T0."ItemCode" = @ItemCode
                      and T0."LineStatus" = 'O'
                      and T0."WhsCode" = @WhsCode
                      and  Case When @Type <> 2 Then T0."OpenInvQty" Else T0."InvQty" end > 0
                      and (@Unit != 0 and T0."UseBaseUn" = 'N' or @Unit = 0 and T0."UseBaseUn" = 'Y')
                      order by T1."CreateDate", T1.CreateTS;
                      """);

            var values = await dbService.QueryAsync(sb.ToString(), parameters.ToArray(), reader => {
                var sourceDocument = new GoodsReceiptAddItemSourceDocument {
                    Type     = reader.GetInt32(0),
                    Entry    = reader.GetInt32(1),
                    LineNum  = reader.GetInt32(2),
                    Quantity = (int)reader.GetDecimal(3)
                };
                response.Add(sourceDocument);
                return sourceDocument;
            });
            response.AddRange(values);
        }

        return response;
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