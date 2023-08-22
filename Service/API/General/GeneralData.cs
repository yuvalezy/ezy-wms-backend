using Service.Shared.Data;

namespace Service.API.General; 

public class GeneralData {
    public string GetEmployeeName(int employeeID) {
        string query = $"""
                        select COALESCE("firstName", 'NO_NAME') {QueryHelper.Concat} ' ' {QueryHelper.Concat} COALESCE("lastName", 'NO_LAST_NAME')
                        from OHEM where "empID" = {employeeID}
                        """;
        return Global.DataObject.GetValue<string>(query);
    }
}