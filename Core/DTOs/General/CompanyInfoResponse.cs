using Core.DTOs.License;
using Core.Enums;

namespace Core.DTOs.General;

public class CompanyInfoResponse {
    public string?              CompanyName     { get; set; }
    public DateTime             ServerTime      { get; set; }
    public List<LicenseWarning> LicenseWarnings { get; set; } = [];
    public DeviceStatus?        DeviceStatus    { get; set; }
    public AccountState?        AccountStatus   { get; set; }
    // Coalesced license/demo expiry (or payment-due grace deadline) so the UI can
    // surface the date for any license that carries one — not just demo accounts.
    public DateTime?            ExpirationDate  { get; set; }
    // Configurable payment-alert spec (windows + audience + enabled) so the FE's
    // banner gates are driven by appsettings instead of hardcoded constants.
    // Optional: absent on older backends ⇒ FE uses its built-in defaults.
    public PaymentAlertSpec?    PaymentAlert    { get; set; }
}