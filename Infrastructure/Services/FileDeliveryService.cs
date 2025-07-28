using System.Net;
using System.Runtime.InteropServices;
using System.Security.Principal;
using Core.Enums;
using Core.Models.Settings;
using Core.Services;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services;

/// <summary>
/// Service for delivering files to various destinations
/// </summary>
public class FileDeliveryService(ILogger<FileDeliveryService> logger) : IFileDeliveryService {
    
    public async Task DeliverFileAsync(string filePath, string fileName, CommandDestination destination, CancellationToken cancellationToken = default) {
        try {
            logger.LogInformation("Delivering file {FileName} to {DestinationType} destination", fileName, destination.Type);
            
            switch (destination.Type) {
                case CommandDestinationType.LocalPath:
                    await DeliverToLocalPathAsync(filePath, fileName, destination, cancellationToken);
                    break;
                    
                case CommandDestinationType.NetworkPath:
                    await DeliverToNetworkPathAsync(filePath, fileName, destination, cancellationToken);
                    break;
                    
                case CommandDestinationType.FTP:
                    await DeliverToFtpAsync(filePath, fileName, destination, cancellationToken);
                    break;
                    
                case CommandDestinationType.SFTP:
                    await DeliverToSftpAsync(filePath, fileName, destination, cancellationToken);
                    break;
                    
                default:
                    throw new NotSupportedException($"Destination type {destination.Type} is not supported");
            }
            
            logger.LogInformation("Successfully delivered file {FileName} to {DestinationType} destination", fileName, destination.Type);
        }
        catch (Exception ex) {
            logger.LogError(ex, "Failed to deliver file {FileName} to {DestinationType} destination", fileName, destination.Type);
            throw;
        }
    }
    
    public async Task<bool> TestConnectionAsync(CommandDestination destination, CancellationToken cancellationToken = default) {
        try {
            switch (destination.Type) {
                case CommandDestinationType.LocalPath:
                    return Directory.Exists(destination.Path);
                    
                case CommandDestinationType.NetworkPath:
                    return await TestNetworkPathAsync(destination, cancellationToken);
                    
                case CommandDestinationType.FTP:
                    return await TestFtpConnectionAsync(destination, cancellationToken);
                    
                case CommandDestinationType.SFTP:
                    return await TestSftpConnectionAsync(destination, cancellationToken);
                    
                default:
                    return false;
            }
        }
        catch (Exception ex) {
            logger.LogError(ex, "Failed to test connection to {DestinationType} destination", destination.Type);
            return false;
        }
    }
    
    private async Task DeliverToLocalPathAsync(string filePath, string fileName, CommandDestination destination, CancellationToken cancellationToken) {
        if (!Directory.Exists(destination.Path)) {
            Directory.CreateDirectory(destination.Path);
        }
        
        var destinationPath = Path.Combine(destination.Path, fileName);
        await FileExtensions.CopyAsync(filePath, destinationPath, cancellationToken);
    }
    
    private async Task DeliverToNetworkPathAsync(string filePath, string fileName, CommandDestination destination, CancellationToken cancellationToken) {
        if (destination.UseNetworkImpersonation && !string.IsNullOrEmpty(destination.Username) && !string.IsNullOrEmpty(destination.Password)) {
            await ImpersonateAndDeliverAsync(filePath, fileName, destination, cancellationToken);
        } else {
            await DeliverToLocalPathAsync(filePath, fileName, destination, cancellationToken);
        }
    }
    
    private async Task DeliverToFtpAsync(string filePath, string fileName, CommandDestination destination, CancellationToken cancellationToken) {
        if (string.IsNullOrEmpty(destination.Host)) {
            throw new ArgumentException("Host is required for FTP destinations");
        }
        
        var ftpUri = $"ftp://{destination.Host}:{destination.Port ?? 21}{destination.Path?.TrimEnd('/')}/{fileName}";
        var request = (FtpWebRequest)WebRequest.Create(ftpUri);
        request.Method = WebRequestMethods.Ftp.UploadFile;
        request.UsePassive = destination.UsePassiveMode;
        request.UseBinary = true;
        
        if (!string.IsNullOrEmpty(destination.Username)) {
            request.Credentials = new NetworkCredential(destination.Username, destination.Password);
        }
        
        if (destination.UseSsl) {
            request.EnableSsl = true;
        }
        
        using var fileStream = File.OpenRead(filePath);
        using var requestStream = await request.GetRequestStreamAsync();
        await fileStream.CopyToAsync(requestStream, cancellationToken);
        
        using var response = (FtpWebResponse)await request.GetResponseAsync();
        if (response.StatusCode != FtpStatusCode.ClosingData) {
            throw new Exception($"FTP upload failed with status: {response.StatusCode} - {response.StatusDescription}");
        }
    }
    
