using System.Data;
using System.Text;
using Adapters.Common.SBO.Services;
using Core.DTOs.PickList;
using Core.Enums;
using Core.Interfaces;
using Microsoft.Data.SqlClient;

namespace Adapters.Common.SBO.Repositories;

public class SboPickingValidationRepository(SboDatabaseService dbService, ISettings settings) {
    public async Task<PickingValidationResult[]> ValidatePickingAddItem(PickListAddItemRequest request) {
        string? pickPackOnly = settings.Filters.PickPackOnly?.Query;

        var sb = new StringBuilder(
            """
            SELECT PKL1."PickEntry",
            CASE
                WHEN OPKL."Status" = 'C' THEN -6  -- Closed document
                WHEN T2."ItemCode" <> @ItemCode THEN -2  -- Wrong item
                WHEN PKL1."RelQtty" = 0 THEN -3  -- Already picked
                WHEN @Quantity > PKL1."RelQtty" THEN -4  -- Too much quantity
                ELSE 0  -- OK
            END AS "ValidationResult",
            PKL1."RelQtty" "OpenQuantity", COALESCE(T3."OnHandQty", 0) - COALESCE(T5."BinQty", 0) "OnHandQty",
            T6."OnHand"
            """);

        if (!string.IsNullOrWhiteSpace(pickPackOnly)) {
            sb.Append($", Case When {pickPackOnly} Then 1 Else 0 End \"PickPackOnly\" ");
        }

        sb.Append("""
                  FROM OPKL
                  INNER JOIN PKL1 ON PKL1."AbsEntry" = OPKL."AbsEntry"
                  inner join OILM T2 on T2."TransType" = PKL1."BaseObject" and T2.DocEntry = PKL1."OrderEntry" and T2."DocLineNum" = PKL1."OrderLine"
                  left outer join OIBQ T3 on T3."BinAbs" = @BinEntry and T3."ItemCode" = @ItemCode
                  left outer join (
                      select T2."BinAbs", T3."ItemCode", Sum(T2."PickQtty") "BinQty"
                      from PKL1 T0
                      inner join OPKL T1 on T1."AbsEntry" = T0."AbsEntry" and T1."Status" = 'P'
                      inner join PKL2 T2 on T2."AbsEntry" = T0."AbsEntry" and T2."PickEntry" = T0."PickEntry"
                      inner join OILM T3 on T3.TransType = T0.BaseObject and T3.DocEntry = T0.OrderEntry and T3.DocLineNum = T0.OrderLine
                      where T2."BinAbs" = @BinEntry
                      Group By T2."BinAbs", T3."ItemCode"
                  ) T5 on T5."BinAbs" = T3."BinAbs" and T5."ItemCode" = T2."ItemCode"
                  """);

        if (!string.IsNullOrWhiteSpace(pickPackOnly)) {
            sb.Append("left outer join OCRD on OCRD.\"CardCode\" = T2.\"BPCardCode\" ");
        }

        sb.Append("""
                  left outer join OITW T6 on T6."ItemCode" = T2."ItemCode" and T6."WhsCode" = T2."LocCode"
                  WHERE OPKL."AbsEntry" = @ID
                  AND PKL1."BaseObject" = @SourceType
                  AND PKL1."OrderEntry" = @SourceEntry
                  AND T2."ItemCode" = @ItemCode
                  order by 2 desc, 3 desc
                  """);

        string query = sb.ToString();

        var sqlParams = new[] {
            new SqlParameter("@ID", SqlDbType.Int) { Value = request.ID },
            new SqlParameter("@SourceType", SqlDbType.Int) { Value = request.Type },
            new SqlParameter("@SourceEntry", SqlDbType.Int) { Value = request.Entry },
            new SqlParameter("@ItemCode", SqlDbType.NVarChar, 50) { Value = request.ItemCode },
            new SqlParameter("@Quantity", SqlDbType.Decimal) { Precision = 18, Scale = 6, Value = request.Quantity },
            new SqlParameter("@BinEntry", SqlDbType.Int) { Value = request.BinEntry ?? -1 },
        };

        var result = await dbService.QueryAsync(query, sqlParams, reader =>
        {
            int returnValue = reader.GetInt32(1);
            if (!string.IsNullOrWhiteSpace(pickPackOnly) && Convert.ToBoolean(reader["PickPackOnly"]) && request.Unit != UnitType.Pack) {
                returnValue = -5;
            }

            return new PickingValidationResult {
                PickEntry = reader.IsDBNull(0) ? null : reader.GetInt32(0),
                ReturnValue = returnValue,
                OpenQuantity = reader.GetDecimal(2),
                BinOnHand = reader.GetDecimal(3),
                OnHand = reader.GetDecimal(4),
                ErrorMessage = returnValue switch {
                    -2 => "Wrong item code",
                    -3 => "Item already fully picked",
                    -4 => "Quantity exceeds open quantity",
                    -5 => "Customer is marked as pick pack only",
                    -6 => "Document is closed",
                    _ => null
                },
                IsValid = returnValue == 0,
            };
        });

        return result.ToArray();
    }

