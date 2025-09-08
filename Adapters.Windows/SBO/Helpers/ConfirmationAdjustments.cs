using System.Runtime.InteropServices;
using Adapters.Windows.SBO.Services;
using Core.DTOs.GoodsReceipt;
using Microsoft.Extensions.Logging;
using SAPbobsCOM;

namespace Adapters.Windows.SBO.Helpers;

public class ConfirmationAdjustments(
    ProcessConfirmationAdjustmentsParameters @params,
    int entrySeries,
    int exitSeries,
    SboCompany sboCompany,
    ILoggerFactory loggerFactory) : IDisposable {
    private readonly ILogger<ConfirmationAdjustments> logger = loggerFactory.CreateLogger<ConfirmationAdjustments>();
    private Documents? goodsIssue;
    private Documents? goodsReceipt;

    public async Task<ConfirmationAdjustmentsResponse> Execute() {
        logger.LogInformation("Starting confirmation adjustments for confirmation {Number} in warehouse {Warehouse}",
            @params.Number, @params.Warehouse);

        try {
            if (!sboCompany.TransactionMutex.WaitOne()) {
                logger.LogWarning("Unable to acquire transaction mutex for confirmation {Number}", @params.Number);
                return ConfirmationAdjustmentsResponse.Error("Unable to acquire transaction mutex");
            }

            try {
                sboCompany.ConnectCompany();
                sboCompany.Company!.StartTransaction();

                int? exitDocEntry = null;
                int? entryDocEntry = null;

                // Process negative items (Goods Issue)
                if (@params.NegativeItems.Count > 0) {
                    logger.LogInformation("Creating inventory goods issue for {Count} negative items", @params.NegativeItems.Count);
                    exitDocEntry = CreateGoodsIssue();
                    logger.LogInformation("Successfully created inventory goods issue with DocEntry {DocEntry}", exitDocEntry);
                }

                // Process positive items (Goods Receipt)
                if (@params.PositiveItems.Count > 0) {
                    logger.LogInformation("Creating inventory goods receipt for {Count} positive items", @params.PositiveItems.Count);
                    entryDocEntry = CreateGoodsReceipt();
                    logger.LogInformation("Successfully created inventory goods receipt with DocEntry {DocEntry}", entryDocEntry);
                }

                // Commit transaction
                if (sboCompany.Company.InTransaction) {
                    sboCompany.Company.EndTransaction(BoWfTransOpt.wf_Commit);
                }

                logger.LogInformation("Successfully completed all confirmation adjustments for confirmation {Number}", @params.Number);
                return ConfirmationAdjustmentsResponse.Ok(entryDocEntry, exitDocEntry);
            }
            finally {
                sboCompany.TransactionMutex.ReleaseMutex();
            }
        }
        catch (Exception ex) {
            logger.LogError(ex, "Error processing confirmation adjustments for confirmation {Number}: {Error}", @params.Number, ex.Message);

            // Rollback transaction on error
            if (sboCompany.Company?.InTransaction == true) {
                sboCompany.Company.EndTransaction(BoWfTransOpt.wf_RollBack);
            }

            return ConfirmationAdjustmentsResponse.Error($"Error processing confirmation adjustments: {ex.Message}");
        }
    }

    private int CreateGoodsIssue() {
        goodsIssue = (Documents)sboCompany.Company!.GetBusinessObject(BoObjectTypes.oInventoryGenExit);

        goodsIssue.Series = exitSeries;
        goodsIssue.DocDate = DateTime.Now;
        goodsIssue.DocDueDate = DateTime.Now;
        goodsIssue.Comments = $"Ajuste de inventario para confirmación de WMS {@params.Number} - Salida de mercancías";

        foreach (var (itemCode, quantity) in @params.NegativeItems) {
            if (!string.IsNullOrWhiteSpace(goodsIssue.Lines.ItemCode)) {
                goodsIssue.Lines.Add();
            }

            goodsIssue.Lines.ItemCode = itemCode;
            goodsIssue.Lines.Quantity = Math.Abs((double)quantity);
            goodsIssue.Lines.WarehouseCode = @params.Warehouse;
            goodsIssue.Lines.UseBaseUnits = BoYesNoEnum.tYES;

            // Add bin allocation if bin locations are enabled
            if (@params is { EnableBinLocation: true, DefaultBinLocation: not null }) {
                goodsIssue.Lines.BinAllocations.BinAbsEntry = @params.DefaultBinLocation.Value;
                goodsIssue.Lines.BinAllocations.Quantity = Math.Abs((double)quantity);
                goodsIssue.Lines.BinAllocations.AllowNegativeQuantity = BoYesNoEnum.tNO;
                goodsIssue.Lines.BinAllocations.SerialAndBatchNumbersBaseLine = -1;
                // goodsIssue.Lines.BinAllocations.BaseLineNumber = goodsIssue.Lines.LineNum;
            }

            logger.LogDebug("Added goods issue line for item {ItemCode} with quantity {Quantity}", itemCode, Math.Abs(quantity));
        }

        int returnValue = goodsIssue.Add();
        if (returnValue != 0) {
            string errorDescription = sboCompany.Company.GetLastErrorDescription();
            logger.LogError("Failed to create goods issue. SAP Error: {ErrorDescription}", errorDescription);
            throw new Exception($"Failed to create goods issue: {errorDescription}");
        }

        return int.Parse(sboCompany.Company.GetNewObjectKey());
    }

    private int CreateGoodsReceipt() {
        goodsReceipt = (Documents)sboCompany.Company!.GetBusinessObject(BoObjectTypes.oInventoryGenEntry);

        goodsReceipt.Series = entrySeries;
        goodsReceipt.DocDate = DateTime.Now;
        goodsReceipt.DocDueDate = DateTime.Now;
        goodsReceipt.Comments = $"Ajuste de inventario para confirmación de WMS {@params.Number} - Entrada de mercancías";

        foreach (var (itemCode, quantity) in @params.PositiveItems) {
            if (!string.IsNullOrWhiteSpace(goodsReceipt.Lines.ItemCode)) {
                goodsReceipt.Lines.Add();
            }

            goodsReceipt.Lines.ItemCode = itemCode;
            goodsReceipt.Lines.Quantity = (double)quantity;
            goodsReceipt.Lines.WarehouseCode = @params.Warehouse;
            goodsReceipt.Lines.UseBaseUnits = BoYesNoEnum.tYES;

            // Add bin allocation if bin locations are enabled
            if (@params is { EnableBinLocation: true, DefaultBinLocation: not null }) {
                goodsReceipt.Lines.BinAllocations.BinAbsEntry = @params.DefaultBinLocation.Value;
                goodsReceipt.Lines.BinAllocations.Quantity = (double)quantity;
                goodsReceipt.Lines.BinAllocations.AllowNegativeQuantity = BoYesNoEnum.tNO;
                goodsReceipt.Lines.BinAllocations.SerialAndBatchNumbersBaseLine = -1;
                // goodsReceipt.Lines.BinAllocations.BaseLineNumber = goodsReceipt.Lines.LineNum;
            }

            logger.LogDebug("Added goods receipt line for item {ItemCode} with quantity {Quantity}", itemCode, quantity);
        }

        int returnValue = goodsReceipt.Add();
        if (returnValue != 0) {
            string errorDescription = sboCompany.Company.GetLastErrorDescription();
            logger.LogError("Failed to create goods receipt. SAP Error: {ErrorDescription}", errorDescription);
            throw new Exception($"Failed to create goods receipt: {errorDescription}");
        }

        return int.Parse(sboCompany.Company.GetNewObjectKey());
    }

    public void Dispose() {
        if (goodsIssue != null) {
            Marshal.ReleaseComObject(goodsIssue);
            goodsIssue = null;
        }

        if (goodsReceipt != null) {
            Marshal.ReleaseComObject(goodsReceipt);
            goodsReceipt = null;
        }
    }
}