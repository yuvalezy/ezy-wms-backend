namespace Core.Entities;

public class LicenseCache : BaseEntity {
    public string   EncryptedData        { get; set; } = string.Empty; // JSON encrypted license data
    public DateTime CacheTimestamp       { get; set; }
    public DateTime ExpirationTimestamp  { get; set; }
    public string?  DataHash             { get; set; } // For integrity verification
}