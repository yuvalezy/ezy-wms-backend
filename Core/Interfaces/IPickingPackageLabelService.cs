using Core.DTOs.PickList;
using Core.Models;

namespace Core.Interfaces;

public interface IPickingPackageLabelService {
    Task<IReadOnlyList<PickingPackageLabelResponse>> ListAsync(int absEntry, string warehouse);
    Task<PickingPackageLabelResponse> CreateNextAsync(int absEntry, SessionInfo sessionInfo);
    Task ValidateForPickListAsync(Guid labelId, int absEntry, string warehouse);
}
