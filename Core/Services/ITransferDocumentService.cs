using Core.DTOs.Transfer;
using Core.Models;

namespace Core.Services;

public interface ITransferDocumentService {
    Task<TransferResponse>              CreateTransfer(CreateTransferRequest request, SessionInfo sessionInfo);
    Task<TransferResponse>              GetTransfer(Guid                     id,      bool        progress = false);
    Task<IEnumerable<TransferResponse>> GetTransfers(TransfersRequest        request, string      warehouse);
    Task<TransferResponse>              GetProcessInfo(Guid                  id);
}
