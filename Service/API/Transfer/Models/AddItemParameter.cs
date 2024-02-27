using System;
using Service.API.General;
using Service.Shared;
using Service.Shared.Data;

namespace Service.API.Transfer.Models;

public class AddItemParameter : AddItemParameterBase {
    public int          Quantity { get; set; }
    public SourceTarget Type     { get; set; }

    public bool Validate(DataConnector conn, Data data, int empID) {
        if (ID <= 0)
            throw new ArgumentException(ErrorMessages.ID_is_a_required_parameter);
        if (Quantity <= 0)
            throw new ArgumentException(ErrorMessages.Quantity_is_a_required_parameter);
        //todo validate Bin Entry only if current session warehouse managed bin location
        // if (BinEntry <= 0)
        //     throw new ArgumentException(ErrorMessages.Bin_is_a_required_parameter);

        if (string.IsNullOrWhiteSpace(ItemCode))
            throw new ArgumentException(ErrorMessages.ItemCode_is_a_required_parameter);
        if (Type == SourceTarget.Source && string.IsNullOrWhiteSpace(BarCode))
            throw new ArgumentException(ErrorMessages.BarCode_is_a_required_parameter);
        var value = (AddItemReturnValueType)data.Transfer.ValidateAddItem(conn, this, empID);
        return value.Value(this);
    }
}