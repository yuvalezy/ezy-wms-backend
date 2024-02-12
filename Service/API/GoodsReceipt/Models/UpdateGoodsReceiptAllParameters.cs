using System.Collections.Generic;

namespace Service.API.GoodsReceipt.Models;

public class UpdateGoodsReceiptAllParameters {
    public int                  ID              { get; set; }
    public List<int>            RemoveRows      { get; set; }
    public Dictionary<int, int> QuantityChanges { get; set; }
}