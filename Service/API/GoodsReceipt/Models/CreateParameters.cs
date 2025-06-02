// using System;
// using System.Collections.Generic;
// using System.Data;
// using System.Linq;
// using System.Text;
// using Service.Shared.Data;
// using Service.Shared.Utils;
//
// namespace Service.API.GoodsReceipt.Models;
//
// public class CreateParameters {
//     public string                  CardCode  { get; set; }
//     public string                  Name      { get; set; }
//     public GoodsReceiptType?       Type      { get; set; }
//     public List<DocumentParameter> Documents { get; set; }
//
//     public Document Validate(Data data, int employeeID) {
//         // if (string.IsNullOrWhiteSpace(Name))
//         //     throw new ArgumentException("Name cannot be empty", nameof(Name));
//         switch (Type) {
//             case GoodsReceiptType.AutoConfirm:
//                 ValidateAutoConfirm(data);
//                 return null;
//             case GoodsReceiptType.SpecificOrders or GoodsReceiptType.SpecificReceipts:
//                 return ValidateSpecificOrders(employeeID, Type.Value);
//             default:
//                 throw new ArgumentException("Invalid type argument", nameof(Type));
//         }
//     }
//
//     private void ValidateAutoConfirm(Data data) {
//         //if (string.IsNullOrWhiteSpace(CardCode))
//         //throw new ArgumentException("Card Code cannot be empty", nameof(CardCode));
//
//         if (!data.General.ValidateVendor(CardCode))
//             throw new ArgumentException($"Card Code {CardCode} is not a valid vendor", nameof(CardCode));
//     }
//
//     private Document ValidateSpecificOrders(int employeeID, GoodsReceiptType type) {
//         if (Documents == null || Documents.Count == 0)
//             throw new ArgumentException("You must specify the documents argument", nameof(Documents));
//
//         string documents = Documents.Aggregate("",
//             (a, b) => a + a.UnionQuery() +
//                       $"""
//                        select {b.ObjectType} "ObjType",
//                        {b.DocumentNumber} "DocNum",
//                        (select top 1 "DocEntry" from O{QueryHelper.ObjectTable(b.ObjectType)} where "DocNum" = {b.DocumentNumber} order by "DocEntry" desc) "DocEntry"
//                        {QueryHelper.FromDummy}
//                        """);
//
//         var queryBuilder = new StringBuilder();
//         queryBuilder.AppendLine("declare @WhsCode nvarchar(8) = (select U_LW_Branch from OHEM where \"empID\" = @empID);");
//         queryBuilder.AppendLine("select X0.\"ObjType\", X0.\"DocNum\", X0.\"DocEntry\",");
//         queryBuilder.AppendLine("Case");
//         queryBuilder.AppendLine("    When COALESCE(X1.\"DocStatus\", X2.\"DocStatus\") is null Then 'E'");
//         queryBuilder.AppendLine($"    When X0.\"ObjType\" = 18 and X2.\"isIns\" = '{(type == GoodsReceiptType.SpecificOrders ? 'N' : 'Y')}' Then 'R'");
//         queryBuilder.AppendLine("    When Sum(COALESCE(X3.\"Quantity\", X4.\"Quantity\", 0)) = 0 Then 'W'");
//         queryBuilder.AppendLine("    When COALESCE(X1.\"DocStatus\", X2.\"DocStatus\") = 'O' Then 'O'");
//         queryBuilder.AppendLine("    Else 'C' End \"DocStatus\"");
//         queryBuilder.AppendLine($"from ({documents}) X0");
//         queryBuilder.AppendLine($"left outer join {(type == GoodsReceiptType.SpecificOrders ? "OPOR" : "OPDN")} X1 on X1.\"DocEntry\" = X0.\"DocEntry\" and X1.\"ObjType\" = X0.\"ObjType\"");
//         queryBuilder.AppendLine("left outer join OPCH X2 on X2.\"DocEntry\" = X0.\"DocEntry\" and X2.\"ObjType\" = X0.\"ObjType\"");
//         queryBuilder.AppendLine($"left outer join {(type == GoodsReceiptType.SpecificOrders ? "POR1" : "PDN1")} X3 on X3.\"DocEntry\" = X1.\"DocEntry\" and X3.\"WhsCode\" = @WhsCode");
//         queryBuilder.AppendLine("left outer join PCH1 X4 on X4.\"DocEntry\" = X2.\"DocEntry\" and X4.\"WhsCode\" = @WhsCode");
//         queryBuilder.AppendLine("group by X0.\"ObjType\", X0.\"DocNum\", X0.\"DocEntry\", X1.\"DocStatus\", X2.\"DocStatus\", X2.\"isIns\"");
//
//         Document  returnValue = null;
//         using var conn        = Global.Connector;
//         conn.ExecuteReader(queryBuilder.ToString(), new Parameter("@empID", SqlDbType.Int) { Value = employeeID }, dr => {
//             string docStatus      = (string)dr["DocStatus"];
//             int    objectType     = (int)dr["ObjType"];
//             int    documentNumber = (int)dr["DocNum"];
//             int    documentEntry  = dr["DocEntry"] != DBNull.Value ? (int)dr["DocEntry"] : -1;
//
//             Documents.First(v => v.ObjectType == objectType && v.DocumentNumber == documentNumber).DocumentEntry = documentEntry;
//             if (docStatus == "O")
//                 return;
//             returnValue = new Document {
//                 Error           = true,
//                 ErrorCode       = -1,
//                 ErrorParameters = [objectType, documentNumber, docStatus]
//             };
//         });
//         return returnValue;
//     }
// }
//
// public class DocumentParameter {
//     public int ObjectType     { get; set; }
//     public int DocumentNumber { get; set; }
//     public int DocumentEntry  { get; set; }
// }