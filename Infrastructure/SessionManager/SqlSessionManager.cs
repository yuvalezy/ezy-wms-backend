using System.Security.Cryptography;
using System.Text;
using Core.Entities;
using Core.Interfaces;
using Core.Models;
using Core.Utils;
using Infrastructure.DbContexts;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.SessionManager;

public class SqlSessionManager(IServiceScopeFactory scopeFactory) : ISessionManager {
    public async Task SetValueAsync(string id, string value, TimeSpan expiration) {
        await UpsertAsync(id, value, DateTime.UtcNow.Add(expiration));
    }

    public async Task SetStringAsync(string id, string value) {
        await UpsertAsync(id, value, null);
    }

    public async Task<string?> GetStringAsync(string id) {
        await using var scope = scopeFactory.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<SystemDbContext>();
        string key = HashId(id);

        var session = await dbContext.WmsSessions.FindAsync(key);
        if (session == null) {
            return null;
        }

        if (session.ExpiresAt.HasValue && session.ExpiresAt.Value <= DateTime.UtcNow) {
            dbContext.WmsSessions.Remove(session);
            await dbContext.SaveChangesAsync();
            return null;
        }

        return session.SessionData;
    }

    public async Task<SessionInfo?> GetSessionAsync(string id) {
        string? rawPayload = await GetStringAsync(id);
        return rawPayload != null
            ? JsonUtils.Deserialize<SessionInfo>(rawPayload)
            : null;
    }

    public async Task RemoveAsync(string id) {
        await using var scope = scopeFactory.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<SystemDbContext>();
        string key = HashId(id);

        var session = await dbContext.WmsSessions.FindAsync(key);
        if (session == null) {
            return;
        }

        dbContext.WmsSessions.Remove(session);
        await dbContext.SaveChangesAsync();
    }

    private async Task UpsertAsync(string id, string value, DateTime? expiresAt) {
        await using var scope = scopeFactory.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<SystemDbContext>();
        string key = HashId(id);
        var now = DateTime.UtcNow;

        var session = await dbContext.WmsSessions.FindAsync(key);
        if (session == null) {
            dbContext.WmsSessions.Add(new WmsSession {
                Id          = key,
                SessionData = value,
                ExpiresAt   = expiresAt,
                CreatedAt   = now,
                UpdatedAt   = now
            });
        }
        else {
            session.SessionData = value;
            session.ExpiresAt   = expiresAt;
            session.UpdatedAt   = now;
        }

        await dbContext.SaveChangesAsync();
    }

    private static string HashId(string id) {
        byte[] bytes = SHA256.HashData(Encoding.UTF8.GetBytes(id));
        return Convert.ToHexString(bytes);
    }
}
