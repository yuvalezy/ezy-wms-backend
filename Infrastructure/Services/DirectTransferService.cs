using Core.DTOs.DirectTransfer;
using Core.DTOs.Transfer;
using Core.Enums;
using Core.Interfaces;
using Core.Models;
using Core.Services;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services;

public class DirectTransferService(
    IExternalSystemAdapter adapter,
    ILogger<DirectTransferService> logger) : IDirectTransferService {

    public async Task<DirectTransferResponse> ExecuteAsync(DirectTransferRequest request, SessionInfo sessionInfo) {
        decimal quantity;

        if (request.TransferAll) {
            var binStocks = await adapter.ItemBinStockAsync(request.ItemCode, sessionInfo.Warehouse);
            var binStock = binStocks.FirstOrDefault(s => s.BinEntry == request.SourceBinEntry);
            if (binStock == null || binStock.Quantity <= 0) {
                return new DirectTransferResponse {
                    Success = false,
                    ErrorMessage = $"No stock found for item {request.ItemCode} in source bin"
                };
            }
            // Bin stock is already in base units, so it transfers as-is — no unit conversion here.
            quantity = binStock.Quantity;
        } else {
            if (request.Quantity <= 0) {
                return new DirectTransferResponse {
                    Success = false,
                    ErrorMessage = "Quantity must be greater than zero"
                };
            }

            // The operator scans a count in a unit (Unit/Dozen/Pack); convert it to base pieces through the same
            // calculator the document-based transfer uses, or a dozen scan would move a single piece. The item's
            // NumInBuy/PurPackUn already reflect Options.ForceDoz because the SQL read applies it.
            var unit = Enum.TryParse<UnitType>(request.UnitCode, ignoreCase: true, out var parsedUnit)
                ? parsedUnit
                : UnitType.Unit;
            var item = (await adapter.ItemCheckAsync(request.ItemCode, null)).FirstOrDefault();
            if (item == null && unit != UnitType.Unit) {
                return new DirectTransferResponse {
                    Success = false,
                    ErrorMessage = $"Item {request.ItemCode} not found"
                };
            }

            quantity = item == null
                ? request.Quantity
                : GoodsReceiptQuantityCalculator.ToBaseQuantity(request.Quantity, unit, item);
        }

        logger.LogInformation(
            "Executing direct transfer: Item {ItemCode}, Qty {Quantity}, Source Bin {SourceBin} -> Target Bin {TargetBin} by user {User}",
            request.ItemCode, quantity, request.SourceBinEntry, request.TargetBinEntry, sessionInfo.Name);

        var transferData = new Dictionary<string, TransferCreationDataResponse> {
            [request.ItemCode] = new TransferCreationDataResponse {
                ItemCode = request.ItemCode,
                Quantity = quantity,
                SourceBins = [
                    new TransferCreationBinResponse {
                        BinEntry = request.SourceBinEntry,
                        Quantity = quantity
                    }
                ],
                TargetBins = [
                    new TransferCreationBinResponse {
                        BinEntry = request.TargetBinEntry,
                        Quantity = quantity
                    }
                ]
            }
        };

        var result = await adapter.ProcessTransfer(
            transferNumber: 0,
            sourceWarehouse: sessionInfo.Warehouse,
            targetWaarehouse: null,
            comments: $"Direct Transfer by {sessionInfo.Name}",
            data: transferData,
            alertRecipients: []);

        if (!result.Success) {
            logger.LogError("Direct transfer failed: {Error}", result.ErrorMessage);
            return new DirectTransferResponse {
                Success = false,
                ErrorMessage = result.ErrorMessage
            };
        }

        logger.LogInformation(
            "Direct transfer successful: SAP Doc {DocNum} (Entry {DocEntry})",
            result.ExternalNumber, result.ExternalEntry);

        return new DirectTransferResponse { Success = true };
    }
}
