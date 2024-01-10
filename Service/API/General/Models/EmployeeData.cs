namespace Service.API.General.Models;

public class EmployeeData {
    public string Name      { get;  }
    public string WhsCode   { get;  }
    public string WhsName   { get;  }
    public bool   EnableBin { get;  }

    public EmployeeData(string name, string whsCode, string whsName, bool enableBin) {
        Name           = name;
        WhsCode        = whsCode;
        WhsName        = whsName;
        EnableBin = enableBin;
    }
}