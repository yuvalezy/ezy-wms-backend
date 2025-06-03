using Core.DTOs;
using Core.Models;

namespace Core.Interfaces;

public interface ITransferService {
    Task<TransferResponse>              CreateTransfer(CreateTransferRequest request, SessionInfo sessionInfo);
    Task<TransferResponse>              GetTransfer(Guid                     id,      bool        progress = false);
    Task<IEnumerable<TransferResponse>> GetTransfers(TransfersRequest        request, string      warehouse);
    Task<TransferResponse>              GetProcessInfo(Guid                  id);
    Task<bool>                          CancelTransfer(Guid                  id,      SessionInfo sessionInfo);
    Task<ProcessTransferResponse>       ProcessTransfer(Guid                 id,      SessionInfo sessionInfo);
    Task<IEnumerable<TransferContentResponse>> GetTransferContent(TransferContentRequest request);
    Task<IEnumerable<TransferContentTargetDetailResponse>> GetTransferContentTargetDetail(TransferContentTargetDetailRequest request);
    Task UpdateContentTargetDetail(UpdateContentTargetDetailRequest request, SessionInfo sessionInfo);
    Task<CreateTransferRequestResponse> CreateTransferRequest(CreateTransferRequestRequest request, SessionInfo sessionInfo);
}