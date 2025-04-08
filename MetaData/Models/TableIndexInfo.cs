using System.Collections.Generic;

namespace MetaData.Models;

public class TableIndexInfo {
    public string       Name   { get; set; }
    public List<string> Fields { get; set; } = new();
    public bool         IsUnique { get; set; }
}