using System.Data;
using System.Text;
using Adapters.Windows.SBO.Services;
using Core.DTOs;
using Core.DTOs.GoodsReceipt;
using Core.Enums;
using Core.Models;
using Microsoft.Data.SqlClient;

namespace Adapters.Windows.SBO.Helpers;

public class SourceDocumentRetrieval(SboDatabaseService dbService) {
    public async Task<IEnumerable<GoodsReceiptAddItemSourceDocumentResponse>> GetPurchaseOrderSourceDocuments(
        string           itemCode,
        string           warehouse,
        UnitType         unit,
        GoodsReceiptType type,
        string?          cardCode,
        List<ObjectKey>  specificDocuments) {
        int[] entries = specificDocuments.Where(v => v.Type == 22).Select(v => v.Entry).ToArray();
        if (type is not (GoodsReceiptType.All or GoodsReceiptType.SpecificOrders) || entries.Length == 0) {
            return [];
        }

        var parameters = new List<SqlParameter> {
            new("@WhsCode", SqlDbType.NVarChar, 8) { Value   = warehouse },
            new("@ItemCode", SqlDbType.NVarChar, 50) { Value = itemCode },
            new("@Unit", SqlDbType.SmallInt) { Value         = (short)unit }
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

        return await dbService.QueryAsync(sb.ToString(), parameters.ToArray(), reader =>
            new GoodsReceiptAddItemSourceDocumentResponse {
                Type     = 22,
                Entry    = reader.GetInt32(1),
                LineNum  = reader.GetInt32(2),
                Quantity = (int)reader.GetDecimal(3)
            });
    }

    private async Task<IEnumerable<GoodsReceiptAddItemSourceDocumentResponse>> GetGoodsReceiptSourceDocuments(
        string           itemCode,
        string           warehouse,
        UnitType         unit,
        GoodsReceiptType type,
        List<ObjectKey>  specificDocuments) {
        int[] entries = specificDocuments.Where(v => v.Type == 20).Select(v => v.Entry).ToArray();
        if (type != GoodsReceiptType.SpecificReceipts || entries.Length == 0) {
            return [];
        }

        var parameters = new List<SqlParameter> {
            new("@WhsCode", SqlDbType.NVarChar, 8) { Value   = warehouse },
            new("@ItemCode", SqlDbType.NVarChar, 50) { Value = itemCode },
            new("@Unit", SqlDbType.SmallInt) { Value         = (short)unit }
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

        return await dbService.QueryAsync(sb.ToString(), parameters.ToArray(), reader =>
            new GoodsReceiptAddItemSourceDocumentResponse {
                Type     = 20,
                Entry    = reader.GetInt32(1),
                LineNum  = reader.GetInt32(2),
                Quantity = (int)reader.GetDecimal(3)
            });
    }

    private async Task<IEnumerable<GoodsReceiptAddItemSourceDocumentResponse>> GetAPInvoiceSourceDocuments(
        string           itemCode,
        string           warehouse,
        UnitType         unit,
        GoodsReceiptType type,
        string?          cardCode,
        List<ObjectKey>  specificDocuments) {
        int[] entries = specificDocuments.Where(v => v.Type == 18).Select(v => v.Entry).ToArray();

        var parameters = new List<SqlParameter> {
            new("@WhsCode", SqlDbType.NVarChar, 8) { Value   = warehouse },
            new("@ItemCode", SqlDbType.NVarChar, 50) { Value = itemCode },
            new("@Unit", SqlDbType.SmallInt) { Value         = (short)unit },
            new("@Type", SqlDbType.SmallInt) { Value         = (short)type }
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
        else if (entries.Length > 0) {
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
                  and Case When @Type <> 2 Then T0."OpenInvQty" Else T0."InvQty" end > 0
                  and (@Unit != 0 and T0."UseBaseUn" = 'N' or @Unit = 0 and T0."UseBaseUn" = 'Y')
                  order by T1."CreateDate", T1.CreateTS;
                  """);

        return await dbService.QueryAsync(sb.ToString(), parameters.ToArray(), reader =>
            new GoodsReceiptAddItemSourceDocumentResponse {
                Type     = 18,
                Entry    = reader.GetInt32(1),
                LineNum  = reader.GetInt32(2),
                Quantity = (int)reader.GetDecimal(3)
            });
    }

    public async Task<IEnumerable<GoodsReceiptAddItemSourceDocumentResponse>> GetAllSourceDocuments(
        string           itemCode,
        string           warehouse,
        UnitType         unit,
        GoodsReceiptType type,
        string?          cardCode,
        List<ObjectKey>  specificDocuments) {
        var response = new List<GoodsReceiptAddItemSourceDocumentResponse>();

        var purchaseOrders = await GetPurchaseOrderSourceDocuments(itemCode, warehouse, unit, type, cardCode, specificDocuments);
        response.AddRange(purchaseOrders);

        var goodsReceipts = await GetGoodsReceiptSourceDocuments(itemCode, warehouse, unit, type, specificDocuments);
        response.AddRange(goodsReceipts);

        var apInvoices = await GetAPInvoiceSourceDocuments(itemCode, warehouse, unit, type, cardCode, specificDocuments);
        response.AddRange(apInvoices);

        return response;
    }
}