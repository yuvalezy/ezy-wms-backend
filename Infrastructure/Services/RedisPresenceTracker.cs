using System.Text.Json;
using Core.Interfaces;
using Microsoft.Extensions.Caching.Distributed;

namespace Infrastructure.Services;

/// <summary>
/// Redis-based implementation of presence tracking
/// Uses distributed cache for multi-instance deployment support
/// </summary>
public class RedisPresenceTracker(IDistributedCache cache) : IPresenceTracker {
    private const string PresenceKeyPrefix = "presence:user:";
    private const string OnlineUsersSetKey = "presence:online_users";

    public async Task UserConnectedAsync(string userId) {
        var key = $"{PresenceKeyPrefix}{userId}";

        // Get current connection count
        var countStr = await cache.GetStringAsync(key);
        var count = string.IsNullOrEmpty(countStr) ? 0 : int.Parse(countStr);
        count++;

        // Store updated count with no expiration (manually managed)
        await cache.SetStringAsync(key, count.ToString());

        // Add user to online users set
        await AddToOnlineUsersSetAsync(userId);
    }

    public async Task UserDisconnectedAsync(string userId) {
        var key = $"{PresenceKeyPrefix}{userId}";

        // Get current connection count
        var countStr = await cache.GetStringAsync(key);
        if (string.IsNullOrEmpty(countStr)) return;

        var count = int.Parse(countStr);
        count = Math.Max(0, count - 1);

        if (count == 0) {
            // Remove from cache and online users set
            await cache.RemoveAsync(key);
            await RemoveFromOnlineUsersSetAsync(userId);
        }
        else {
            // Update count
            await cache.SetStringAsync(key, count.ToString());
        }
    }

    public async Task<string[]> GetOnlineUsersAsync() {
        var setStr = await cache.GetStringAsync(OnlineUsersSetKey);
        if (string.IsNullOrEmpty(setStr)) return Array.Empty<string>();

        try {
            var userSet = JsonSerializer.Deserialize<HashSet<string>>(setStr);
            return userSet?.ToArray() ?? Array.Empty<string>();
        }
        catch {
            return Array.Empty<string>();
        }
    }

    public async Task<bool> IsUserOnlineAsync(string userId) {
        var key = $"{PresenceKeyPrefix}{userId}";
        var countStr = await cache.GetStringAsync(key);

        if (string.IsNullOrEmpty(countStr)) return false;

        return int.TryParse(countStr, out var count) && count > 0;
    }

    private async Task AddToOnlineUsersSetAsync(string userId) {
        var setStr = await cache.GetStringAsync(OnlineUsersSetKey);
        var userSet = string.IsNullOrEmpty(setStr)
            ? new HashSet<string>()
            : JsonSerializer.Deserialize<HashSet<string>>(setStr) ?? new HashSet<string>();

        userSet.Add(userId);

        var updatedSetStr = JsonSerializer.Serialize(userSet);
        await cache.SetStringAsync(OnlineUsersSetKey, updatedSetStr);
    }

    private async Task RemoveFromOnlineUsersSetAsync(string userId) {
        var setStr = await cache.GetStringAsync(OnlineUsersSetKey);
        if (string.IsNullOrEmpty(setStr)) return;

        var userSet = JsonSerializer.Deserialize<HashSet<string>>(setStr);
        if (userSet == null) return;

        userSet.Remove(userId);

        var updatedSetStr = JsonSerializer.Serialize(userSet);
        await cache.SetStringAsync(OnlineUsersSetKey, updatedSetStr);
    }
}
