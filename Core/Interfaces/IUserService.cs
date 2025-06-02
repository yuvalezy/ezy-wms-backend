using Core.Entities;
using Core.Models;

namespace Core.Interfaces;

public interface IUserService {
    Task<IEnumerable<UserResponse>> GetUsersAsync();
    Task<User?>                     GetUserAsync(Guid                 id);
    Task<User>                      CreateUserAsync(CreateUserRequest request);
    Task<bool>                      UpdateUserAsync(Guid              id, UpdateUserRequest request, Guid currentUserId);
    Task<bool>                      DeleteUserAsync(Guid              id, Guid              currentUserId);
    Task<bool>                      DisableUserAsync(Guid             id, Guid              currentUserId);
    Task<bool>                      EnableUserAsync(Guid              id);
}