using Core.DTOs.GoodsReceipt;
using Core.DTOs.Items;
using Core.Entities;
using Core.Enums;
using Core.Models;

namespace Core.Interfaces;

public interface IGoodsReceiptLineItemProcessService {
    Task<ValidateGoodsReceiptAndItemResponse> ValidateGoodsReceiptAndItem(GoodsReceiptAddItemRequest request, Guid userId, string warehouse);

    Task<ProcessSourceDocumentsAllocationResponse> ProcessSourceDocumentsAllocation(
        string            itemCode,
        UnitType          unit,
        string            warehouse,
        GoodsReceipt      goodsReceipt,
        ItemCheckResponse item,
        List<ObjectKey>   specificDocuments,
        int               quantity     = 1,
        Guid?             updateLineId = null);

    Task<GoodsReceiptLine> CreateGoodsReceiptLine(
        GoodsReceiptAddItemRequest                      request,
        GoodsReceipt                                    goodsReceipt,
        List<GoodsReceiptAddItemSourceDocumentResponse> sourceDocuments,
        int                                             quantity,
        Guid                                            userId);

    void UpdateGoodsReceiptStatus(GoodsReceipt goodsReceipt);

    Task<(int Fulfillment, int Showroom)> ProcessTargetDocumentAllocation(
        GoodsReceiptAddItemRequest request,
        string                     warehouse,
        GoodsReceiptLine           line,
        int                        quantity,
        Guid                       userId);

    GoodsReceiptAddItemResponse BuildAddItemResponse(
        GoodsReceiptLine  line,
        ItemCheckResponse item,
        int               fulfillment,
        int               showroom,
        int               quantity);
}

public record ValidateGoodsReceiptAndItemResponse(GoodsReceiptAddItemResponse? ErrorResponse, GoodsReceipt? GoodsReceipt, ItemCheckResponse? Item, List<ObjectKey>? SpecificDocuments);

public record ProcessSourceDocumentsAllocationResponse(GoodsReceiptAddItemResponse? ErrorResponse, List<GoodsReceiptAddItemSourceDocumentResponse>? SourceDocuments, int CalculatedQuantity);