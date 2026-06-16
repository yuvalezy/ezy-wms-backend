using System.Security.Cryptography;
using System.Text;
using Core.Configuration;

namespace Infrastructure.Configuration;

/// <summary>
/// Encrypts/decrypts individual secret leaf values stored inside configuration
/// section JSON. AES-CBC with a random IV prepended, base64-encoded, and tagged
/// with <see cref="ConfigSectionCatalog.EncryptedPrefix"/> so the operation is
/// idempotent and the provider can decrypt marker-tagged values without a catalog.
///
/// Mirrors the key handling of <c>LicenseEncryptionService</c> (base64 AES key
/// from <c>Licensing:EncryptionKey</c>). Constructible before the DI container is
/// built, so the configuration provider can use it during <c>Load()</c>.
/// </summary>
public sealed class ConfigSecretProtector {
    private readonly byte[]? key;

    public ConfigSecretProtector(string? base64Key) {
        if (!string.IsNullOrWhiteSpace(base64Key)) {
            try {
                key = Convert.FromBase64String(base64Key);
            }
            catch (FormatException) {
                key = null; // invalid key -> passthrough (logged by callers)
            }
        }
    }

    /// <summary>True when a usable encryption key is configured.</summary>
    public bool IsEnabled => key is not null;

    /// <summary>Encrypts a plaintext value, returning a marker-tagged ciphertext. Idempotent.</summary>
    public string Protect(string plaintext) {
        if (key is null || ConfigSectionCatalog.IsEncrypted(plaintext)) {
            return plaintext;
        }

        using var aes = Aes.Create();
        aes.Key = key;
        aes.GenerateIV();

        using var encryptor = aes.CreateEncryptor();
        byte[] plainBytes = Encoding.UTF8.GetBytes(plaintext);
        byte[] cipher     = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);

        byte[] combined = new byte[aes.IV.Length + cipher.Length];
        Buffer.BlockCopy(aes.IV, 0, combined, 0, aes.IV.Length);
        Buffer.BlockCopy(cipher, 0, combined, aes.IV.Length, cipher.Length);

        return ConfigSectionCatalog.EncryptedPrefix + Convert.ToBase64String(combined);
    }

    /// <summary>Decrypts a marker-tagged value. Returns the input unchanged if not tagged.</summary>
    public string Unprotect(string value) {
        if (key is null || !ConfigSectionCatalog.IsEncrypted(value)) {
            return value;
        }

        try {
            byte[] combined = Convert.FromBase64String(value[ConfigSectionCatalog.EncryptedPrefix.Length..]);

            using var aes = Aes.Create();
            aes.Key = key;

            byte[] iv = new byte[16];
            Buffer.BlockCopy(combined, 0, iv, 0, iv.Length);
            aes.IV = iv;

            using var decryptor = aes.CreateDecryptor();
            byte[] plain = decryptor.TransformFinalBlock(combined, iv.Length, combined.Length - iv.Length);
            return Encoding.UTF8.GetString(plain);
        }
        catch (CryptographicException) {
            return value;
        }
        catch (FormatException) {
            return value;
        }
    }
}
