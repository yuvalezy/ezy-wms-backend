using Adapters.Windows.SBO.Services;
using Core.DTOs.PickList;
using Core.Enums;
using Microsoft.Extensions.Logging;
using SAPbobsCOM;

namespace Adapters.Windows.SBO.Helpers;

public class PickingCancellation(SboCompany sboCompany, int absEntry, ILoggerFactory loggerFactory) {
    private readonly ILogger<PickingCancellation> logger = loggerFactory.CreateLogger<PickingCancellation>();
    public ProcessPickListResponse Execute() {
        logger.LogInformation("Starting pick list cancellation for AbsEntry: {AbsEntry}", absEntry);
        try {
            if (!sboCompany.TransactionMutex.WaitOne()) {
                logger.LogWarning("Unable to acquire transaction mutex for pick list {AbsEntry}", absEntry);
                return ErrorMessage("Unable to acquire transaction mutex");
            }

            try {
                sboCompany.ConnectCompany();

                var pl = (PickLists)sboCompany!.Company.GetBusinessObject(BoObjectTypes.oPickLists);
                if (!pl.GetByKey(absEntry)) {
                    logger.LogWarning("Could not find Pick List with AbsEntry: {AbsEntry}", absEntry);
                    return ErrorMessage($"Could not find Pick List ${absEntry}");
                }
                
                if (pl.Status == BoPickStatus.ps_Closed) {
                    logger.LogWarning("Cannot cancel pick list {AbsEntry} - status is already closed", absEntry);
                    return ErrorMessage("Cannot cancel process if the Status is closed");
                }

                int returnValue = pl.Close();
                if (returnValue != 0) {
                    string errorDescription = sboCompany.Company.GetLastErrorDescription();
                    logger.LogError("Failed to close pick list {AbsEntry}. SAP Error: {ErrorDescription}", absEntry, errorDescription);
                    return ErrorMessage(errorDescription);
                }
                    
                logger.LogInformation("Successfully cancelled pick list {AbsEntry}", absEntry);
                return new ProcessPickListResponse { Status = ResponseStatus.Ok };
            }
            finally {
                sboCompany.TransactionMutex.ReleaseMutex();
            }
        }
        catch (Exception ex) {
            logger.LogError(ex, "Error cancelling pick list {AbsEntry}: {ErrorMessage}", absEntry, ex.Message);
            return ErrorMessage($"Error generating Goods Receipt: {ex.Message}");
        }
    }

    private static ProcessPickListResponse ErrorMessage(string errorMessage) {
        return new ProcessPickListResponse {
            Status       = ResponseStatus.Error,
            ErrorMessage = errorMessage
        };
    }
}