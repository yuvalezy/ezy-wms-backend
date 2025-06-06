using Core.Models;

namespace Core.DTOs.GoodsReceipt;

public class GoodsReceiptValidateProcessDocumentsDataResponse {
    public          int                   ObjectType     { get; set; }
    public          int                   DocumentEntry  { get; set; }
    public          int                   DocumentNumber { get; set; }
    public required ExternalValue<string> Vendor         { get; set; }
}