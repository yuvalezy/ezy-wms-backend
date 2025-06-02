using Core.Models;

namespace Core.Interfaces;

public interface IExternalSystemAdapter {
    Task<ExternalUserResponse?> GetUserInfoAsync(string id);
    Task<IEnumerable<ExternalUserResponse>> GetUsersAsync();
}