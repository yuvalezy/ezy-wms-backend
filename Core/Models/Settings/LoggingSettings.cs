namespace Core.Models.Settings;

public class LoggingSettings {
    public LogLevelSettings LogLevel { get; set; } = new();
}