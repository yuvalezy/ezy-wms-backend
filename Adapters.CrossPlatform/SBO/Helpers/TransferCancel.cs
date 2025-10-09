using Adapters.CrossPlatform.SBO.Services;
using Core.DTOs.Transfer;
using Core.Enums;
using Microsoft.Extensions.Logging;

namespace Adapters.CrossPlatform.SBO.Helpers;

public class TransferCancel(SboCompany sboCompany, int docEntry, ILoggerFactory loggerFactory) : IDisposable {
    private readonly ILogger<TransferCancel> logger = loggerFactory.CreateLogger<TransferCancel>();

    public async Task Execute() {
        var body = new { };
        var response = await sboCompany.PostAsync($"StockTransfers({docEntry})/Cancel", body);
        if (!response.success) {
            logger.LogError("Failed to cancel transfer {DocEntry}", docEntry);
        }
    }

    public void Dispose() {
    }
}