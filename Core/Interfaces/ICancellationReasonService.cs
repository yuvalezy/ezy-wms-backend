using Core.DTOs;
using Core.DTOs.Settings;
using Core.Enums;

namespace Core.Interfaces;

public interface ICancellationReasonService {
    Task<CancellationReasonResponse>              CreateAsync(CreateCancellationReasonRequest request);
    Task<CancellationReasonResponse>              UpdateAsync(UpdateCancellationReasonRequest request);
    Task<bool>                                    DeleteAsync(Guid                            id);
    Task<IEnumerable<CancellationReasonResponse>> GetAllAsync(GetCancellationReasonsRequest   request);
    Task<CancellationReasonResponse?> GetByIdAsync(Guid id);
}