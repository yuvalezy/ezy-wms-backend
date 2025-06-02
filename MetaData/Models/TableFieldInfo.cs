using System.Collections.Generic;

namespace MetaData.Models;

public class TableFieldInfo {
    public string                     Name         { get; set; }
    public string                     Description  { get; set; }
    public string                     Type         { get; set; }
    public string                     EditType     { get; set; }
    public int                        Size         { get; set; }
    public string                     DefaultValue { get; set; }
    public bool                       IsMandatory  { get; set; }
    public Dictionary<string, string> ValidValues  { get; set; } = new();
}