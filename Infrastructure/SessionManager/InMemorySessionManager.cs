using System.Collections.Concurrent;
using Core.Interfaces;
using Core.Models;
using Core.Utils;

namespace Infrastructure.SessionManager;

public class InMemorySessionManager : ISessionManager {
    private readonly ConcurrentDictionary<string, (string SessionData, DateTime? Expiration)> sessions = new();

    public Task SetValueAsync(string id, string value, TimeSpan expiration) {
        sessions[id] = (value, DateTime.UtcNow.Add(expiration));
        return Task.CompletedTask;
    }

    public Task SetStringAsync(string id, string value) {
        sessions[id] = (value, null);
        return Task.CompletedTask;
    }

    public Task<string?> GetStringAsync(string id) {
        if (sessions.TryGetValue(id, out var session) && (session.Expiration == null || session.Expiration > DateTime.UtcNow)) {
            sessions[id] = (session.SessionData, DateTime.UtcNow.AddHours(8));
            return Task.FromResult(session.SessionData);
        }

        // Remove expired session
        sessions.TryRemove(id, out _);
        return Task.FromResult<string?>(null);
    }

    public Task<SessionInfo?> GetSessionAsync(string id) {
        string? rawPayload = GetStringAsync(id)?.Result;
        return rawPayload != null
            ? Task.FromResult(JsonUtils.Deserialize<SessionInfo>(rawPayload))
            : Task.FromResult<SessionInfo?>(null);
    }

    public Task RemoveAsync(string id) {
        sessions.TryRemove(id, out _);
        return Task.CompletedTask;
    }
}