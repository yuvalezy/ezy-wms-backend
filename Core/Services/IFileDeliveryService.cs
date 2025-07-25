using Core.Models.Settings;

namespace Core.Services;

/// <summary>
/// Service for delivering files to various destinations (local, network, FTP, SFTP)
/// </summary>
public interface IFileDeliveryService {
    /// <summary>
    /// Delivers a file to the specified destination
    /// </summary>
    /// <param name="filePath">Path to the file to deliver</param>
    /// <param name="fileName">Name of the file at destination</param>
    /// <param name="destination">Destination configuration</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task representing the delivery operation</returns>
    Task DeliverFileAsync(string filePath, string fileName, CommandDestination destination, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Tests connectivity to a destination
    /// </summary>
    /// <param name="destination">Destination configuration</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if connection successful, false otherwise</returns>
    Task<bool> TestConnectionAsync(CommandDestination destination, CancellationToken cancellationToken = default);
}