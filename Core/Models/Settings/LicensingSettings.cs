namespace Core.Models.Settings;

public class LicensingSettings {
    public string                EncryptionKey        { get; set; } = string.Empty;
    public string                CloudEndpoint        { get; set; } = string.Empty;
    public string                BearerToken          { get; set; } = string.Empty;
    public int                   CacheExpirationHours { get; set; } = 24;
    public PaymentAlertSettings  PaymentAlert         { get; set; } = new();
}

/// <summary>
/// Configurable spec for the user-facing payment / account-status alerts the WMS
/// surfaces. WMS does not own grace (that is computed upstream by MyEzy and mirrored
/// as <c>expirationDate</c>); these knobs only control <em>when</em>, <em>to whom</em>,
/// and <em>whether</em> the WMS shows the alert. Delivered to the frontend on the
/// <c>CompanyInfo</c> payload. Defaults reproduce the previously-hardcoded behavior
/// exactly (3 / 30 / superuser / vague login text).
/// </summary>
public class PaymentAlertSettings {
    /// <summary>Master switch. When false, no payment banners are shown at all.</summary>
    public bool   Enabled                  { get; set; } = true;

    /// <summary>Days-before-expiration window for the login (pre-auth) banner. Was the FE const LOGIN_ACCOUNT_STATUS_WARNING_DAYS.</summary>
    public int    LoginWarnDays            { get; set; } = 3;

    /// <summary>Days-before-expiration window for the authenticated (in-app) banner. Was the FE const AUTHENTICATED_ACCOUNT_STATUS_WARNING_DAYS.</summary>
    public int    AuthenticatedWarnDays    { get; set; } = 30;

    /// <summary>Who sees the authenticated banner: "superuser" (default, today's behavior) or "all" (also warehouse operators).</summary>
    public string Audience                 { get; set; } = "superuser";

    /// <summary>When true, the login banner shows the specific payment message instead of the vague generic warning.</summary>
    public bool   ShowPaymentDetailAtLogin { get; set; } = false;
}
