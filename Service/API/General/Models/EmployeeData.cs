namespace Service.API.General.Models;

public class EmployeeData(string name, string whsCode, string whsName, bool enableBin, string showroomWhsCode, string showroomWhsName) {
    public string Name            { get;  } = name;
    public string WhsCode         { get;  } = whsCode;
    public string WhsName         { get;  } = whsName;
    public bool   EnableBin       { get;  } = enableBin;
    public string ShowroomWhsCode { get; }  = showroomWhsCode;
    public string ShowroomWhsName { get; }  = showroomWhsName;
}