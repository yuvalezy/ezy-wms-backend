namespace Core.Models.Settings;

public class CookieSettings
{
    public string? Domain { get; set; }
    public bool Secure { get; set; } = false;
    public string SameSite { get; set; } = "Lax";
    public bool HttpOnly { get; set; } = true;
}