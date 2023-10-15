namespace Service.API.General.Models;

public class EmployeeData {

    public string Name       { get; set; }
    public string WhsCode    { get; set; }
    public string WhsName    { get; set; }

    public EmployeeData(string name, string whsCode, string whsName) {
        Name         = name;
        WhsCode      = whsCode;
        WhsName = whsName;
    }
}