    public async Task<PickListClosureInfo> GetPickListClosureInfo(int absEntry) {
        // Query to get pick list closure information and any follow-up documents
        // This joins PKL1 (pick list lines) with:
        // - DLN1: Delivery note lines (ObjType 15)
        // - PCH1: Purchase invoice lines for returns (ObjType 18)
        // - WTR1: Inventory transfer lines (ObjType 67)
        // The joins use PickIdNo field which links delivery/return/transfer lines back to the pick list
        // OILM and OBTL are used to get bin location information from inventory transactions
        const string query =
        """
        select T0.PickEntry, T0.PickStatus,
        COALESCE(T1.DocEntry, T2.DocEntry, T3.DocEntry) DocEntry,
        COALESCE(T1.ObjType, T2.ObjType, T3.ObjType) ObjType,
        COALESCE(T1.LineNum, T2.LineNum, T3.ObjType) LineNum,
        COALESCE(T1.DocDate, T2.DocDate, T3.DocDate) DocDate,
        COALESCE(T1.ItemCode, T2.ItemCode, T3.ItemCode) ItemCode,
        COALESCE(T1.InvQty, T2.InvQty, T3.InvQty) InvQty,
        T11."BinAbs", T11."Quantity" "BinQty"
        from PKL1 T0
        left outer join DLN1 T1 on T1.BaseEntry = T0.OrderEntry and T1.BaseType = T0.BaseObject and T1.BaseLine = T0.OrderLine and T1.PickIdNo = T0.AbsEntry
        left outer join PCH1 T2 on T2.BaseEntry = T0.OrderEntry and T2.BaseType = T0.BaseObject and T2.BaseLine = T0.OrderLine and T2.PickIdNo = T0.AbsEntry
        left outer join WTR1 T3 on T3.BaseEntry = T0.OrderEntry and T3.BaseType = T0.BaseObject and T3.BaseLine = T0.OrderLine and T3.PickIdNo = T0.AbsEntry
        left outer join OILM T10 on T10.DocEntry = COALESCE(T1.DocEntry, T2.DocEntry, T3.DocEntry) and T10.TransType = COALESCE(T1.ObjType, T2.ObjType, T3.ObjType) and T10.DocLineNum = COALESCE(T1.LineNum, T2.LineNum, T3.ObjType) and T10.AccumType = 1 and T10.ActionType = 2
        left outer join OBTL T11 on T11.MessageID = T10.MessageID
        where T0."AbsEntry" = @AbsEntry
        """;

        using var dt = await dbService.GetDataTableAsync(query, [new SqlParameter("@AbsEntry", SqlDbType.Int) { Value = absEntry }]);
        if (dt.Rows.Count == 0) {
            throw new Exception("Pick list data not found");
        }

        // Check if pick list is closed (Status = 'C')
        bool isClosed = (string)dt.Rows[0]["PickStatus"] == "C";
        var info = new PickListClosureInfo {
            IsClosed = isClosed,
        };

        // Determine closure reason based on whether follow-up documents exist
        if (isClosed) {
            info.ClosureReason = dt.Rows[0]["DocEntry"] != DBNull.Value ? PickListClosureReasonType.FollowUpDocument : PickListClosureReasonType.Closed;
        }

        // Dictionary to group follow-up documents by PickEntry
        var control = new Dictionary<int, FollowUpDocumentInfo>();

        foreach (DataRow dr in dt.Rows) {
            // Skip rows without follow-up documents
            if (dr["DocEntry"] == DBNull.Value) continue;

            int pickEntry = (int)dr["PickEntry"];
            int docEntry = (int)dr["DocEntry"];
            int objType = Convert.ToInt32(dr["ObjType"]);
            int lineNum = (int)dr["LineNum"];
            var docDate = (DateTime)dr["DocDate"];
            string itemCode = (string)dr["ItemCode"];
            int invQty = Convert.ToInt32(dr["InvQty"]);
            int? binEntry = null;
            int? binQty = null;

            // Get bin information from OBTL if available
            if (dr["BinAbs"] != DBNull.Value) {
                binEntry = (int)dr["BinAbs"];
                binQty = Convert.ToInt32(dr["BinQty"]);
            }

            // Group items by PickEntry and document
            if (!control.TryGetValue(pickEntry, out var documentInfo)) {
                documentInfo = new FollowUpDocumentInfo {
                    PickEntry = pickEntry,
                    DocumentType = objType, // 15=Delivery, 18=Return, 67=Transfer
                    DocumentEntry = docEntry,
                    DocumentNumber = lineNum, // Using LineNum as DocumentNumber (should be DocNum)
                    DocumentDate = docDate,
                };

                control.Add(pickEntry, documentInfo);
                info.FollowUpDocuments.Add(documentInfo);
            }

            // Add item details to the follow-up document
            documentInfo.Items.Add(new FollowUpDocumentItem {
                ItemCode = itemCode,
                Quantity = binQty ?? invQty, // Use bin quantity if available, otherwise invoice quantity
                BinEntry = binEntry
            });
        }

        return info;
    }
}
