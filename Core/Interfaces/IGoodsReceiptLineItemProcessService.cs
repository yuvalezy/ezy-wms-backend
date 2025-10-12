using Core.DTOs.GoodsReceipt;
using Core.DTOs.Items;
using Core.Entities;
using Core.Enums;
using Core.Models;

namespace Core.Interfaces;

public interface IGoodsReceiptLineItemProcessService {
    Task<ValidateGoodsReceiptAndItemResponse> ValidateGoodsReceiptAndItem(GoodsReceiptAddItemRequest request, Guid userId, string warehouse);

    Task<ProcessSourceDocumentsAllocationResponse> ProcessSourceDocumentsAllocation(string itemCode,
        UnitType unit,
        string warehouse,
        GoodsReceipt goodsReceipt,
        ItemCheckResponse item,
        List<ObjectKey> specificDocuments,
        decimal quantity = 1,
        Guid? updateLineId = null, 
        bool applyFactor = true);

    Task<GoodsReceiptLine> CreateGoodsReceiptLine(
        GoodsReceiptAddItemRequest                      request,
        GoodsReceipt                                    goodsReceipt,
        List<GoodsReceiptAddItemSourceDocumentResponse> sourceDocuments,
        decimal                                         quantity,
        Guid                                            userId);

    void UpdateGoodsReceiptStatus(GoodsReceipt goodsReceipt);

    Task<(decimal Fulfillment, decimal Showroom)> ProcessTargetDocumentAllocation(
        GoodsReceiptAddItemRequest request,
        string                     warehouse,
        GoodsReceiptLine           line,
        decimal                    quantity,
        Guid                       userId);

    GoodsReceiptAddItemResponse BuildAddItemResponse(
        GoodsReceiptLine  line,
        ItemCheckResponse item,
        decimal           fulfillment,
        decimal           showroom,
        decimal           quantity);
}

public record ValidateGoodsReceiptAndItemResponse(GoodsReceiptAddItemResponse? ErrorResponse, GoodsReceipt? GoodsReceipt, ItemCheckResponse? Item, List<ObjectKey>? SpecificDocuments);

public record ProcessSourceDocumentsAllocationResponse(GoodsReceiptAddItemResponse? ErrorResponse, List<GoodsReceiptAddItemSourceDocumentResponse>? SourceDocuments, decimal CalculatedQuantity);