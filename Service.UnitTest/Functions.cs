// using System;
// using LMService;
// using NUnit.Framework;
//
// namespace LM.Service.UnitTest {
//     [TestFixture]
//     public class Functions : Session {
//         [Test, Order(1)]
//         public void TestCheckWarehouse() {
//             const string sqlStr = "select \"WhsCode\" from OWHS where \"BinActivat\" = 'Y'";
//
//             string whsCode = Global.DataObject.GetValue<string>(sqlStr);
//             Assert.That(!string.IsNullOrWhiteSpace(whsCode));
//             (bool exists, bool bin) = DataCommands.CheckWarehouse(whsCode);
//             Assert.That(exists);
//             Assert.That(bin);
//         }
//
//         [Test, Order(2)]
//         public void TestCheckPackingStructure() {
//             const string sqlStr = @"select top 1 T0.""Code"", T1.""LineId"", T1.""U_ManQty""
// from ""@B1SLMCMPS"" T0
// left outer join ""@B1SLMCMPS1"" T1 on T1.""Code"" = T0.""Code"" and T1.""LineId"" = 1 ";
//             (string checkCode, int checkLineID, bool checkManual) = Global.DataObject.GetValue<string, int, bool>(sqlStr);
//             Assert.That(!string.IsNullOrWhiteSpace(checkCode));
//
//             (bool isValid, bool isValidLevel, bool isManual) = DataCommands.CheckPackingStructure(checkCode, checkLineID);
//             Assert.That(isValid);
//             Assert.That(isValidLevel);
//             Assert.That(checkManual == isManual);
//         }
//
//         [Test, Order(3)]
//         public void TestExistsContainer() {
//             const string sqlStr    = "select top 1 \"Code\" from B1SLMCMC";
//             string       checkCode = Global.DataObject.GetValue<string>(sqlStr);
//             Assert.That(DataCommands.ExistsContainer(checkCode));
//         }
//
//     }
// }