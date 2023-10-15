namespace Service.API.GoodsReceipt.Models;

public class AddItemResponse {
    public int?               LineID { get; }
    public AddItemReturnValue Value  { get; }

    public AddItemResponse(AddItemReturnValue value, int? lineID = null) {
        Value  = value;
        LineID = lineID;
    }
}
public enum AddItemReturnValue {
    ClosedDocument   = -1,
    StoreInWarehouse = 1,
    Fulfillment      = 2,
    Showroom         = 3
}

