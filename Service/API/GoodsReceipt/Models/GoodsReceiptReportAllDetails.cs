using System;
using Newtonsoft.Json;
using Service.API.General;
using Service.Models;

namespace Service.API.GoodsReceipt.Models;

public class GoodsReceiptReportAllDetails {
    public int    LineID       { get; set; }
    public string EmployeeName { get; set; }

    [JsonConverter(typeof(CustomDateTimeConverter))]
    public DateTime TimeStamp { get; set; }

    public int      Quantity { get; set; }
    public UnitType Unit     { get; set; }
}