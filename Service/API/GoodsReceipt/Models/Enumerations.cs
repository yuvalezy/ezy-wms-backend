namespace Service.API.GoodsReceipt.Models;

public enum OrderBy {
    ID,
    Name,
    Date
}

public enum GoodsReceiptType {
    AutoConfirm = 'A',
    SpecificOrders = 'S'
}