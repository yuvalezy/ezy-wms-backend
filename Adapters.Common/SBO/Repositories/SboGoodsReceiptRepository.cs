using System.Data;
using System.Text;
using Adapters.Common.SBO.Helpers;
using Adapters.Common.SBO.Services;
using Adapters.Common.SBO.Utils;
using Adapters.Common.Utils;
using Core.DTOs;
using Core.DTOs.GoodsReceipt;
using Core.Enums;
using Core.Exceptions;
using Core.Interfaces;
using Core.Models;
using Core.Models.Settings;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;

namespace Adapters.Common.SBO.Repositories;

public class SboGoodsReceiptRepository(SboDatabaseService dbService, ILoggerFactory loggerFactory, ISettings settings) {
    private readonly SourceDocumentRetrieval sourceDocumentRetrieval = new(dbService);

    private List<CustomField> GetCustomFields() => CustomFieldsHelper.GetCustomFields(settings, "Items");

    public async Task<GoodsReceiptValidationResult> ValidateGoodsReceiptAddItem(string itemCode, string barcode, string warehouse, List<ObjectKey> specificDocuments) {
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
            where T0."ItemCode" = @ItemCode
            """;
        int? result = await dbService.QuerySingleAsync<int?>(checkItem, [
            new SqlParameter("@ItemCode", SqlDbType.NVarChar, 50) { Value = itemCode },
            new SqlParameter("@BarCode", SqlDbType.NVarChar, 254) { Value = barcode }
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
            new("@ItemCode", SqlDbType.NVarChar, 50) { Value = itemCode },
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

    public async Task<IEnumerable<GoodsReceiptAddItemSourceDocumentResponse>> AddItemSourceDocuments(
        string           itemCode, UnitType unit,
        string           warehouse,
        GoodsReceiptType type,
        string?          cardCode,
        List<ObjectKey>  specificDocuments) {
        return await sourceDocumentRetrieval.GetAllSourceDocuments(itemCode, warehouse, unit, type, cardCode, specificDocuments);
    }

    public async Task<IEnumerable<GoodsReceiptAddItemTargetDocumentsResponse>> AddItemTargetDocuments(string warehouse, string itemCode) {
        const string query =
            """
            -- Priority 1: Reserved A/R Invoices (highest priority)
            select 1 [Priority], T0."ObjType", T1.DocDate, T0."DocEntry", T0."LineNum", T0."OpenInvQty"
            from INV1 T0
                     inner join OINV T1 on T1."DocEntry" = T0."DocEntry" and T1."DocStatus" = 'O' and T1."isIns" = 'Y'
            where T0."ItemCode" = @ItemCode
              and T0."InvntSttus" = 'O'        -- Open inventory status
              and T0."WhsCode" = @WhsCode

            -- Priority 2: Open Sales Orders
            union
            select 2, T0."ObjType", T1.DocDate, T0."DocEntry", T0."LineNum", T0."OpenInvQty"
            from RDR1 T0
                     inner join ORDR T1 on T1."DocEntry" = T0."DocEntry" and T1."DocStatus" = 'O'
            where T0."ItemCode" = @ItemCode
              and T0."InvntSttus" = 'O'
              and T0."WhsCode" = @WhsCode

            -- Priority 3: Open Transfer Requests (lowest priority)
            union
            select 3, T0."ObjType", T1.DocDate, T0."DocEntry", T0."LineNum", T0."OpenInvQty"
            from WTQ1 T0
                     inner join OWTQ T1 on T1."DocEntry" = T0."DocEntry" and T1."DocStatus" = 'O'
            where T0."ItemCode" = @ItemCode
              and T0."InvntSttus" = 'O'
              and T0."FromWhsCod" = @WhsCode    -- From our warehouse
              and T0."WhsCode" = @WhsCode
            """;
        var result = await dbService.QueryAsync(query, [
            new SqlParameter("@ItemCode", SqlDbType.NVarChar, 50) { Value = itemCode },
            new SqlParameter("@WhsCode", SqlDbType.NVarChar, 8) { Value   = warehouse }
        ], reader => new GoodsReceiptAddItemTargetDocumentsResponse() {
            Priority = reader.GetInt16(0),
            Type     = reader.GetInt32(1),
            Date     = reader.GetDateTime(2),
            Entry    = reader.GetInt32(3),
            LineNum  = reader.GetInt32(4),
            Quantity = reader.GetInt32(5)
        });
        return result;
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

    public async Task<IEnumerable<GoodsReceiptValidateProcessDocumentsDataResponse>> GoodsReceiptValidateProcessDocumentsData(ObjectKey[] docs) {
        if (docs.Length == 0)
            return [];

        var (query, customFields) = BuildProcessDocumentsDataQuery(docs);
        var parameters = new SqlParameter[docs.Length * 2];
        for (int i = 0; i < docs.Length; i++) {
            parameters[i * 2]     = new SqlParameter($"@ObjType{i}", SqlDbType.Int) { Value  = docs[i].Type };
            parameters[i * 2 + 1] = new SqlParameter($"@DocEntry{i}", SqlDbType.Int) { Value = docs[i].Entry };
        }

        var control = new Dictionary<(int Type, int Entry), GoodsReceiptValidateProcessDocumentsDataResponse>();
        var response = await dbService.QueryAsync(query.headerQuery, parameters, reader => {
            var value = new GoodsReceiptValidateProcessDocumentsDataResponse {
                ObjectType     = reader.GetInt32(0),
                DocumentEntry  = reader.GetInt32(1),
                DocumentNumber = reader.GetInt32(2),
                Vendor         = new ExternalValue<string> { Id = reader.GetString(3), Name = reader[4]!.ToString() }
            };
            control.Add((value.ObjectType, value.DocumentEntry), value);
            return value;
        });

        parameters = new SqlParameter[docs.Length * 2];
        for (int i = 0; i < docs.Length; i++) {
            parameters[i * 2]     = new SqlParameter($"@ObjType{i}", SqlDbType.Int) { Value  = docs[i].Type };
            parameters[i * 2 + 1] = new SqlParameter($"@DocEntry{i}", SqlDbType.Int) { Value = docs[i].Entry };
        }

        await dbService.ExecuteReaderAsync(query.linesQuery, parameters, reader => {
            var value = control[(reader.GetInt32("ObjType"), reader.GetInt32("DocEntry"))];
            var item = new GoodsReceiptValidateProcessDocumentsDataLineResponse {
                LineNumber       = reader.GetInt32("LineNum"),
                DocumentQuantity = (int)reader.GetDecimal("OpenInvQty"),
                VisualLineNumber = reader.GetInt32("VisOrder")
            };
            ItemResponseHelper.PopulateItemResponse(reader, item);
            CustomFieldsHelper.ReadCustomFields(reader, customFields, item);
            value.Lines.Add(item);
        });
        return response;
    }

    private ((string headerQuery, string linesQuery) query, List<CustomField> customFields) BuildProcessDocumentsDataQuery(ObjectKey[] docs) {
        var sbDocs = new StringBuilder();
        for (int i = 0; i < docs.Length; i++) {
            if (i > 0) sbDocs.Append(" union ");
            sbDocs.Append($"select @ObjType{i} \"ObjType\", @DocEntry{i} \"DocEntry\" ");
        }

        string headerQuery = $"""
                              select T0."ObjType", T0."DocEntry", COALESCE(T1.DocNum, T2.DocNum, T3.DocNum) "DocNum", 
                              COALESCE(T1.CardCode, T2.CardCode, T3.CardCode) "CardCode", 
                              COALESCE(T1.CardName, T2.CardName, T3.CardName) "CardName"
                              from ({sbDocs}) T0
                              left outer join OPOR T1 on T1."DocEntry" = T0."DocEntry" and T1."ObjType" = T0."ObjType"
                              left outer join OPCH T2 on T2."DocEntry" = T0."DocEntry" and T2."ObjType" = T0."ObjType"
                              left outer join OPDN T3 on T3."DocEntry" = T0."DocEntry" and T3."ObjType" = T0."ObjType"
                              """;

        var queryBuilder = new StringBuilder();
        queryBuilder.Append($"""
                             select T0."ObjType" as "ObjType",
                                    T0."DocEntry" as "DocEntry",
                                    COALESCE(T1."LineNum", T2."LineNum", T3."LineNum") as "LineNum",
                                    COALESCE(T1."ItemCode", T2."ItemCode", T3."ItemCode") as "ItemCode",
                                    OITM."ItemName" as "ItemName",
                                    COALESCE(OITM."NumInBuy", 1) as "NumInBuy",
                                    OITM."BuyUnitMsr" as "BuyUnitMsr",
                                    COALESCE(OITM."PurPackUn", 1) as "PurPackUn",
                                    OITM."PurPackMsr" as "PurPackMsr",
                                    COALESCE(T1."OpenInvQty", Case When T5."isIns" = 'Y' Then T2."OpenInvQty" Else T2."InvQty" End, T3."InvQty", 0) as "OpenInvQty",
                                    COALESCE(T1."VisOrder", T2."VisOrder", T3."VisOrder")+1 as "VisOrder"
                             """);

        var customFields = GetCustomFields();
        CustomFieldsHelper.AppendCustomFieldsToQuery(queryBuilder, customFields);

        queryBuilder.Append($"""
                             from ({sbDocs}) T0
                                      left outer join POR1 T1 on T1.DocEntry = T0.DocEntry and T1.ObjType = T0.ObjType
                                      left outer join PCH1 T2 on T2.DocEntry = T0.DocEntry and T2.ObjType = T0.ObjType
                                      left outer join PDN1 T3 on T3.DocEntry = T0.DocEntry and T3.ObjType = T0.ObjType
                                      inner join OITM on OITM."itemCode" = COALESCE(T1."ItemCode", T2."ItemCode", T3."ItemCode")
                                      left outer join OPCH T5 on T5."DocEntry" = T2."DocEntry" and T5."ObjType" = T0."ObjType"
                             """);

        return ((headerQuery, queryBuilder.ToString()), customFields);
    }

    public async Task LoadGoodsReceiptItemData(Dictionary<string, List<GoodsReceiptCreationDataResponse>> data) {
        if (data.Count == 0)
            return;
        string[] items      = data.Select(v => v.Key).Distinct().ToArray();
        var parameters = new SqlParameter[items.Length];
        var sb = new StringBuilder(
            $"""
             select "ItemCode",
                    COALESCE("NumInBuy", 1) as "NumInBuy",
                    COALESCE("PurPackUn", 1) as "PurPackUn"
                    from OITM
                    where "ItemCode" in (
             """);
        for (int i = 0; i < items.Length; i++) {
            if (i > 0)
                sb.Append(" , ");
            sb.Append($"@ItemCode{i}");
            parameters[i] = new SqlParameter($"@ItemCode{i}", SqlDbType.NVarChar, 50) { Value = items[i] };
        }

        sb.Append(")");

        await dbService.ExecuteReaderAsync(sb.ToString(), parameters, reader => {
            var itemData = data[reader.GetString("ItemCode")];
            foreach (var value in itemData.Where(value => !value.UseBaseUnit)) {
                value.Quantity /= Convert.ToInt32(reader["NumInBuy"]);
                foreach (var source in value.Sources) {
                    source.Quantity /= Convert.ToInt32(reader["NumInBuy"]);
                }
            }
        });
    }
    
}