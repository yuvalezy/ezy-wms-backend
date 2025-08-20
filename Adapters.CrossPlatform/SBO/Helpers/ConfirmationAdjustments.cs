using Adapters.CrossPlatform.SBO.Services;
using Microsoft.Extensions.Logging;

namespace Adapters.CrossPlatform.SBO.Helpers;

public class ConfirmationAdjustments(
    int number,
    string warehouse,
    bool enableBinLocation,
    int? defaultBinLocation,
    List<(string ItemCode, decimal Quantity)> negativeItems,
    List<(string ItemCode, decimal Quantity)> positiveItems,
    int entrySeries,
    int exitSeries,
    SboCompany sboCompany,
    ILoggerFactory loggerFactory) {
    public async Task<(bool success, string? errorMessage)> Execute() {
        
        if (negativeItems.Count > 0) {
            // todo: Create SBO Inventory Goods Issue
        }

        if (positiveItems.Count > 0) {
            // todo: Create SBO Inventory Goods Entry
        }
        
        return (true, null);
    }
}