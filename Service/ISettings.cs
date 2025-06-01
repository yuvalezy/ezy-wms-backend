namespace Service;

public class Settings : ISettings {
    public LoggingSettings           Logging           { get; set; } = new();
    public string                    AllowedHosts      { get; set; }
    public ConnectionStringsSettings ConnectionStrings { get; set; }
    public JwtSettings               Jwt               { get; set; }
    public Options                   Options           { get; set; }
    public SBOConnectionSettings     SBOConnection     { get; set; }
}

public class ISettings {
    public LoggingSettings           Logging           { get; set; } = new();
    public string                    AllowedHosts      { get; set; }
    public ConnectionStringsSettings ConnectionStrings { get; set; }
    public JwtSettings               Jwt               { get; set; }
    public Options                   Options           { get; set; }
}

public class LoggingSettings {
    public LogLevelSettings LogLevel { get; set; } = new();
}

public class LogLevelSettings {
    public string Default             { get; set; }
    public string MicrosoftAspNetCore { get; set; }
}

public class ConnectionStringsSettings {
    public required string DefaultConnection { get; set; }
}

public class JwtSettings {
    public string Key              { get; set; }
    public string Issuer           { get; set; }
    public string Audience         { get; set; }
    public int    ExpiresInMinutes { get; set; }
}

public class SBOConnectionSettings {
    public required string  Server              { get; set; }
    public          bool    TrustedConnection { get; set; }
    public          string? ServerUser      { get; set; }
    public          string? ServerPassword      { get; set; }
    public required string  User          { get; set; }
    public required string  Password          { get; set; }
}

public class Options {
    bool GRPODraft                           { get; }
    bool GRPOModificationsRequiredSupervisor { get; }
    bool GRPOCreateSupervisorRequired        { get; }
    bool TransferTargetItems                 { get; }
}