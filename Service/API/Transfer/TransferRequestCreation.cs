// using System;
// using System.Runtime.InteropServices;
// using SAPbobsCOM;
// using Service.API.General.Models;
// using Service.API.Transfer.Models;
// using Service.Shared.Company;
// using GeneralData = Service.API.General.GeneralData;
//
// namespace Service.API.Transfer;
//
// public class TransferRequestCreation(TransferContent[] contents, EmployeeData employee) : IDisposable {
//     private StockTransfer transfer;
//     private Recordset     rs;
//
//     public int Entry  { get; private set; }
//     public int Number { get; private set; }
//
//     public void Execute() {
//         try {
//             if (!Global.TransactionMutex.WaitOne())
//                 return;
//             try {
//                 int transferSeries = GeneralData.GetSeries("1250000001");
//                 Global.ConnectCompany();
//                 ConnectionController.BeginTransaction();
//                 CreateTransferRequest(transferSeries);
//
//                 ConnectionController.Commit();
//             }
//             finally {
//                 Global.TransactionMutex.ReleaseMutex();
//             }
//         }
//         catch (Exception e) {
//             ConnectionController.TryRollback();
//             throw new Exception("Error generating GRPO: " + e.Message);
//         }
//     }
//
//     private void CreateTransferRequest(int transferSeries) {
//         transfer         = (StockTransfer)ConnectionController.Company.GetBusinessObject(BoObjectTypes.oInventoryTransferRequest);
//         transfer.DocDate = DateTime.Now;
//         transfer.Series  = transferSeries;
//
//         var lines = transfer.Lines;
//
//         foreach (var content in contents) {
//             if (!string.IsNullOrWhiteSpace(lines.ItemCode))
//                 lines.Add();
//             lines.ItemCode          = content.Code;
//             lines.FromWarehouseCode = employee.ShowroomWhsCode;
//             lines.WarehouseCode     = employee.WhsCode;
//             lines.Quantity          = content.Quantity;
//         }
//
//         if (transfer.Add() != 0) {
//             throw new Exception(ConnectionController.Company.GetLastErrorDescription());
//         }
//
//         Entry = int.Parse(ConnectionController.Company.GetNewObjectKey());
//         rs    = (Recordset)ConnectionController.Company.GetBusinessObject(BoObjectTypes.BoRecordset);
//         rs.DoQuery($"select \"DocNum\" from OWTQ where \"DocEntry\" = {Entry}");
//         Number = (int)rs.Fields.Item(0).Value;
//     }
//
//     public void Dispose() {
//         if (transfer != null) {
//             Marshal.ReleaseComObject(transfer);
//             transfer = null;
//         }
//
//         if (rs != null) {
//             Marshal.ReleaseComObject(rs);
//             rs = null;
//         }
//
//         GC.Collect();
//     }
// }