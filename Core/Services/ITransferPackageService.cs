using Core.DTOs.Transfer;
using Core.Models;

namespace Core.Services;

public interface ITransferPackageService {
    Task<TransferAddItemResponse> HandleSourcePackageScanAsync(TransferAddSourcePackageRequest request, SessionInfo sessionInfo);
    Task<TransferAddItemResponse> HandleTargetPackageTransferAsync(TransferAddTargetPackageRequest request, SessionInfo sessionInfo);
    Task MovePackagesOnTransferProcessAsync(Guid transferId, SessionInfo sessionInfo);
}