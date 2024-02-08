using System.Collections.Generic;

namespace Service.API.Models;

public class AlertColumn {

    public string           Name   { get; set; }
    public bool             Link   { get; set; }
    public List<AlertValue> Values { get; set; } = new();
    
    public AlertColumn() {
    }
    public AlertColumn(string name) => Name = name;

    public AlertColumn(string name, bool link) {
        Name = name;
        Link = link;
    }
}

public class AlertValue {

    public string Value     { get; set; }
    public string Object    { get; set; }
    public string ObjectKey { get; set; }
    public AlertValue() {
    }
    public AlertValue(string value) => Value = value;

    public AlertValue(string value, string @object, string objectKey) {
        Value     = value;
        Object    = @object;
        ObjectKey = objectKey;
    }
}