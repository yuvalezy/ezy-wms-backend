// using System;
// using System.Collections.Generic;
// using System.Data;
// using System.IO;
// using System.Linq;
// using System.Text;
// using System.Web.UI.WebControls;
// using Service.API.General.Models;
// using Service.API.Picking.Models;
// using Service.Shared.Company;
// using Service.Shared.Data;
// using Parameter = Service.Shared.Data.Parameter;
//
// namespace Service.API.Picking;
//
// public class PickingData {
//     public PickingDocument GetPicking(int id, string whsCode, int? type, int? entry, bool? availableBins, int? binEntry) {
//         var pick = GetPickings(new PickingParameters { ID = id, WhsCode = whsCode, Statuses = null }).FirstOrDefault();
//         if (pick == null)
//             return null;
//         GetPickingDetail(pick, type, entry, availableBins, binEntry);
//         return pick;
//     }
//
//     private void GetPickingDetail(PickingDocument pick, int? type, int? entry, bool? availableBins, int? binEntry) {
//         pick.Detail = [];
//         using var conn = Global.Connector;
//         conn.ExecuteReader(GetQuery("GetPickingDetails"),
//             [
//                 new Parameter("@AbsEntry", SqlDbType.Int, pick.Entry),
//                 new Parameter("@Type", SqlDbType.Int, type.HasValue ? type.Value : DBNull.Value),
//                 new Parameter("@Entry", SqlDbType.Int, entry.HasValue ? entry.Value : DBNull.Value)
//             ],
//             dr => pick.Detail.Add(PickingDocumentDetail.Read(dr)));
//         if (!type.HasValue || !entry.HasValue || pick.Detail.Count == 0)
//             return;
//         GetPickingDetailItems(pick.Entry, pick.Detail[0], availableBins, binEntry);
//     }
//
//     private void GetPickingDetailItems(int absEntry, PickingDocumentDetail detail, bool? availableBins, int? binEntry) {
//         detail.Items = [];
//         var       control = new Dictionary<string, PickingDocumentDetailItem>();
//         using var conn    = Global.Connector;
//         conn.ExecuteReader(GetQuery("GetPickingDetailItems"),
//             [
//                 new Parameter("@AbsEntry", SqlDbType.Int, absEntry),
//                 new Parameter("@Type", SqlDbType.Int, detail.Type),
//                 new Parameter("@Entry", SqlDbType.Int, detail.Entry)
//             ],
//             dr => {
//                 var value = PickingDocumentDetailItem.Read(dr);
//                 control.Add(value.ItemCode, value);
//                 detail.Items.Add(value);
//             });
//
//         if (availableBins == null || !availableBins.Value)
//             return;
//
//         conn.ExecuteReader(GetQuery("GetPickingDetailItemsAvailableBins"),
//             [
//                 new Parameter("@AbsEntry", SqlDbType.Int, absEntry),
//                 new Parameter("@Type", SqlDbType.Int, detail.Type),
//                 new Parameter("@Entry", SqlDbType.Int, detail.Entry),
//                 new Parameter("@BinEntry", SqlDbType.Int, !binEntry.HasValue ? DBNull.Value : binEntry.Value)
//             ],
//             dr => {
//                 string itemCode = (string)dr["ItemCode"];
//                 var    detail   = control[itemCode];
//                 detail.BinQuantities ??= [];
//                 var binLocationQuantity = new BinLocationQuantity {
//                     Entry    = (int)dr["BinEntry"],
//                     Code     = (string)dr["BinCode"],
//                     Quantity = Convert.ToInt32(dr["Quantity"])
//                 };
//                 detail.BinQuantities.Add(binLocationQuantity);
//             });
//
//         if (!binEntry.HasValue)
//             return;
//
//         detail.Items.RemoveAll(v => v.BinQuantities == null || v.OpenQuantity == 0);
//         detail.Items.ForEach(a => a.Available = a.BinQuantities.Sum(b => b.Quantity));
//     }
//
//
//     public IEnumerable<PickingDocument> GetPickings(PickingParameters parameters) {
//         List<PickingDocument> values = [];
//         var                   sb     = new StringBuilder(Environment.NewLine);
//         if (parameters.Statuses is { Length: > 0 }) {
//             sb.AppendLine("and PICKS.\"Status\" in (");
//             for (int i = 0; i < parameters.Statuses.Length; i++) {
//                 if (i > 0)
//                     sb.Append(", ");
//                 sb.Append($"'{(char)parameters.Statuses[i]}'");
//             }
//
//             sb.Append(") ");
//         }
//
//         var queryParams = new Parameters {
//             new Parameter("@WhsCode", SqlDbType.NVarChar, 8) { Value = parameters.WhsCode }
//         };
//
//         if (parameters.ID != null) {
//             queryParams.Add("@AbsEntry", SqlDbType.Int).Value = parameters.ID;
//             sb.AppendLine(" and PICKS.\"AbsEntry\" = @AbsEntry ");
//         }
//
//         if (parameters.Date != null) {
//             queryParams.Add("@Date", SqlDbType.DateTime).Value = parameters.Date;
//             sb.AppendLine(" and DATEDIFF(day,PICKS.\"U_StatusDate\",@Date) = 0 ");
//         }
//
//         using var conn   = Global.Connector;
//         string    query = string.Format(GetQuery("GetPickings"), sb);
//         conn.ExecuteReader(query, queryParams, dr => values.Add(PickingDocument.Read(dr)));
//         return values;
//     }
//
//
//     public static string GetQuery(string id) {
//         string resourceName = $"Service.API.Picking.Queries.{ConnectionController.DatabaseType}.{id}.sql";
//         var    assembly     = typeof(Queries).Assembly;
//         string resourcePath = resourceName;
//
//         using var stream = assembly.GetManifestResourceStream(resourcePath);
//         if (stream == null) {
//             throw new ArgumentException($"Specified resource not found: {resourceName}");
//         }
//
//         using var reader = new StreamReader(stream);
//         return reader.ReadToEnd();
//     }
//
//     public AddItemResponse AddItem(DataConnector conn, AddItemParameter parameters, int empID) {
//         conn.Execute(GetQuery("AddItem"), [
//             new Parameter("@AbsEntry", SqlDbType.Int, parameters.ID),
//             new Parameter("@PickEntry", SqlDbType.Int, parameters.PickEntry),
//             new Parameter("@Quantity", SqlDbType.Int, parameters.ID, parameters.Quantity),
//             new Parameter("@empID", SqlDbType.Int, empID),
//             new Parameter("@ItemCode", SqlDbType.NVarChar, 50, parameters.ItemCode),
//             new Parameter("@BinEntry", SqlDbType.Int, parameters.BinEntry),
//             new Parameter("@Unit", SqlDbType.SmallInt, parameters.Unit),
//         ]);
//         return AddItemResponse.OkResponse;
//     }
//
//     public int ValidateAddItem(DataConnector conn, AddItemParameter parameters, int empID, out int pickEntry) {
//         int returnValue = -1, returnPickEntry = -1;
//         conn.ExecuteReader(GetQuery("ValidateAddItemParameters"), [
//             new Parameter("@ID", SqlDbType.Int, parameters.ID),
//             new Parameter("@SourceType", SqlDbType.Int, parameters.Type),
//             new Parameter("@SourceEntry", SqlDbType.Int, parameters.Entry),
//             new Parameter("@ItemCode", SqlDbType.NVarChar, 50, parameters.ItemCode),
//             new Parameter("@empID", SqlDbType.Int, empID),
//             new Parameter("@Quantity", SqlDbType.Int, parameters.Quantity),
//             new Parameter("@BinEntry", SqlDbType.Int, parameters.BinEntry),
//             new Parameter("@Unit", SqlDbType.SmallInt, parameters.Unit),
//         ], dr => {
//             returnPickEntry = (int)dr[0];
//             returnValue     = (int)dr[1];
//         });
//         if (returnPickEntry == -1) {
//             returnValue = -6;
//         }
//
//         pickEntry = returnPickEntry;
//         return returnValue;
//     }
//
//     public AddItemResponse Process(int id, string whsCode) {
//         using var picking = new PickingUpdate(id);
//         picking.Execute();
//         return AddItemResponse.OkResponse;
//     }
// }