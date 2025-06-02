using Core.Interfaces;
using Core.Models;
using Core.Utils;
using StackExchange.Redis;

namespace Infrastructure.SessionManager;

public class RedisSessionManager : ISessionManager {
    private readonly IDatabase database;

    public RedisSessionManager(ISettings settings) {
        string host          = settings.SessionManagement.Redis.Host ?? "localhost";
        int    port          = settings.SessionManagement.Redis.Port ?? 6379;
        string configuration = $"{host}:{port}";
        var    connection    = ConnectionMultiplexer.Connect(configuration);
        database = connection.GetDatabase();
    }

    public async Task SetValueAsync(string id, string value, TimeSpan expiration) {
        await database.StringSetAsync(id, value, expiration);
    }

    public async Task SetStringAsync(string id, string value) => await database.StringSetAsync(id, value);

    public async Task<string?> GetStringAsync(string id) {
        var data = await database.StringGetAsync(id);
        if (!data.HasValue) 
            return null;
        // renew TTL
        await database.KeyExpireAsync(id, TimeSpan.FromHours(8));
        return data;
    }

    public async Task<SessionInfo?> GetSessionAsync(string id) {
        string? strPayload = await GetStringAsync(id);
        return strPayload is not null
            ? JsonUtils.Deserialize<SessionInfo>(strPayload)
            : null;
    }

    public async Task RemoveAsync(string token) {
        await database.KeyDeleteAsync(token);
    }
}