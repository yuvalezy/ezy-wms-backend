using Core.DTOs;
using Core.Enums;

namespace Core.Interfaces;

public interface ICancellationReasonService {
    Task<CancellationReasonResponse> CreateAsync(CreateCancellationReasonRequest request);
    Task<CancellationReasonResponse> UpdateAsync(UpdateCancellationReasonRequest request);
    Task<bool> DeleteAsync(Guid id);
    Task<IEnumerable<CancellationReasonResponse>> GetAllAsync(ObjectType? objectType = null, bool includeDisabled = false);
    Task<CancellationReasonResponse?> GetByIdAsync(Guid id);
}