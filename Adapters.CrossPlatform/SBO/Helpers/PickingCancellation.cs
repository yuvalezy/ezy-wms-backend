using Adapters.CrossPlatform.SBO.Services;
using Core.DTOs.Items;
using Core.DTOs.PickList;
using Core.Enums;
using Microsoft.Extensions.Logging;

namespace Adapters.CrossPlatform.SBO.Helpers;

public class PickingCancellation(SboCompany sboCompany, int absEntry, PickingSelectionResponse[] selection, int transferBinEntry, ILoggerFactory loggerFactory) {
    private readonly ILogger<PickingCancellation> logger = loggerFactory.CreateLogger<PickingCancellation>();

    public async Task<ProcessPickListResponse> Execute() {
        logger.LogInformation("Starting pick list cancellation for AbsEntry: {AbsEntry}", absEntry);
        try {
            var data = new {
                PickList = new {
                    Absoluteentry = absEntry
                }
            };
            (bool success, string? errorMessage) = await sboCompany.PostAsync("PickListsService_Close", data);
            if (!success) {
                logger.LogError("Failed to close pick list {AbsEntry} via Service Layer. Error: {ErrorMessage}", absEntry, errorMessage);
                return new ProcessPickListResponse {
                    Status       = ResponseStatus.Error,
                    ErrorMessage = errorMessage
                };
            }

            logger.LogInformation("Successfully cancelled pick list {AbsEntry}", absEntry);
            return new ProcessPickListResponse { Status = ResponseStatus.Ok };
        }
        catch (Exception ex) {
            logger.LogError(ex, "Error cancelling pick list {AbsEntry}: {ErrorMessage}", absEntry, ex.Message);
            return new ProcessPickListResponse {
                Status       = ResponseStatus.Error,
                ErrorMessage = ex.Message
            };
        }
    }
}