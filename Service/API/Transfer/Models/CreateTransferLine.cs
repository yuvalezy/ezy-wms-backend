using System;
using System.Collections.Generic;
using System.Data;
using Service.Shared;

namespace Service.API.Transfer.Models;

public class CreateTransferLine(DataRow dr) {
    public string                      ItemCode   { get; } = (string)dr["ItemCode"];
    public double                      Quantity   { get; } = Convert.ToDouble(dr["Quantity"]);
    public List<CreateTransferLineBin> SourceBins { get; } = new();
    public List<CreateTransferLineBin> TargetBins { get; } = new();
}

public class CreateTransferLineBin(DataRow dr) {
    public int    BinEntry { get; } = (int)dr["BinEntry"];
    public double Quantity { get; } = Convert.ToDouble(dr["Quantity"]);
}