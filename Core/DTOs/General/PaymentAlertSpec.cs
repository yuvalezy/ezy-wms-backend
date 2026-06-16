namespace Core.DTOs.General;

/// <summary>
/// FE-facing projection of the configurable payment-alert spec (sourced from
/// <c>LicensingSettings.PaymentAlert</c>). Delivered on <see cref="CompanyInfoResponse"/>
/// so the frontend's banner gates are config-driven instead of relying on hardcoded
/// constants. Absent on older backends ⇒ the FE falls back to its built-in defaults
/// (3 / 30 / superuser / vague login), so behavior is unchanged.
/// </summary>
public class PaymentAlertSpec {
    public bool   Enabled                  { get; set; }
    public int    LoginWarnDays            { get; set; }
    public int    AuthenticatedWarnDays    { get; set; }
    public string Audience                 { get; set; } = "superuser";
    public bool   ShowPaymentDetailAtLogin { get; set; }
}
