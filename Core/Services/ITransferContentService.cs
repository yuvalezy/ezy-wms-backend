using Core.DTOs.Transfer;
using Core.Models;

namespace Core.Services;

public interface ITransferContentService {
    Task<IEnumerable<TransferContentResponse>>             GetTransferContent(TransferContentRequest                          request);
    Task<IEnumerable<TransferContentTargetDetailResponse>> GetTransferContentTargetDetail(TransferContentTargetDetailRequest  request);
    Task                                                   UpdateContentTargetDetail(TransferUpdateContentTargetDetailRequest request, SessionInfo sessionInfo);
}
