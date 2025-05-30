using System;
using Newtonsoft.Json;
using Service.API.General;
using Service.Shared.Data;

namespace Service.API.Picking.Models;

public class AddItemParameter : AddItemParameterBase {
    public int Type     { get; set; }
    public int Entry    { get; set; }
    public int Quantity { get; set; }
    public int BinEntry { get; set; }

    [JsonIgnore] internal int PickEntry { get; set; }

    public bool Validate(DataConnector conn, Data data, int empID) {
        if (ID <= 0)
            throw new ArgumentException(ErrorMessages.ID_is_a_required_parameter);
        if (string.IsNullOrWhiteSpace(ItemCode))
            throw new ArgumentException(ErrorMessages.ItemCode_is_a_required_parameter);
        if (!Unit.HasValue) 
            throw new ArgumentException(ErrorMessages.UnitType_is_a_required_parameter);
        var value = (AddItemReturnValueType)data.Picking.ValidateAddItem(conn, this, empID, out int pickEntry);
        PickEntry = pickEntry;
        return value.Value(this);
    }
}