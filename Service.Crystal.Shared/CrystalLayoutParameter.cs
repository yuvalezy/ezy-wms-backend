using System.Collections.Generic;

namespace Service.Crystal.Shared; 

public class CrystalLayoutParameters : List<CrystalLayoutParameter> {
    public CrystalLayoutParameter Add(int id, string name, string type) {
        var retVal = new CrystalLayoutParameter(id, name, type);
        base.Add(retVal);
        return retVal;
    }
}

public class CrystalLayoutParameter {
    public CrystalLayoutParameter(int id, string name, string type) {
        ID     = id;
        Name   = name;
        Type   = type;
        Values = new CrystalLayoutParameterValues();
    }
    public int                          ID   { get; set; }
    public string                       Name { get; set; }
    public string                       Type { get; set; }
    public CrystalLayoutParameterValues Values;
}

public class CrystalLayoutParameterValues : List<CrystalLayoutParameterValue> {
    public CrystalLayoutParameterValue Add(string value, string description) {
        var retVal = new CrystalLayoutParameterValue(value, description);
        base.Add(retVal);
        return retVal;
    }
}

public class CrystalLayoutParameterValue {
    public CrystalLayoutParameterValue(string value, string description) {
        Value       = value;
        Description = description;
    }

    public string Value       { get; set; }
    public string Description { get; set; }
}