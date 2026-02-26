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
        logger.LogInformation(
            "Executing direct transfer: Item {ItemCode}, Qty {Quantity}, Source Bin {SourceBin} -> Target Bin {TargetBin} by user {User}",
            request.ItemCode, request.Quantity, request.SourceBinEntry, request.TargetBinEntry, sessionInfo.Name);

        var transferData = new Dictionary<string, TransferCreationDataResponse> {
            [request.ItemCode] = new TransferCreationDataResponse {
                ItemCode = request.ItemCode,
                Quantity = request.Quantity,
                SourceBins = [
                    new TransferCreationBinResponse {
                        BinEntry = request.SourceBinEntry,
                        Quantity = request.Quantity
                    }
                ],
                TargetBins = [
                    new TransferCreationBinResponse {
                        BinEntry = request.TargetBinEntry,
                        Quantity = request.Quantity
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
