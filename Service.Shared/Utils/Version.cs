using System;

namespace Service.Shared.Utils; 

/// <summary>
/// This utility is used to do version comparison operations
/// </summary>
/// <example>
///   <code lang="C#"><![CDATA[var check = UtilsVersion.CheckVersion(dbVersion, CurrentVersion);
///if (check == VersionCheck.Newer) {
/// //do something...
///}
///else if (check == VersionCheck.Older) {
/// //do
///}]]></code>
/// </example>
public static class Version {

    /// <summary>
    /// Compare database version against the application version
    /// </summary>
    /// <param name="info">Objects where the current database version and application version</param>
    /// <returns>Returns version check enumeration value</returns>
    public static VersionCheck CheckVersion(VersionInfo info) => CheckVersion(info.DatabaseVersion, info.ApplicationVersion);

    /// <summary>
    /// Function that compares version a with version
    /// </summary>
    /// <param name="version">Version to compare from</param>
    /// <param name="compare">Version to compare to</param>
    /// <returns>
    ///   <list type="number">
    ///     <item>
    ///       <description>If both versions are the same the return value will be VersionCheck.Current</description>
    ///     </item>
    ///     <item>
    ///       <description>If the compare version is more then version the return value will be VersionCheck.Newer</description>
    ///     </item>
    ///     <item>
    ///       <description>If the compare version is less then version the return value will be VersionCheck.Older</description>
    ///     </item>
    ///   </list>
    /// </returns>
    public static VersionCheck CheckVersion(string version, string compare) {
        if (string.IsNullOrWhiteSpace(version))
            version = "0.0.0.0";
            
        string[] arrReq  = version.Split(".".ToCharArray());
        string[] arrComp = compare.Split(".".ToCharArray());

        int r = Convert.ToInt32(arrReq[0]);
        int c = Convert.ToInt32(arrComp[0]);

        //Major
        var retVal = CheckVersionPart(r, c);
        //Minor
        if (retVal == VersionCheck.Current) {
            r      = Convert.ToInt32(arrReq[1]);
            c      = Convert.ToInt32(arrComp[1]);
            retVal = CheckVersionPart(r, c);
        }

        //Revision
        if (retVal == VersionCheck.Current) {
            r      = Convert.ToInt32(arrReq[2]);
            c      = Convert.ToInt32(arrComp[2]);
            retVal = CheckVersionPart(r, c);
        }

        return retVal;

        //Version check function, this is only used inside the CheckVersion function
        VersionCheck CheckVersionPart(int db, int app) {
            if (db > app)
                return VersionCheck.Older;
            if (db < app)
                return VersionCheck.Newer;
            return VersionCheck.Current;
        }
    }

    /// <summary>
    /// Convert System.Version value to Formatted string
    /// </summary>
    /// <param name="version">Version to be converted to formatted version string</param>
    /// <returns>
    /// Version Object:
    /// {Version.Major}.{Version.Minor}.{Version.Build}
    /// </returns>
    /// <example>
    ///   <code lang="C#"><![CDATA[string version =
    /// UtilsVersion.FormatVersion(System.Reflection.Assembly.GetExecutingAssembly().GetName().Version);]]></code>
    /// </example>
    public static string FormatVersion(System.Version version) => $"{version.Major}.{version.Minor}.{version.Build}";
}