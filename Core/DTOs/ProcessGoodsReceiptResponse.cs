using Core.Enums;

namespace Core.DTOs;

public class ProcessGoodsReceiptResponse : ResponseBase {
    public int? DocumentNumber { get; set; }
    public bool Success { get; set; }
}