using System;
using Newtonsoft.Json;
using Service.Models;

namespace Service.API.Transfer.Models;

public class TransferContentTargetItemDetail {
    public int    LineID       { get; set; }
    public string EmployeeName { get; set; }

    [JsonConverter(typeof(CustomDateTimeConverter))]
    public DateTime TimeStamp { get; set; }

    public int Quantity { get; set; }
}