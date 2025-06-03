using Core.Models;

namespace Core.Interfaces;

public interface ITransferService {
    Task<TransferResponse> CreateTransfer(CreateTransferRequest request, SessionInfo sessionInfo);
    Task<TransferResponse> GetTransfer(Guid                     id,      bool        progress = false);
    Task<IEnumerable<TransferResponse>> GetTransfers(TransfersRequest        request, string      warehouse);
}