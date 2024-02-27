using System;
using Service.API.General;
using Service.Shared.Data;

namespace Service.API.GoodsReceipt.Models;

public class AddItemParameter : AddItemParameterBase {
    public string CardCode { get; set; }

    public bool Validate(DataConnector conn, Data data, int empID) {
        if (ID <= 0)
            throw new ArgumentException(ErrorMessages.ID_is_a_required_parameter);
        if (string.IsNullOrWhiteSpace(ItemCode))
            throw new ArgumentException(ErrorMessages.ItemCode_is_a_required_parameter);
        if (string.IsNullOrWhiteSpace(BarCode))
            throw new ArgumentException(ErrorMessages.BarCode_is_a_required_parameter);
        var value = (AddItemReturnValueType)data.GoodsReceipt.ValidateAddItem(conn, ID, ItemCode, BarCode, empID);
        return value.Value(this);
    }
}