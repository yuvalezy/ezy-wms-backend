using Core.Enums;

namespace Core.Models;

public class LicenseCacheData {
    public AccountState                    AccountStatus             { get; set; }
    public DateTime?                       ExpirationDate            { get; set; }
    public DateTime?                       PaymentCycleDate          { get; set; }
    public DateTime?                       DemoExpirationDate        { get; set; }
    public string?                         InactiveReason            { get; set; }
    public DateTime                        LastValidationTimestamp   { get; set; }
    public int                             ActiveDeviceCount         { get; set; }
    public int                             MaxAllowedDevices         { get; set; }
    public Dictionary<string, object>      AdditionalData            { get; set; } = new Dictionary<string, object>();
}