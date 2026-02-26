using Core.DTOs.DirectTransfer;
using Core.Models;

namespace Core.Services;

public interface IDirectTransferService {
    Task<DirectTransferResponse> ExecuteAsync(DirectTransferRequest request, SessionInfo sessionInfo);
}
