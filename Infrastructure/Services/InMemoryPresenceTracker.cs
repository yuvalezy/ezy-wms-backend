using System.Collections.Concurrent;
using Core.Interfaces;

namespace Infrastructure.Services;

/// <summary>
/// In-memory implementation of presence tracking
/// Tracks connection counts per user to handle multiple concurrent connections
/// </summary>
public class InMemoryPresenceTracker : IPresenceTracker {
    private readonly ConcurrentDictionary<string, int> _onlineUsers = new();

    public Task UserConnectedAsync(string userId) {
        _onlineUsers.AddOrUpdate(userId, 1, (_, count) => count + 1);
        return Task.CompletedTask;
    }

    public Task UserDisconnectedAsync(string userId) {
        _onlineUsers.AddOrUpdate(userId, 0, (_, count) => Math.Max(0, count - 1));

        // Remove user from dictionary if connection count reaches 0
        if (_onlineUsers.TryGetValue(userId, out var count) && count == 0) {
            _onlineUsers.TryRemove(userId, out _);
        }

        return Task.CompletedTask;
    }

    public Task<string[]> GetOnlineUsersAsync() {
        var onlineUsers = _onlineUsers
            .Where(kvp => kvp.Value > 0)
            .Select(kvp => kvp.Key)
            .ToArray();

        return Task.FromResult(onlineUsers);
    }

    public Task<bool> IsUserOnlineAsync(string userId) {
        var isOnline = _onlineUsers.TryGetValue(userId, out var count) && count > 0;
        return Task.FromResult(isOnline);
    }
}
