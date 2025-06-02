using Core.Models;

namespace Core.Interfaces;

public interface IAuthenticationService {
    Task<SessionInfo?> LoginAsync(string        password);
    Task<bool>         ChangePasswordAsync(Guid userId, string currentPassword, string newPassword);
    Task               LogoutAsync(string       sessionToken);
}