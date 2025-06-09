using Core.DTOs.Settings;

namespace Core.Interfaces;

public interface IAuthorizationGroupService {
    Task<AuthorizationGroupResponse> CreateAsync(CreateAuthorizationGroupRequest request);
    Task<AuthorizationGroupResponse> UpdateAsync(UpdateAuthorizationGroupRequest request);
    Task<bool> DeleteAsync(Guid id);
    Task<AuthorizationGroupResponse?> GetByIdAsync(Guid id);
    Task<IEnumerable<AuthorizationGroupResponse>> GetAllAsync();
}