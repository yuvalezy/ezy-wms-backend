using System;
using System.Collections.Generic;
using System.Data;
using Service.API.General.Models;
using Service.API.Models;
using Service.Shared.Data;

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
}