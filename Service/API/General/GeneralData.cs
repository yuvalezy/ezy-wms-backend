using System.Collections.Generic;
using System.Data;
using Service.API.Models;
using Service.Shared.Data;

namespace Service.API.General;

public class GeneralData {
    public string GetEmployeeName(int employeeID) {
        string query = $"""
                        select COALESCE("firstName", 'NO_NAME') {QueryHelper.Concat} ' ' {QueryHelper.Concat} COALESCE("lastName", 'NO_LAST_NAME')
                        from OHEM where "empID" = @empID
                        """;
        return Global.DataObject.GetValue<string>(query, new Parameter("@empID", SqlDbType.Int) { Value = employeeID });
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
}