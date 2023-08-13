namespace Service.Shared.Utils;

public class VersionInfo {
    public VersionInfo() {
    }
    public VersionInfo(string databaseVersion, string applicationVersion) {
        DatabaseVersion    = databaseVersion;
        ApplicationVersion = applicationVersion;
    }
    public string DatabaseVersion { get; set; }
    public string ApplicationVersion { get; set; }
}