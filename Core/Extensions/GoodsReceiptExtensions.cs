using Core.DTOs.GoodsReceipt;
using Core.Entities;

namespace Core.Extensions;

public static class GoodsReceiptExtensions {
    public static GoodsReceiptReportAllDetailsResponse TotReportAllDetailsResponseDto(this GoodsReceiptLine line) {
        return new GoodsReceiptReportAllDetailsResponse {
            LineId            = line.Id,
            CreatedByUserName = line.CreatedByUser!.FullName,
            TimeStamp         = line.UpdatedAt ?? line.CreatedAt,
            Quantity          = line.Quantity,
            Unit              = line.Unit
        };
    }
    public static GoodsReceiptValidateProcessLineResponse ToValidateProcessLineDto(this GoodsReceiptValidateProcessDocumentsDataLineResponse docLine, decimal sourceQuantity, Guid baseLine) {
        var lineValue = new GoodsReceiptValidateProcessLineResponse {
            VisualLineNumber = docLine.VisualLineNumber,
            LineNumber       = docLine.LineNumber,
            ItemCode         = docLine.ItemCode,
            ItemName         = docLine.ItemName,
            Quantity         = sourceQuantity,
            BaseLine         = baseLine,
            DocumentQuantity = docLine.DocumentQuantity,
            NumInBuy         = docLine.NumInBuy,
            BuyUnitMsr       = docLine.BuyUnitMsr,
            PurPackUn        = docLine.PurPackUn,
            PurPackMsr       = docLine.PurPackMsr,
            CustomFields     = docLine.CustomFields,
            LineStatus       = GoodsReceiptValidateProcessLineStatus.OK
        };
        if (docLine.DocumentQuantity < sourceQuantity)
            lineValue.LineStatus = GoodsReceiptValidateProcessLineStatus.LessScan;
        else if (docLine.DocumentQuantity > sourceQuantity)
            lineValue.LineStatus = GoodsReceiptValidateProcessLineStatus.MoreScan;
        else if (sourceQuantity == 0)
            lineValue.LineStatus = GoodsReceiptValidateProcessLineStatus.NotReceived;
        return lineValue;
    }
}