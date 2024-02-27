using System;
using Service.API.General.Models;
using Service.Shared.Data;

namespace Service.API.Counting.Models;

public class UpdateLineParameter {
    public int    ID             { get; set; }
    public int    LineID         { get; set; }
    public string Comment        { get; set; }
    public int?   Quantity { get; set; }
    public int?   CloseReason    { get; set; }

    public UpdateLineReturnValue Validate(DataConnector conn, Data data) {
        if (ID <= 0)
            throw new ArgumentException(ErrorMessages.ID_is_a_required_parameter);
        if (LineID < 0)
            throw new ArgumentException(ErrorMessages.LineID_is_a_required_parameter);

        if (Quantity is < 1)
            throw new Exception("Quantity in Unit cannot be less then 1!");

        return (UpdateLineReturnValue)data.Counting.ValidateUpdateLine(conn, this);
    }
}