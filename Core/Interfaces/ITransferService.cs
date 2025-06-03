using Core.Models;

namespace Core.Interfaces;

public interface ITransferService {
    Task<TransferResponse> CreateTransfer(CreateTransferRequest request, SessionInfo sessionInfo);
}