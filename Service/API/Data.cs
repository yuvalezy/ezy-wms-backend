// using System;
// using Service.API.Counting;
// using Service.API.General;
// using Service.API.GoodsReceipt;
// using Service.API.Picking;
// using Service.API.Transfer;
// using Service.Shared.Utils;
//
// namespace Service.API;
//
// public class Data {
//     public CountingData     Counting     { get; } = new();
//     public GoodsReceiptData GoodsReceipt { get; } = new();
//     public GeneralData      General      { get; } = new();
//     public PickingData      Picking      { get; } = new();
//     public TransferData     Transfer     { get; } = new();
//
//     public const string ValidateAccessFailedMessage = "Wrong supervisor password!";
//
//     public static bool ValidateAccess(string loginString, out int empID, out bool isValidBranch) {
//         empID = -1;
//         string    sqlStr     = $"select empID, (select WhsCode from OWHS where WhsCode = OHEM.U_LW_Branch) Branch from OHEM where U_LW_Login = '{loginString.ToQuery()}'";
//         using var conn = Global.Connector;
//         (empID, string branch) = conn.GetValue<int, string>(sqlStr);
//
//         isValidBranch = !string.IsNullOrWhiteSpace(branch);
//         if (!isValidBranch)
//             return false;
//
//         if (empID > 0)
//             Global.LoadAuthorization(empID);
//         return empID > 0;
//     }
// }