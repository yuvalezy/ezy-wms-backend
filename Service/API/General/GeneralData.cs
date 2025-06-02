// using System;
// using System.Collections.Generic;
// using System.Data;
// using System.Linq;
// using Service.API.General.Models;
// using Service.API.Models;
// using Service.Shared;
// using Service.Shared.Data;
// using Service.Shared.Utils;
//
// namespace Service.API.General;
//
// public class GeneralData {
//     public EmployeeData GetEmployeeData(int employeeID) {
//         string query = """
//                        select COALESCE(T0."firstName", 'NO_NAME') + ' ' + COALESCE(T0."lastName", 'NO_LAST_NAME') "Name",
//                               T1."WhsCode",
//                               T1."WhsName",
//                               T1."BinActivat",
//                               T2."WhsCode" "SWWhsCode",
//                               T2."WhsName" "SWWhsName"
//                        from OHEM T0
//                                 left outer join OWHS T1 on T1."WhsCode" = T0."U_LW_Branch"
//                                 left outer join OWHS T2 on T2."WhsCode" = T1."U_LW_SW_WHS"
//                        where T0."empID" = @empID
//                        """;
//         using var conn = Global.Connector;
//         (string name, string whsCode, string whsName, bool enableBin, string swWhsCode, string swWhsName) =
//             conn.GetValue<string, string, string, bool, string, string>(query, new Parameter("@empID", SqlDbType.Int) { Value = employeeID });
//         return new EmployeeData(name, whsCode, whsName, enableBin, swWhsCode, swWhsName);
//     }
//
//     public IEnumerable<BusinessPartner> GetVendors() {
//         var          list  = new List<BusinessPartner>();
//         const string query = """select "CardCode", "CardName" from OCRD where "CardType" = 'S' and "U_LW_YUVAL08_ENABLE" = 'Y' order by 2""";
//         using var    conn  = Global.Connector;
//         conn.ExecuteReader(query, dr => list.Add(new BusinessPartner((string)dr["CardCode"], dr["CardName"].ToString())));
//         return list;
//     }
//
//     public bool ValidateVendor(string cardCode) {
//         if (string.IsNullOrWhiteSpace(cardCode))
//             return true;
//         const string query = """select 1 from OCRD where "CardCode" = @CardCode and "CardType" = 'S' and "U_LW_YUVAL08_ENABLE" = 'Y'""";
//         using var    conn  = Global.Connector;
//         return conn.GetValue<bool>(query, new Parameter("@CardCode", SqlDbType.NVarChar, 50, cardCode));
//     }
//
//     public static int GetSeries(ObjectTypes objectType) => GetSeries(((int)objectType).ToString());
//
//     public static int GetSeries(string objectCode) {
//         string query =
//             """
//             select top 1 T1."Series"
//             from OFPR T0
//                      inner join NNM1 T1 on T1."ObjectCode" = @ObjectCode and T1."Indicator" = T0."Indicator"
//             where (T1."LastNum" is null or T1."LastNum" >= "NextNumber")
//             and T0."F_RefDate" <= @Date and T0."T_RefDate" >= @Date
//             """;
//         using var conn = Global.Connector;
//         return conn.GetValue<int>(query, [
//             new Parameter("@ObjectCode", SqlDbType.NVarChar, 50, objectCode),
//             new Parameter("@Date", SqlDbType.DateTime, DateTime.Now)
//         ]);
//     }
//
//     public IEnumerable<Item> ScanItemBarCode(string scanCode, bool item = false) {
//         var    list = new List<Item>();
//         string query;
//         if (!item) {
//             query = """
//                     SELECT T0."ItemCode", T1."ItemName", T2."Father", T1."U_LW_BOX_NUM" "BoxNumber"
//                     FROM OBCD T0
//                              INNER JOIN OITM T1 ON T0."ItemCode" = T1."ItemCode"
//                     left outer join ITT1 T2 on T2."Code" = T0."ItemCode"
//                     WHERE T0."BcdCode" = @ScanCode
//                     """;
//         }
//         else {
//             query = """
//                     SELECT T0."ItemCode", T1."ItemName", T2."Father", T1."U_LW_BOX_NUM" "BoxNumber"
//                     FROM OITM T1
//                              left outer JOIN OBCD T0 ON T0."ItemCode" = T1."ItemCode"
//                     left outer join ITT1 T2 on T2."Code" = T0."ItemCode"
//                     WHERE T1."ItemCode" = @ScanCode or T0."BcdCode" = @ScanCode
//                     """;
//         }
//
//         using var conn = Global.Connector;
//         conn.ExecuteReader(query,
//             new Parameter("@ScanCode", SqlDbType.NVarChar, 50) { Value = scanCode },
//             dr => {
//                 var item = new Item((string)dr["ItemCode"]);
//                 if (dr["ItemName"] != DBNull.Value)
//                     item.Name = (string)dr["ItemName"];
//                 if (dr["Father"] != DBNull.Value)
//                     item.Father = (string)dr["Father"];
//                 if (dr["BoxNumber"] != DBNull.Value)
//                     item.BoxNumber = (int)dr["BoxNumber"];
//                 list.Add(item);
//             });
//         return list;
//     }
//
//     public List<string> AlertUsers {
//         get {
//             var          list  = new List<string>();
//             const string query = "select USER_CODE from OUSR where U_LW_WMS_ALERTS = 'Y'";
//             using var    conn  = Global.Connector;
//             conn.ExecuteReader(query, dr => list.Add(dr.GetString(0)));
//             return list;
//         }
//     }
//
//     public IEnumerable<ItemCheckResponse> ItemCheck(string scanItemCode, string scanBarCode) {
//         var response = new List<ItemCheckResponse>();
//         if (string.IsNullOrWhiteSpace(scanItemCode) && string.IsNullOrWhiteSpace(scanBarCode))
//             return response;
//
//         using var conn = Global.Connector;
//         var       data = new List<ItemCheckResponse>();
//         if (!string.IsNullOrWhiteSpace(scanBarCode)) {
//             const string query =
//                 """
//                 select T0."ItemCode"
//                      , T1."ItemName"
//                      , T1."BuyUnitMsr"
//                      , COALESCE(T1."NumInBuy", 1)  "NumInBuy"
//                      , T1."PurPackMsr"
//                      , COALESCE(T1."PurPackUn", 1) "PurPackUn"
//                 from OBCD T0
//                          inner join OITM T1 on T1."ItemCode" = T0."ItemCode"
//                 where T0."BcdCode" = @ScanCode
//                 """;
//             var       parameter = new Parameter("@ScanCode", SqlDbType.NVarChar, 255, scanBarCode);
//             using var rows      = conn.GetDataTable(query, parameter);
//             data.AddRange(rows.AsEnumerable().Select(row => new ItemCheckResponse {
//                 ItemCode   = (string)row["ItemCode"],
//                 ItemName   = row["ItemName"].ToString(),
//                 NumInBuy   = Convert.ToInt32(row["NumInBuy"]),
//                 BuyUnitMsr = row["BuyUnitMsr"].ToString(),
//                 PurPackUn  = Convert.ToInt32(row["PurPackUn"]),
//                 PurPackMsr = row["PurPackMsr"].ToString(),
//             }));
//         }
//         else {
//             const string query =
//                 """select "ItemCode", "ItemName", "BuyUnitMsr" , COALESCE("NumInBuy", 1)  "NumInBuy", "PurPackMsr" , COALESCE("PurPackUn", 1) "PurPackUn" from OITM where "ItemCode" = @ItemCode""";
//
//             var parameter = new Parameter("@ItemCode", SqlDbType.NVarChar, 50, scanItemCode);
//             conn.ExecuteReader(query, parameter, dr => {
//                 var value = new ItemCheckResponse {
//                     ItemCode   = (string)dr["ItemCode"],
//                     ItemName   = dr["ItemName"].ToString(),
//                     NumInBuy   = Convert.ToInt32(dr["NumInBuy"]),
//                     BuyUnitMsr = dr["BuyUnitMsr"].ToString(),
//                     PurPackUn  = Convert.ToInt32(dr["PurPackUn"]),
//                     PurPackMsr = dr["PurPackMsr"].ToString(),
//                 };
//                 data.Add(value);
//             });
//         }
//
//         const string barcodeQuery = """select "BcdCode" from OBCD where "ItemCode" = @ItemCode""";
//         data.ForEach(value => {
//             conn.ExecuteReader(barcodeQuery, new Parameter("@ItemCode", SqlDbType.NVarChar, 50, value.ItemCode),
//                 dr => value.Barcodes.Add((string)dr[0]));
//             response.Add(value);
//         });
//
//         return response;
//     }
//
//     public BinLocation ScanBinLocation(string binCode) {
//         string query = $"select \"AbsEntry\", \"BinCode\" from OBIN where \"BinCode\" = '{binCode.ToQuery()}'";
//         (int absEntry, string code) = query.ExecuteQueryValue<int, string>();
//         if (!string.IsNullOrWhiteSpace(code)) {
//             return new() {
//                 Entry = absEntry,
//                 Code  = code,
//             };
//         }
//
//         return null;
//     }
//
//     public IEnumerable<ValueDescription<int>> GetCancelReasons(ReasonType type) {
//         string tableID = type switch {
//             ReasonType.Counting     => "OINC",
//             ReasonType.Transfer     => "TRANS",
//             ReasonType.GoodsReceipt => "GRPO",
//             _                       => throw new ArgumentOutOfRangeException(nameof(type), type, null)
//         };
//         var       values = new List<ValueDescription<int>>();
//         using var conn   = Global.Connector;
//         conn.ExecuteReader($"select \"Code\", \"Name\" from \"@LW_YUVAL08_CR\" where \"U_{tableID}\" = 'Y' order by 2", dr => {
//             var value = new ValueDescription<int>((int)dr["Code"], (string)dr["Name"]);
//             values.Add(value);
//         });
//         return values;
//     }
//
//     public IEnumerable<ItemStockResponse> ItemStock(string itemCode, string whsCode) {
//         string query =
//             """
//             select T1."BinCode", T0."OnHandQty"
//             from OIBQ T0
//                      inner join OBIN T1 on T1."AbsEntry" = T0."BinAbs"
//             where T0."ItemCode" = @ItemCode
//               and T0."WhsCode" = @WhsCode
//               and T0."OnHandQty" > 0
//             order by 1
//             """;
//         var       values = new List<ItemStockResponse>();
//         using var conn   = Global.Connector;
//         conn.ExecuteReader(query, [new Parameter("@ItemCode", SqlDbType.NVarChar, 50, itemCode), new Parameter("@WhsCode", SqlDbType.NVarChar, 8, whsCode)],
//             dr => { values.Add(new ItemStockResponse { BinCode = (string)dr["BinCode"], Quantity = Convert.ToInt32(dr["OnHandQty"]) }); });
//         return values;
//     }
//
//     public IEnumerable<BinContent> BinCheck(int binEntry) {
//         string query =
//             """
//             select T1."ItemCode", T2."ItemName", T1."OnHandQty" "OnHand", 
//             COALESCE(T2."NumInBuy", 1) "NumInBuy", T2."BuyUnitMsr",
//             COALESCE(T2."PurPackUn", 1) "PurPackUn", T2."PurPackMsr"
//             from OIBQ T1 
//             inner join OITM T2 on T2."ItemCode" = T1."ItemCode"
//             where T1."BinAbs" = @AbsEntry and T1."OnHandQty" <> 0
//             order by 1
//             """;
//         var       values = new List<BinContent>();
//         using var conn   = Global.Connector;
//         conn.ExecuteReader(query, [new Parameter("@AbsEntry", SqlDbType.Int, binEntry)],
//             dr => {
//                 values.Add(new BinContent {
//                     ItemCode   = (string)dr["ItemCode"],
//                     ItemName   = dr["ItemName"].ToString(),
//                     OnHand     = Convert.ToInt32(dr["OnHand"]),
//                     NumInBuy   = Convert.ToInt32(dr["NumInBuy"]),
//                     BuyUnitMsr = dr["BuyUnitMsr"].ToString(),
//                     PurPackUn  = Convert.ToInt32(dr["PurPackUn"]),
//                     PurPackMsr = dr["PurPackMsr"].ToString()
//                 });
//             });
//         return values;
//     }
//
//     public HomeInfo GetHomeInfo(EmployeeData data) {
//
//         const string query =
//             """
//             select (select Count(1) from OITW where "WhsCode" = @WhsCode and "OnHand" > 0)                                                 "ItemCheck",
//                    (select Count(1) from OBIN where "WhsCode" = @WhsCode)                                                                  "BinCheck",
//                    (select Count(1) from "@LW_YUVAL08_GRPO" where "U_WhsCode" = @WhsCode and "U_Status" in ('O', 'I') and "U_Type" <> 'R') "GoodsReceipt",
//                    (select Count(1) from "@LW_YUVAL08_GRPO" where "U_WhsCode" = @WhsCode and "U_Status" in ('O', 'I') and "U_Type" = 'R')  "ReceiptConfirmation",
//                    (select Count(distinct PICKS."AbsEntry")
//                     from OPKL PICKS
//                              inner join PKL1 T1 on T1."AbsEntry" = PICKS."AbsEntry"
//                              inner join OILM T2 on T2.TransType = T1.BaseObject and T2.DocEntry = T1.OrderEntry and T2.DocLineNum = T1.OrderLine
//                     where T2.LocCode = @WhsCode
//                       and PICKS."Status" in ('R', 'P', 'D'))                                                                               "Picking",
//                    (select Count(1) from "@LW_YUVAL08_OINC" where "U_WhsCode" = @WhsCode and "U_Status" in ('O', 'I'))                     "Counting",
//                    (select Count(1) from "@LW_YUVAL08_TRANS" where "U_WhsCode" = @WhsCode and "U_Status" in ('O', 'I'))                    "Transfers"
//             """;
//         using var conn = Global.Connector;
//         var       info = new HomeInfo();
//         conn.ExecuteReader(query, new Parameter("@WhsCode", SqlDbType.NVarChar, 50) { Value = data.WhsCode }, dr => {
//             info.ItemCheck           = Convert.ToInt32(dr["ItemCheck"]);
//             info.BinCheck            = Convert.ToInt32(dr["BinCheck"]);
//             info.GoodsReceipt        = Convert.ToInt32(dr["GoodsReceipt"]);
//             info.ReceiptConfirmation = Convert.ToInt32(dr["ReceiptConfirmation"]);
//             info.Picking             = Convert.ToInt32(dr["Picking"]);
//             info.Counting            = Convert.ToInt32(dr["Counting"]);
//             info.Transfers           = Convert.ToInt32(dr["Transfers"]);
//         });
//         return info;
//     }
// }