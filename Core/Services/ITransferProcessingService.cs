using Core.DTOs.Transfer;
using Core.Models;

namespace Core.Services;

public interface ITransferProcessingService {
    Task<bool>                             CancelTransfer(Guid                        id,      SessionInfo sessionInfo);
    Task<ProcessTransferResponse>          ProcessTransfer(Guid                       id,      SessionInfo sessionInfo);
    Task<Dictionary<string, TransferCreationDataResponse>> PrepareTransferData(Guid   transferId);
    Task<CreateTransferRequestResponse>    CreateTransferRequest(CreateTransferRequestRequest request, SessionInfo sessionInfo);
    Task<ProcessTransferResponse>          ApproveTransferRequest(TransferApprovalRequest     request, SessionInfo sessionInfo);
}
