using Service.API.General.Models;

namespace Service.API.Transfer.Models;

public class UpdateItemResponse(UpdateLineReturnValue? returnValue = null) {
    public bool                  ClosedDocument { get; set; }
    public string                ErrorMessage   { get; set; }
    public UpdateLineReturnValue ReturnValue    { get; set; } = returnValue ?? UpdateLineReturnValue.Ok;
    public bool                  Fulfillment    { get; set; }
    public bool                  Showroom       { get; set; }
    public bool                  Warehouse      { get; set; }
    public int                   Quantity       { get; set; }
}