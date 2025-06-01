using Core.Models;

namespace Core.Interfaces;

public interface ISessionManager {
    Task               SetValueAsync(string   token, string sessionData, TimeSpan expiration);
    Task<string?>      GetStringAsync(string  token);
    Task<SessionInfo?> GetSessionAsync(string token);
    Task               RemoveAsync(string     token);
}