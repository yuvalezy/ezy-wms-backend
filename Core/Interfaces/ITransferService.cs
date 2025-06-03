using Core.Models;

namespace Core.Interfaces;

public interface ITransferService {
    Task<TransferResponse>              CreateTransfer(CreateTransferRequest request, SessionInfo sessionInfo);
    Task<IEnumerable<TransferResponse>> GetTransfers(TransfersRequest        request, string      warehouse);
}