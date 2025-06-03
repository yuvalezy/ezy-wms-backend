using Core.Models;

namespace Core.Interfaces;

public interface IAuthenticationService {
    Task<(SessionInfo? sessionInfo, string sessionId)> LoginAsync(LoginRequest request);
    Task<bool>                                          ChangePasswordAsync(Guid userId, string currentPassword, string newPassword);
    Task                                                LogoutAsync(string       sessionToken);
}