using System;
using Service.API.General;

namespace Service.API.Counting.Models;

public class AddItemParameter : AddItemParameterBase {
    public int Quantity { get; set; }
    public bool Validate(Data data, int empID) {
        if (ID <= 0)
            throw new ArgumentException(ErrorMessages.ID_is_a_required_parameter);
        if (Quantity <= 0)
            throw new ArgumentException(ErrorMessages.Quantity_is_a_required_parameter);
        //todo validate Bin Entry only if current session warehouse managed bin location
        // if (BinEntry <= 0)
        //     throw new ArgumentException(ErrorMessages.Bin_is_a_required_parameter);
            
        if (string.IsNullOrWhiteSpace(ItemCode))
            throw new ArgumentException(ErrorMessages.ItemCode_is_a_required_parameter);
        if (string.IsNullOrWhiteSpace(BarCode))
            throw new ArgumentException(ErrorMessages.BarCode_is_a_required_parameter);
        var value = (AddItemReturnValueType)data.Counting.ValidateAddItem(this, empID);
        return value.Value(this);
    }

}