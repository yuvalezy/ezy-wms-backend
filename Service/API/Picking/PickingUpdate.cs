// using System;
// using System.Collections.Generic;
// using System.Linq;
// using System.Runtime.InteropServices;
// using System.Text;
// using SAPbobsCOM;
// using Service.API.General;
// using Service.API.Picking.Models;
// using Service.Shared.Company;
// using Service.Shared.Utils;
//
// namespace Service.API.Picking;
//
// public class PickingUpdate(int id) : IDisposable {
//     private List<PickingValue> data;
//
//     private readonly Recordset rs = Shared.Utils.Shared.GetRecordset();
//
//     public void Execute() {
//         try {
//             if (!Global.TransactionMutex.WaitOne())
//                 return;
//             try {
//                 Global.ConnectCompany();
//                 PreparePickList();
//                 Process();
//             }
//             finally {
//                 Global.TransactionMutex.ReleaseMutex();
//             }
//         }
//         catch (Exception e) {
//             UpdatePickingStatus(PickingStatus.Open, e.Message);
//             if (ConnectionController.InTransaction)
//                 ConnectionController.Rollback();
//             throw;
//         }
//     }
//
//     private void Process() {
//         var pl = (PickLists)ConnectionController.Company.GetBusinessObject(BoObjectTypes.oPickLists);
//         try {
//             ConnectionController.BeginTransaction();
//             UpdatePickingStatus(PickingStatus.Processing);
//             LoadPickList(pl);
//             UpdatePickList(pl);
//             UpdatePickingStatus(PickingStatus.Finished);
//             ConnectionController.Commit();
//         }
//         finally {
//             Shared.Utils.Shared.ReleaseComObject(pl);
//             GC.Collect();
//         }
//     }
//
//     private void PreparePickList() {
//         var pl = (PickLists)ConnectionController.Company.GetBusinessObject(BoObjectTypes.oPickLists);
//         try {
//             if (!pl.GetByKey(id)) {
//                 throw new Exception($"Could not find Pick List ${id}");
//             }
//
//             if (pl.UserFields.Fields.Item("U_LW_YUVAL08_READY").Value.ToString() == "Y")
//                 return;
//
//             if (pl.Status == BoPickStatus.ps_Closed) {
//                 throw new Exception("Cannot process document if the Status is closed");
//             }
//
//             for (int i = 0; i < pl.Lines.Count; i++) {
//                 pl.Lines.SetCurrentLine(i);
//                 for (int j = 0; i < pl.Lines.BinAllocations.Count; i++) {
//                     pl.Lines.BinAllocations.SetCurrentLine(j);
//                     pl.Lines.BinAllocations.Quantity = 0;
//                 }
//             }
//
//             pl.UserFields.Fields.Item("U_LW_YUVAL08_READY").Value = "Y";
//             if (pl.UpdateReleasedAllocation() != 0) {
//                 throw new Exception(ConnectionController.Company.GetLastErrorDescription());
//             }
//         }
//         finally {
//             Shared.Utils.Shared.ReleaseComObject(pl);
//         }
//     }
//
//
//     private void LoadPickList(PickLists pl) {
//         string query = string.Format(PickingData.GetQuery("LoadPickValues"), id);
//         data = query.ExecuteQueryReader<PickingValue>();
//         var control = data.ToDictionary(v => v.PickEntry, v => v);
//
//         query = string.Format(PickingData.GetQuery("LoadPickValuesBins"), id);
//         var bins = query.ExecuteQueryReader<PickingValueBin>();
//         bins.ForEach(bin => control[bin.PickEntry].BinLocations.Add(bin));
//
//         if (!pl.GetByKey(id)) {
//             throw new Exception($"Could not find Pick List ${id}");
//         }
//
//         if (pl.Status == BoPickStatus.ps_Closed) {
//             throw new Exception("Cannot process document if the Status is closed");
//         }
//     }
//
//     private void UpdatePickList(PickLists pl) {
//         for (int i = 0; i < pl.Lines.Count; i++) {
//             pl.Lines.SetCurrentLine(i);
//             var value = data.FirstOrDefault(v => v.PickEntry == pl.Lines.LineNumber);
//             if (value == null) {
//                 continue;
//             }
//
//             pl.Lines.PickedQuantity = (double)value.Quantity / (value.Unit != UnitType.Unit ? value.NumInBuy : 1);
//
//             UpdatePickListBinLocations(pl.Lines.BinAllocations, value);
//         }
//
//         if (pl.Update() != 0) {
//             throw new Exception($"Could not update Pick List: {ConnectionController.Company.GetLastErrorDescription()}");
//         }
//     }
//
//     private void UpdatePickListBinLocations(DocumentLinesBinAllocations bins, PickingValue value) {
//         var control = new Dictionary<int, int>();
//         for (int i = 0; i < bins.Count; i++) {
//             if (bins.BinAbsEntry == 0)
//                 continue;
//             bins.SetCurrentLine(i);
//             control[bins.BinAbsEntry] = i;
//         }
//
//         value.BinLocations.ForEach(binValue => {
//             if (control.TryGetValue(binValue.BinEntry, out int index)) {
//                 bins.SetCurrentLine(index);
//             }
//             else {
//                 if (bins.Count == 1 && bins.BinAbsEntry != 0)
//                     bins.Add();
//                 bins.BinAbsEntry = binValue.BinEntry;
//                 control.Add(binValue.BinEntry, bins.Count - 1);
//             }
//
//             bins.Quantity = binValue.Quantity;
//         });
//     }
//
//     private void UpdatePickingStatus(PickingStatus status, string errorMessage = null) {
//         var sb = new StringBuilder($"update \"@LW_YUVAL08_PKL1\" set \"U_Status\" = '{(char)status}', ");
//         sb.Append($"\"U_ErrorMessage\" = {errorMessage.ToSaveQuery()} ");
//         sb.Append($"where \"U_AbsEntry\" = {id}");
//         rs.DoQuery(sb.ToString());
//     }
//
//     public void Dispose() => Shared.Utils.Shared.ReleaseComObject(rs);
// }