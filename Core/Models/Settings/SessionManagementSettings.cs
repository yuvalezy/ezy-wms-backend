using Core.Enums;

namespace Core.Models.Settings;

public class SessionManagementSettings {
    public SessionManagementType Type   { get; set; }
    public RedisSettings         Redis  { get; set; } = new();
    public CookieSettings        Cookie { get; set; } = new();
}

public class RedisSettings {
    public string? Host { get; set; }
    public int?    Port { get; set; }
}