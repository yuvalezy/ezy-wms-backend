using Core.Models;
using Core.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Infrastructure.Services;

public class LicenseEncryptionService(IConfiguration configuration, ILogger<LicenseEncryptionService> logger) : ILicenseEncryptionService {
    private readonly string _encryptionKey = configuration["Licensing:EncryptionKey"] ?? 
        throw new InvalidOperationException("Licensing encryption key not configured");

    public string EncryptLicenseData(LicenseCacheData data) {
        try {
            var json = JsonSerializer.Serialize(data);
            var plainTextBytes = Encoding.UTF8.GetBytes(json);
            
            using var aes = Aes.Create();
            aes.Key = Convert.FromBase64String(_encryptionKey);
            aes.GenerateIV();
            
            using var encryptor = aes.CreateEncryptor();
            using var msEncrypt = new MemoryStream();
            msEncrypt.Write(aes.IV, 0, aes.IV.Length);
            using var csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write);
            csEncrypt.Write(plainTextBytes, 0, plainTextBytes.Length);
            csEncrypt.FlushFinalBlock();
            return Convert.ToBase64String(msEncrypt.ToArray());
        }
        catch (Exception ex) {
            logger.LogError(ex, "Failed to encrypt license data");
            throw;
        }
    }

    public LicenseCacheData DecryptLicenseData(string encryptedData) {
        try {
            var fullCipher = Convert.FromBase64String(encryptedData);
            
            using var aes = Aes.Create();
            aes.Key = Convert.FromBase64String(_encryptionKey);
            
            var iv = new byte[16];
            Array.Copy(fullCipher, 0, iv, 0, iv.Length);
            aes.IV = iv;
            
            using var decryptor = aes.CreateDecryptor();
            using var msDecrypt = new MemoryStream(fullCipher, iv.Length, fullCipher.Length - iv.Length);
            using var csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read);
            using var srDecrypt = new StreamReader(csDecrypt);
            var json = srDecrypt.ReadToEnd();
            return JsonSerializer.Deserialize<LicenseCacheData>(json) ?? throw new InvalidOperationException("Failed to deserialize license data");
        }
        catch (Exception ex) {
            logger.LogError(ex, "Failed to decrypt license data");
            throw;
        }
    }

    public string GenerateDataHash(LicenseCacheData data) {
        var json = JsonSerializer.Serialize(data);
        using var sha256 = SHA256.Create();
        var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(json));
        return Convert.ToBase64String(hash);
    }

    public bool ValidateDataHash(LicenseCacheData data, string hash) {
        var computedHash = GenerateDataHash(data);
        return computedHash == hash;
    }
}