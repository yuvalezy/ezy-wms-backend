using System.Collections.Concurrent;
using Core.Interfaces;
using Core.Models;
using Core.Utils;

namespace Infrastructure;

public class InMemorySessionManager : ISessionManager {
    private readonly ConcurrentDictionary<string, (string SessionData, DateTime Expiration)> sessions = new();

    public Task SetValueAsync(string token, string sessionData, TimeSpan expiration) {
        sessions[token] = (sessionData, DateTime.UtcNow.Add(expiration));
        return Task.CompletedTask;
    }

    public Task<string?> GetStringAsync(string token) {
        if (sessions.TryGetValue(token, out var session) && session.Expiration > DateTime.UtcNow) {
            sessions[token] = (session.SessionData, DateTime.UtcNow.AddHours(8));
            return Task.FromResult(session.SessionData);
        }

        // Remove expired session
        sessions.TryRemove(token, out _);
        return Task.FromResult<string?>(null);
    }

    public Task<SessionInfo?> GetSessionAsync(string token) {
        string? rawPayload = GetStringAsync(token)?.Result;
        return rawPayload != null
            ? Task.FromResult(JsonUtils.Deserialize<SessionInfo>(rawPayload))
            : Task.FromResult<SessionInfo?>(null);
    }

    public Task RemoveAsync(string token) {
        sessions.TryRemove(token, out _);
        return Task.CompletedTask;
    }
}