using Core.DTOs.PickList;
using Core.Models;

namespace Core.Services;

public interface IPickListCancelService {
    /// <summary>
    /// Cancels a pick list and creates transfer to move items back to cancel bin location
    /// </summary>
    /// <param name="absEntry">Pick list AbsEntry to cancel</param> 
    /// <param name="sessionInfo">User session information</param>
    /// <returns>Cancellation response with transfer information</returns>
    Task<ProcessPickListCancelResponse> CancelPickListAsync(int absEntry, SessionInfo sessionInfo);
}