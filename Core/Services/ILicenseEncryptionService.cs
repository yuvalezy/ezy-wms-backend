using Core.Models;

namespace Core.Services;

public interface ILicenseEncryptionService {
    string             EncryptLicenseData(LicenseCacheData data);
    LicenseCacheData   DecryptLicenseData(string encryptedData);
    string             GenerateDataHash(LicenseCacheData data);
    bool               ValidateDataHash(LicenseCacheData data, string hash);
}