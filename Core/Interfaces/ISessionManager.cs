using Core.Models;

namespace Core.Interfaces;

public interface ISessionManager {
    Task               SetValueAsync(string   id, string value, TimeSpan expiration);
    Task               SetStringAsync(string  id,    string value);
    Task<string?>      GetStringAsync(string  id);
    Task<SessionInfo?> GetSessionAsync(string id);
    Task               RemoveAsync(string     id);
}