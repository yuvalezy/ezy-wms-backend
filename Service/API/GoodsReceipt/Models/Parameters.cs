using System;
using CrystalDecisions.CrystalReports.Engine;
using Service.API.Models;

namespace Service.API.GoodsReceipt.Models;

public class CreateParameters {
    public string CardCode { get; set; }
    public string Name     { get; set; }
}

public class AddItemParameter {
    public int    ID       { get; set; }
    public string ItemCode { get; set; }
    public string BarCode  { get; set; }

    public void Validate() {
        if (ID <= 0)
            throw new ArgumentException("ID is a required parameter");
        //todo validate document is open / in process
        if (string.IsNullOrWhiteSpace(ItemCode))
            throw new ArgumentException("ItemCode is a required parameter");
        //todo validate item exists
        if (string.IsNullOrWhiteSpace(BarCode))
            throw new ArgumentException("BarCode is a required parameter");
        //todo validate barcode exists
            
    }
}

public class FilterParameters {
    internal string           WhsCode  { get; set; }
    public   DocumentStatus[] Statuses { get; set; }
    public   OrderBy?         OrderBy  { get; set; }
    public   int?             ID       { get; set; }
    public   bool             Desc     { get; set; }
}

public enum OrderBy {
    ID,
    Name,
    Date
}

public enum AddItemReturnValue {
    StoreInWarehouse = 1,
    Fulfillment = 2,
    Showroom = 3
}