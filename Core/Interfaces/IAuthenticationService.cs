using Core.DTOs;
using Core.DTOs.General;
using Core.Models;

namespace Core.Interfaces;

public interface IAuthenticationService {
    Task<SessionInfo?> LoginAsync(LoginRequest request, string deviceUuid);
    Task<bool>         ChangePasswordAsync(Guid userId, string currentPassword, string newPassword);
    Task               LogoutAsync(string       sessionToken);
}