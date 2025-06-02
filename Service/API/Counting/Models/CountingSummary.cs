using System;
using System.Collections.Generic;
using System.Data;

namespace Service.API.Counting.Models;

public class CountingSummary {
    public string                    Name  { get; set; }
    public List<CountingSummaryLine> Lines { get; set; } = [];
}

public class CountingSummaryLine(IDataReader dr) {
    public string BinCode  { get; set; } = (string)dr["BinCode"];
    public string ItemCode { get; set; } = (string)dr["ItemCode"];
    public string ItemName { get; set; } = dr["ItemName"].ToString();
    public int    Unit     { get; set; } = Convert.ToInt32(dr["Unit"]);
    public int    Dozen    { get; set; } = Convert.ToInt32(dr["Dozen"]);
    public int    Pack     { get; set; } = Convert.ToInt32(dr["Pack"]);
}