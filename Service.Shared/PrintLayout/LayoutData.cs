using System;
using System.Data;
using Service.Shared.Data;

namespace Service.Shared.PrintLayout; 

public class LayoutData {
    // Required for regular data reader
    // ReSharper disable once UnusedMember.Global
    public LayoutData() {
    }

    public LayoutData(DataRow dr) {
        ID     = Convert.ToInt32(dr["ID"]);
        FileID = Convert.ToInt32(dr["FileID"]);
        if (dr["FileName"] != DBNull.Value)
            FileName = (string)dr["FileName"];
        Variable = (string)dr["Variable"];
        MD5      = (string)dr["MD5"];
        Name     = (string)dr["Name"];
        Order    = (int)dr["Order"];
        if (dr["Query"] != DBNull.Value)
            Query = (string)dr["Query"];
    }

    [RecordsetReaderColumn] public int ID     { get; set; }
    [RecordsetReaderColumn] public int FileID { get; set; }

    [RecordsetReaderColumn(IgnoreIfEmpty = true)]
    public string FileName { get; set; }

    public                         string FullPath => $"{System.IO.Path.GetTempPath()}\\{MD5}_{FileName}";
    [RecordsetReaderColumn] public string Variable { get; set; }
    [RecordsetReaderColumn] public string MD5      { get; set; }
    [RecordsetReaderColumn] public string Name     { get; set; }
    [RecordsetReaderColumn] public int    Order    { get; set; }
    [RecordsetReaderColumn] public string Query    { get; set; }
}