    private async Task DeliverToSftpAsync(string filePath, string fileName, CommandDestination destination, CancellationToken cancellationToken) {
        // Note: This is a placeholder implementation. In a real scenario, you would use a library like SSH.NET
        // For now, throwing NotImplementedException to indicate this needs proper SFTP library integration
        throw new NotImplementedException("SFTP delivery requires SSH.NET or similar library integration");
        
        // Example implementation with SSH.NET would look like:
        /*
        using var client = new SftpClient(destination.Host, destination.Port ?? 22, destination.Username, destination.Password);
        await client.ConnectAsync(cancellationToken);
        
        var remotePath = $"{destination.Path?.TrimEnd('/')}/{fileName}";
        using var fileStream = File.OpenRead(filePath);
        await client.UploadAsync(fileStream, remotePath, cancellationToken);
        
        client.Disconnect();
        */
    }
    
    private async Task<bool> TestNetworkPathAsync(CommandDestination destination, CancellationToken cancellationToken) {
        try {
            if (destination.UseNetworkImpersonation && !string.IsNullOrEmpty(destination.Username) && !string.IsNullOrEmpty(destination.Password)) {
                return await Task.Run(() => {
                    using var context = ImpersonationContext.Create(destination.Username, destination.Password);
                    return Directory.Exists(destination.Path);
                }, cancellationToken);
            } else {
                return Directory.Exists(destination.Path);
            }
        }
        catch {
            return false;
        }
    }
    
    private async Task<bool> TestFtpConnectionAsync(CommandDestination destination, CancellationToken cancellationToken) {
        try {
            if (string.IsNullOrEmpty(destination.Host)) return false;
            
            var ftpUri = $"ftp://{destination.Host}:{destination.Port ?? 21}{destination.Path ?? "/"}";
            var request = (FtpWebRequest)WebRequest.Create(ftpUri);
            request.Method = WebRequestMethods.Ftp.ListDirectory;
            request.UsePassive = destination.UsePassiveMode;
            
            if (!string.IsNullOrEmpty(destination.Username)) {
                request.Credentials = new NetworkCredential(destination.Username, destination.Password);
            }
            
            if (destination.UseSsl) {
                request.EnableSsl = true;
            }
            
            using var response = (FtpWebResponse)await request.GetResponseAsync();
            return response.StatusCode == FtpStatusCode.OpeningData || response.StatusCode == FtpStatusCode.DataAlreadyOpen;
        }
        catch {
            return false;
        }
    }
    
    private async Task<bool> TestSftpConnectionAsync(CommandDestination destination, CancellationToken cancellationToken) {
        // Placeholder for SFTP connection test
        await Task.CompletedTask;
        return false;
    }
    
    private async Task ImpersonateAndDeliverAsync(string filePath, string fileName, CommandDestination destination, CancellationToken cancellationToken) {
        await Task.Run(() => {
            using var context = ImpersonationContext.Create(destination.Username!, destination.Password!);
            var destinationPath = Path.Combine(destination.Path, fileName);
            
            if (!Directory.Exists(destination.Path)) {
                Directory.CreateDirectory(destination.Path);
            }
            
            File.Copy(filePath, destinationPath, true);
        }, cancellationToken);
    }
}

/// <summary>
/// Helper class for Windows impersonation
/// </summary>
internal class ImpersonationContext : IDisposable {
    private readonly WindowsIdentity? _impersonatedIdentity;
    
    private ImpersonationContext(WindowsIdentity? impersonatedIdentity) {
        _impersonatedIdentity = impersonatedIdentity;
    }
    
    public static ImpersonationContext Create(string username, string password) {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
            return new ImpersonationContext(null);
        }
        
        try {
            // Parse domain\username format
            var parts = username.Split('\\');
            var domain = parts.Length > 1 ? parts[0] : ".";
            var user = parts.Length > 1 ? parts[1] : username;
            
            var token = IntPtr.Zero;
            var success = LogonUser(user, domain, password, 2, 0, out token);
            
            if (!success) {
                throw new UnauthorizedAccessException("Failed to impersonate user");
            }
            
            var identity = new WindowsIdentity(token);
            
            return new ImpersonationContext(identity);
        }
        catch {
            return new ImpersonationContext(null);
        }
    }
    
    public void Dispose() {
        _impersonatedIdentity?.Dispose();
    }
    
    [DllImport("advapi32.dll", SetLastError = true)]
    private static extern bool LogonUser(string lpszUsername, string lpszDomain, string lpszPassword, int dwLogonType, int dwLogonProvider, out IntPtr phToken);
}

// Extension method for File.CopyAsync (if not available in the framework version)
internal static class FileExtensions {
    public static async Task CopyAsync(string source, string destination, CancellationToken cancellationToken = default) {
        using var sourceStream = File.OpenRead(source);
        using var destinationStream = File.Create(destination);
        await sourceStream.CopyToAsync(destinationStream, cancellationToken);
    }
}