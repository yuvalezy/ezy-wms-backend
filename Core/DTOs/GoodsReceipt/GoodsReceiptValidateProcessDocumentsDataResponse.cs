using Core.DTOs.Items;
using Core.Models;

namespace Core.DTOs.GoodsReceipt;

public class GoodsReceiptValidateProcessDocumentsDataResponse {
    public          int                                                               ObjectType     { get; set; }
    public          int                                                               DocumentEntry  { get; set; }
    public          int                                                               DocumentNumber { get; set; }
    public required ExternalValue<string>                                             Vendor         { get; set; }
    public          ICollection<GoodsReceiptValidateProcessDocumentsDataLineResponse> Lines          { get; set; } = [];
}

public class GoodsReceiptValidateProcessDocumentsDataLineResponse : ItemResponse {
    public          int    LineNumber          { get; set; }
    public          int    DocumentQuantity     { get; set; }
    public          int    VisualLineNumber { get; set; }
}