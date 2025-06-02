using System.Collections.Generic;

namespace MetaData.Models;

public class TableInfo {
    public string               TableName        { get; set; }
    public string               TableDescription { get; set; }
    public string               TableType        { get; set; }
    public List<TableFieldInfo> Fields           { get; set; } = new();
    public List<TableIndexInfo> Indexes          { get; set; } = new();
    public string               ObjectType       { get; set; }
}