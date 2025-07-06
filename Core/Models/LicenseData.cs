using Core.Enums;

namespace Core.Models;

public class LicenseData {
    public AccountState                AccountStatus        { get; set; }
    public DateTime?                   ExpirationDate       { get; set; }
    public DateTime?                   PaymentCycleDate     { get; set; }
    public DateTime?                   DemoExpirationDate   { get; set; }
    public string?                     InactiveReason       { get; set; }
    public int                         MaxAllowedDevices    { get; set; }
    public int                         ActiveDeviceCount    { get; set; }
    public Dictionary<string, object>  AdditionalData       { get; set; } = new();
}