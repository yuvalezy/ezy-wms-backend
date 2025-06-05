using Core.Enums;

namespace Core.DTOs;

public class GoodsReceiptAddItemResponse : ResponseBase {
    public bool ClosedDocument { get; set; }
    
    public static GoodsReceiptAddItemResponse OkResponse => new() { 
        Status = ResponseStatus.Ok 
    };
}