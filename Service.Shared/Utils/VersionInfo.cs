namespace Service.Shared.Utils;

/// <summary>
/// This class is used as parameter object for <see cref="BE1S.Utils.Version" /> methods.
/// </summary>
public class VersionInfo {
    /// <summary>
    /// Initializes a new instance of the <see cref="VersionInfo" /> class.
    /// </summary>
    public VersionInfo() {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="VersionInfo" /> class.
    /// </summary>
    /// <param name="databaseVersion">Database Version</param>
    /// <param name="applicationVersion">Application Version</param>
    public VersionInfo(string databaseVersion, string applicationVersion) {
        DatabaseVersion    = databaseVersion;
        ApplicationVersion = applicationVersion;
    }

    /// <summary>
    /// Database Version (Major, Minor, Build)
    /// </summary>
    public string DatabaseVersion { get; set; }

    /// <summary>
    /// Application Version (Major, Minor, Build)
    /// </summary>
    public string ApplicationVersion { get; set; }
}