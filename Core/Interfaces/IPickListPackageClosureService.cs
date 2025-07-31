using Core.DTOs.PickList;

namespace Core.Interfaces;

public interface IPickListPackageClosureService {
    Task ClearPickListCommitmentsAsync(int absEntry, Guid userId);
    Task ProcessPickListClosureAsync(int absEntry, PickListClosureInfo closureInfo, Guid userId);
    Task ProcessTargetPackageMovements(int absEntry, Guid userId);
}