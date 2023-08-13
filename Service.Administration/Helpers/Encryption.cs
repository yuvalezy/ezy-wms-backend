using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace Service.Administration.Helpers;

public class Encryption {
    private const string Key = "5DbQ2%s9Q9KcY0Wi3vmr";

    public static string EncryptToBase64(string originalText) {
        byte[]             userBytes = Encoding.UTF8.GetBytes(originalText); // UTF8 saves Space
        byte[]             userHash  = MD5.Create().ComputeHash(userBytes);
        SymmetricAlgorithm crypt     = Aes.Create();                       // (Default: AES-CCM (Counter with CBC-MAC))
        crypt.Key = MD5.Create().ComputeHash(Encoding.UTF8.GetBytes(Key)); // MD5: 128 Bit Hash
        crypt.IV  = new byte[16];                                          // by Default. IV[] to 0.. is OK simple crypt
        using var memoryStream = new MemoryStream();
        using var cryptoStream = new CryptoStream(memoryStream, crypt.CreateEncryptor(), CryptoStreamMode.Write);
        cryptoStream.Write(userBytes, 0, userBytes.Length); // User Data
        cryptoStream.Write(userHash, 0, userHash.Length);   // Add HASH
        cryptoStream.FlushFinalBlock();
        string resultString = Convert.ToBase64String(memoryStream.ToArray());
        return resultString;
    }

    public static string DecryptFromBase64(string encryptedText) {
        byte[]             encryptedBytes = Convert.FromBase64String(encryptedText);
        SymmetricAlgorithm crypt          = Aes.Create();
        crypt.Key = MD5.Create().ComputeHash(Encoding.UTF8.GetBytes(Key));
        crypt.IV  = new byte[16];
        using var memoryStream = new MemoryStream();
        using var cryptoStream = new CryptoStream(memoryStream, crypt.CreateDecryptor(), CryptoStreamMode.Write);
        cryptoStream.Write(encryptedBytes, 0, encryptedBytes.Length);
        cryptoStream.FlushFinalBlock();
        byte[] allBytes = memoryStream.ToArray();
        int    userLen  = allBytes.Length - 16;
        if (userLen < 0) throw new Exception("Invalid Len"); // No Hash?
        byte[] userHash = new byte[16];
        Array.Copy(allBytes, userLen, userHash, 0, 16); // Get the 2 Hashes
        byte[] decryptHash = MD5.Create().ComputeHash(allBytes, 0, userLen);
        if (userHash.SequenceEqual(decryptHash) == false) throw new Exception("Invalid Hash");
        string resultString = Encoding.UTF8.GetString(allBytes, 0, userLen);
        return resultString;
    }
}