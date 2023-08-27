using Service.API.General;
using Service.API.GoodsReceipt;

namespace Service.API;

public class Data {
    public GoodsReceiptData GoodsReceiptData { get; } = new();
    public GeneralData      GeneralData      { get; } = new();
}