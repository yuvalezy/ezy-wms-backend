using System.Collections.Generic;

namespace Service.API.Counting.Models;

public class CountingSummary {
    public string                    Name  { get; set; }
    public List<CountingSummaryLine> Lines { get; set; } = [];
}

public class CountingSummaryLine(string binCode, string itemCode, string itemName, double quantity) {
    public string BinCode  { get; set; } = binCode;
    public string ItemCode { get; set; } = itemCode;
    public string ItemName { get; set; } = itemName;
    public double Quantity { get; set; } = quantity;
}