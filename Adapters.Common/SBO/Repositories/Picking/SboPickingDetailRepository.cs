using System.Data;
using System.Text;
using Adapters.Common.SBO.Services;
using Adapters.Common.Utils;
using Core.DTOs.PickList;
using Core.Interfaces;
using Core.Models.Settings;

namespace Adapters.Common.SBO.Repositories;

public class SboPickingDetailRepository(SboDatabaseService dbService, ISettings settings) {
    public async Task<IEnumerable<PickingDetailResponse>> GetPickingDetails(Dictionary<string, object> parameters) {
        var sb = new StringBuilder(
            """
            SELECT
                T0."BaseObject" AS "Type",
                T0."OrderEntry" AS "Entry",
                COALESCE(ORDR."DocNum", OINV."DocNum", OWTQ."DocNum") AS "DocNum",
                COALESCE(ORDR."DocDate", OINV."DocDate", OWTQ."DocDate") AS "DocDate",
                COALESCE(ORDR."CardCode", OINV."CardCode", OWTQ."CardCode") AS "CardCode",
                COALESCE(ORDR."CardName", OINV."CardName", OWTQ."CardName") AS "CardName",
                SUM(T0."RelQtty" + T0."PickQtty") AS "TotalItems",
                SUM(T0."RelQtty") AS "TotalOpenItems",
                T0."PickEntry"
            """);

        if (settings.CustomFields?["PickingDetails"] != null) {
            CustomFieldsHelper.AppendCustomFieldsToQuery(sb, settings.CustomFields["PickingDetails"]);
        }

        sb.Append(
            """
            FROM PKL1 T0
            LEFT JOIN ORDR ON ORDR."DocEntry" = T0."OrderEntry" AND ORDR."ObjType" = T0."BaseObject"
            LEFT JOIN OINV ON OINV."DocEntry" = T0."OrderEntry" AND OINV."ObjType" = T0."BaseObject"
            LEFT JOIN OWTQ ON OWTQ."DocEntry" = T0."OrderEntry" AND OWTQ."ObjType" = T0."BaseObject"
            WHERE T0."AbsEntry" = @AbsEntry
            """);

        if (parameters.ContainsKey("@Type")) {
            sb.Append(" AND T0.\"BaseObject\" = @Type");
        }

        if (parameters.ContainsKey("@Entry")) {
            sb.Append(" AND T0.\"OrderEntry\" = @Entry");
        }

        sb.Append("""
                   GROUP BY
                      T0."BaseObject",
                      T0."OrderEntry",
                      ORDR."DocNum", OINV."DocNum", OWTQ."DocNum",
                      ORDR."DocDate", OINV."DocDate", OWTQ."DocDate",
                      ORDR."CardCode", OINV."CardCode", OWTQ."CardCode",
                      ORDR."CardName", OINV."CardName", OWTQ."CardName",
                      T0."PickEntry"
                  """);

        if (settings.CustomFields?["PickingDetails"] != null) {
            CustomFieldsHelper.AppendCustomFieldsToGroupBy(sb, settings.CustomFields["PickingDetails"]);
        }

        sb.Append(" ORDER BY T0.\"BaseObject\", T0.\"OrderEntry\"");

        var sqlParams = SboPickingRepositoryHelpers.ConvertToSqlParameters(parameters);

        return await dbService.QueryAsync(sb.ToString(), sqlParams, reader =>
        {
            var detail = new PickingDetailResponse {
                Type = reader.GetInt32(0),
                Entry = reader.GetInt32(1),
                Number = reader.GetInt32(2),
                Date = reader.GetDateTime(3),
                CardCode = reader.IsDBNull(4) ? string.Empty : reader.GetString(4),
                CardName = reader.IsDBNull(5) ? string.Empty : reader.GetString(5),
                TotalItems = reader.GetDecimal(6),
                TotalOpenItems = reader.GetDecimal(7),
                PickEntry = reader.GetInt32(8),
            };

            if (settings.CustomFields?["PickingDetails"] != null) {
                CustomFieldsHelper.ReadCustomFields(reader, settings.CustomFields["PickingDetails"], detail);
            }

            return detail;
        });
    }

