using Core.DTOs;
using Core.Models;

namespace Core.Interfaces;

public interface IAuthenticationService {
    Task<SessionInfo?> LoginAsync(LoginRequest  request);
    Task<bool>         ChangePasswordAsync(Guid userId, string currentPassword, string newPassword);
    Task               LogoutAsync(string       sessionToken);
}