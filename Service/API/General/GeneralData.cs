using System;
using System.Collections.Generic;
using System.Data;
using System.Text;
using Service.API.General.Models;
using Service.API.Models;
using Service.Shared.Data;
using Service.Shared.Utils;

namespace Service.API.General;

public class GeneralData {
    public EmployeeData GetEmployeeData(int employeeID) {
        string query = $"""
                        select COALESCE(T0."firstName", 'NO_NAME') {QueryHelper.Concat} ' ' {QueryHelper.Concat} COALESCE(T0."lastName", 'NO_LAST_NAME') "Name",
                        T1."WhsCode", T1."WhsName"
                        from OHEM T0
                        left outer join OWHS T1 on T1."WhsCode" = T0."U_LW_Branch"
                        where T0."empID" = @empID
                        """;
        (string name, string whsCode, string whsName) =
            Global.DataObject.GetValue<string, string, string>(query, new Parameter("@empID", SqlDbType.Int) { Value = employeeID });
        return new EmployeeData(name, whsCode, whsName);
    }

    public IEnumerable<BusinessPartner> GetVendors() {
        var    list  = new List<BusinessPartner>();
        string query = """select "CardCode", "CardName" from OCRD where "CardType" = 'S' and "U_LW_YUVAL08_ENABLE" = 'Y' order by 2""";
        Global.DataObject.ExecuteReader(query, dr => list.Add(new BusinessPartner((string)dr["CardCode"], dr["CardName"].ToString())));
        return list;
    }

    public bool ValidateVendor(string cardCode) {
        string query = """select 1 from OCRD where "CardCode" = @CardCode and "CardType" = 'S' and "U_LW_YUVAL08_ENABLE" = 'Y'""";
        return Global.DataObject.GetValue<bool>(query, new Parameter("@CardCode", SqlDbType.NVarChar, 50, cardCode));
    }

    public static int GetSeries(string objectCode) {
        string query = """
                       select top 1 T1."Series"
                       from OFPR T0
                                inner join NNM1 T1 on T1."ObjectCode" = @ObjectCode and T1."Indicator" = T0."Indicator"
                       where (T1."LastNum" is null or T1."LastNum" >= "NextNumber")
                       and T0."F_RefDate" <= @Date and T0."T_RefDate" >= @Date
                       """;
        return Global.DataObject.GetValue<int>(query, new Parameters {
            new Parameter("@ObjectCode", SqlDbType.NVarChar, 50, objectCode),
            new Parameter("@Date", SqlDbType.DateTime, DateTime.Now)
        });
    }

    public IEnumerable<Item> ScanItemBarCode(string scanCode) {
        var list = new List<Item>();
        string query =
            """
            SELECT T0."ItemCode", T2."Father", T1."U_LW_BOX_NUM" "BoxNumber"
            FROM OBCD T0
                     INNER JOIN OITM T1 ON T0."ItemCode" = T1."ItemCode"
            left outer join ITT1 T2 on T2."Code" = T0."ItemCode"
            WHERE T0."BcdCode" = @ScanCode
            """;
        Global.DataObject.ExecuteReader(query,
            new Parameter("@ScanCode", SqlDbType.NVarChar, 50) { Value = scanCode },
            dr => {
                var item = new Item((string)dr["ItemCode"]);
                if (dr["Father"] != DBNull.Value)
                    item.Father = (string)dr["Father"];
                if (dr["BoxNumber"] != DBNull.Value)
                    item.BoxNumber = (int)dr["BoxNumber"];
                list.Add(item);
            });
        return list;
    }

    public List<string> AlertUsers {
        get {
            var          list  = new List<string>();
            const string query = "select USER_CODE from OUSR where U_LW_WMS_ALERTS = 'Y'";
            Global.DataObject.ExecuteReader(query, dr => list.Add(dr.GetString(0)));
            return list;
        }
    }

    public IEnumerable<ItemCheckResponse> ItemCheck(string scanItemCode, string scanBarCode) {
        var response = new List<ItemCheckResponse>();
        if (string.IsNullOrWhiteSpace(scanItemCode) && string.IsNullOrWhiteSpace(scanBarCode))
            return response;

        if (!string.IsNullOrWhiteSpace(scanBarCode)) {
            const string query = """
                                 select T0."ItemCode", T1."ItemName", COALESCE(T1."PurPackUn", 1) "PurPackUn"
                                 from OBCD T0
                                 inner join OITM T1 on T1."ItemCode" = T0."ItemCode"
                                 where T0."BcdCode" = @ScanCode
                                 """;
            var parameter = new Parameter("@ScanCode", SqlDbType.NVarChar, 255, scanBarCode);
            var items     = Global.DataObject.GetDataTable(query, parameter);
            foreach (DataRow row in items.Rows)
                AddItem((string)row["ItemCode"], row["ItemName"].ToString(), Convert.ToInt32(row["PurPackUn"]));
        }
        else {
            const string query     = "select \"ItemCode\", \"ItemName\", COALESCE(\"PurPackUn\", 1) \"PurPackUn\" from OITM where \"ItemCode\" = @ItemCode";
            var          parameter = new Parameter("@ItemCode", SqlDbType.NVarChar, 50, scanItemCode);
            Global.DataObject.ExecuteReader(query, parameter, dr =>
                AddItem((string)dr["ItemCode"], dr["ItemName"].ToString(), Convert.ToInt32(dr["PurPackUn"])));
        }

        void AddItem(string itemCode, string itemName, int PurPackUn) {
            var responseValue = new ItemCheckResponse {
                ItemCode = itemCode,
                ItemName = itemName,
                PurPackUn = PurPackUn
            };
            const string query = """select "BcdCode" from OBCD where "ItemCode" = @ItemCode""";
            Global.DataObject.ExecuteReader(query, new Parameter("@ItemCode", SqlDbType.NVarChar, 50, itemCode),
                dr => responseValue.Barcodes.Add((string)dr[0]));
            response.Add(responseValue);
        }

        return response;
    }
}