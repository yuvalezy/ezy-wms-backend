using System.Security.Cryptography;

namespace Core.Utils;

public static class PasswordUtils {
    public static string HashPasswordWithSalt(string password) {
        // Generate a random salt
        byte[] salt = new byte[32]; // 256 bits
        using (var rng = RandomNumberGenerator.Create()) {
            rng.GetBytes(salt);
        }
        
        // Hash the password with salt using PBKDF2
        using (var pbkdf2 = new Rfc2898DeriveBytes(password, salt, 10000, HashAlgorithmName.SHA256)) {
            byte[] hash = pbkdf2.GetBytes(32); // 256 bits
            
            // Combine salt and hash
            byte[] hashBytes = new byte[64]; // 32 bytes salt + 32 bytes hash
            Array.Copy(salt, 0, hashBytes, 0, 32);
            Array.Copy(hash, 0, hashBytes, 32, 32);
            
            // Convert to base64 for storage
            return Convert.ToBase64String(hashBytes);
        }
    }
}