namespace Core.Interfaces;

/// <summary>
/// Service for tracking online presence of users
/// </summary>
public interface IPresenceTracker {
    /// <summary>
    /// Mark a user as connected
    /// </summary>
    /// <param name="userId">The user ID</param>
    /// <returns>Task that completes when the operation is done</returns>
    Task UserConnectedAsync(string userId);

    /// <summary>
    /// Mark a user as disconnected
    /// </summary>
    /// <param name="userId">The user ID</param>
    /// <returns>Task that completes when the operation is done</returns>
    Task UserDisconnectedAsync(string userId);

    /// <summary>
    /// Get all currently online user IDs
    /// </summary>
    /// <returns>Array of online user IDs</returns>
    Task<string[]> GetOnlineUsersAsync();

    /// <summary>
    /// Check if a specific user is online
    /// </summary>
    /// <param name="userId">The user ID to check</param>
    /// <returns>True if the user is online, false otherwise</returns>
    Task<bool> IsUserOnlineAsync(string userId);
}
