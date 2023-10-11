using System;
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
    public string CardCode { get; set; }

    public bool Validate(Data data) {
        if (ID <= 0)
            throw new ArgumentException(ErrorMessages.ID_is_a_required_parameter);
        if (string.IsNullOrWhiteSpace(ItemCode))
            throw new ArgumentException(ErrorMessages.ItemCode_is_a_required_parameter);
        if (string.IsNullOrWhiteSpace(BarCode))
            throw new ArgumentException(ErrorMessages.BarCode_is_a_required_parameter);
        int value = data.GoodsReceiptData.ValidateAddItem(ID, ItemCode, BarCode);
        switch (value) {
            case -1:
                throw new ArgumentException(string.Format(ErrorMessages.Item_Code__0__was_not_found_in_the_database, ItemCode));
            case -2:
                throw new ArgumentException(string.Format(ErrorMessages.BarCode__0__does_not_match_with_Item__1__BarCode, BarCode, ItemCode));
            case -3:
                throw new ArgumentException(string.Format(ErrorMessages.Transaction_with_ID__0__does_not_exists_in_the_system, ID));
            case -5:
                throw new ArgumentException(string.Format(ErrorMessages.Item__0___Bar_Code__1__is_not_a_purchase_item, ItemCode, BarCode));
            case -4:
                return false;
        }

        return true;
    }
}

public class FilterParameters {
    internal string WhsCode { get; set; }

    public DateTime?        Date            { get; set; }
    public string           BusinessPartner { get; set; }
    public string           Name            { get; set; }
    public DocumentStatus[] Status          { get; set; }
    public OrderBy?         OrderBy         { get; set; }
    public int?             ID              { get; set; }
    public int?             GRPO            { get; set; }
    public bool             Desc            { get; set; }
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