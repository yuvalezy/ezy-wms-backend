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

    public bool Validate(Data data) {
        if (ID <= 0)
            throw new ArgumentException("ID is a required parameter");
        if (string.IsNullOrWhiteSpace(ItemCode))
            throw new ArgumentException("ItemCode is a required parameter");
        if (string.IsNullOrWhiteSpace(BarCode))
            throw new ArgumentException("BarCode is a required parameter");
        int value = data.GoodsReceiptData.ValidateAddItem(ID, ItemCode, BarCode);
        switch (value) {
            case -1:
                throw new ArgumentException($"Item Code {ItemCode} was not found in the database");
            case -2:
                throw new ArgumentException($"The BarCode {BarCode} does not match with Item {ItemCode} BarCode");
            case -3:
                throw new ArgumentException($"Transaction with ID {ID} does not exists in the system");
            case -4:
                return false;
        }

        return true;
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
    ClosedDocument   = -1,
    StoreInWarehouse = 1,
    Fulfillment      = 2,
    Showroom         = 3
}