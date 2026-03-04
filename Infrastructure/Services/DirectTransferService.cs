using Core.DTOs.DirectTransfer;
using Core.DTOs.Transfer;
using Core.Interfaces;
using Core.Models;
using Core.Services;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services;

public class DirectTransferService(
    IExternalSystemAdapter adapter,
    ILogger<DirectTransferService> logger) : IDirectTransferService {

    public async Task<DirectTransferResponse> ExecuteAsync(DirectTransferRequest request, SessionInfo sessionInfo) {
        var quantity = request.Quantity;

        if (request.TransferAll) {
            var binStocks = await adapter.ItemBinStockAsync(request.ItemCode, sessionInfo.Warehouse);
            var binStock = binStocks.FirstOrDefault(s => s.BinEntry == request.SourceBinEntry);
            if (binStock == null || binStock.Quantity <= 0) {
                return new DirectTransferResponse {
                    Success = false,
                    ErrorMessage = $"No stock found for item {request.ItemCode} in source bin"
                };
            }
            quantity = binStock.Quantity;
        } else if (quantity <= 0) {
            return new DirectTransferResponse {
                Success = false,
                ErrorMessage = "Quantity must be greater than zero"
            };
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