    public async Task<IEnumerable<PickingDetailItemResponse>> GetPickingDetailItems(Dictionary<string, object> parameters) {
        (string query, var customFields) = BuildPickingDetailItemsQuery();
        var sqlParams = SboPickingRepositoryHelpers.ConvertToSqlParameters(parameters);

        return await dbService.QueryAsync(query, sqlParams, reader =>
        {
            var item = new PickingDetailItemResponse {
                Quantity = reader.GetDecimal("Quantity"),
                Picked = reader.GetDecimal("Picked"),
                OpenQuantity = reader.GetDecimal("OpenQuantity")
            };

            ItemResponseHelper.PopulateItemResponse(reader, item);
            CustomFieldsHelper.ReadCustomFields(reader, customFields, item);
            return item;
        });
    }

    private (string query, CustomField[] customFields) BuildPickingDetailItemsQuery() {
        var queryBuilder = new StringBuilder();
        queryBuilder.Append("""
                            SELECT
                                COALESCE(T3."ItemCode", T4."ItemCode", T5."ItemCode") as "ItemCode",
                                OITM."ItemName" as "ItemName",
                                SUM(PKL1."RelQtty" + PKL1."PickQtty") AS "Quantity",
                                SUM(PKL1."PickQtty") AS "Picked",
                                SUM(PKL1."RelQtty") AS "OpenQuantity",
                                COALESCE(OITM."NumInBuy", 1) AS "NumInBuy",
                                OITM."BuyUnitMsr" as "BuyUnitMsr",
                                COALESCE(OITM."PurPackUn", 1) AS "PurPackUn",
                                OITM."PurPackMsr" as "PurPackMsr",
                                OITM."PurFactor1",
                                OITM."PurFactor2",
                                OITM."PurFactor3",
                                OITM."PurFactor4"
                            """);

        var customFields = CustomFieldsHelper.GetCustomFields(settings, "Items");
        CustomFieldsHelper.AppendCustomFieldsToQuery(queryBuilder, customFields);

        queryBuilder.Append("""
                            FROM PKL1
                            left outer join RDR1 T3 on T3."DocEntry" = PKL1."OrderEntry" and T3."LineNum" = PKL1."OrderLine" and T3."ObjType" = PKL1."BaseObject"
                            left outer join INV1 T4 on T4."DocEntry" = PKL1."OrderEntry" and T4."LineNum" = PKL1."OrderLine" and T4."ObjType" = PKL1."BaseObject"
                            left outer join WTQ1 T5 on T5."DocEntry" = PKL1."OrderEntry" and T5."LineNum" = PKL1."OrderLine" and T5."ObjType" = PKL1."BaseObject"
                            INNER JOIN OITM ON OITM."ItemCode" = COALESCE(T3."ItemCode", T4."ItemCode", T5."ItemCode")
                            WHERE PKL1."AbsEntry" = @AbsEntry AND PKL1."BaseObject" = @Type AND PKL1."OrderEntry" = @Entry
                            Group by T5."ItemCode", T4."ItemCode", T3."ItemCode", OITM."ItemName", OITM."NumInBuy", OITM."BuyUnitMsr", OITM."PurPackUn", OITM."PurPackMsr", OITM."PurFactor1", OITM."PurFactor2", OITM."PurFactor3", OITM."PurFactor4"
                            """);

        // Add custom fields to GROUP BY clause
        CustomFieldsHelper.AppendCustomFieldsToGroupBy(queryBuilder, customFields);

        queryBuilder.Append("""
                             ORDER BY
                                CASE WHEN SUM(PKL1."RelQtty") = 0 THEN 1 ELSE 0 END,
                                "ItemCode"
                            """);

        return (queryBuilder.ToString(), customFields);
    }
}
