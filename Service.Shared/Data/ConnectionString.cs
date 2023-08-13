using SAPbobsCOM;

namespace Service.Shared.Data; 

/// <summary>
/// This utility contains helper to generate connection strings for SqlConnection and HanaConnection objects functions
/// </summary>
public static class ConnectionString {
    /// <summary>
    /// This method is used to generate a connection string
    /// </summary>
    /// <param name="type">Database Server Type</param>
    /// <param name="server">Database Server Network Name or Address</param>
    /// <param name="user">Database Server User Name</param>
    /// <param name="password">Database Server User Password</param>
    /// <param name="dbName">Database Name</param>
    /// <param name="applicationName">Application Name</param>
    public static string GetConnectionString(BoDataServerTypes type, string server, string user, string password, string dbName, string applicationName = null) =>
        type switch {
            BoDataServerTypes.dst_HANADB => HanaConnectionString(server, user, password, dbName),
            _                            => SqlConnectionString(server, user, password, dbName, applicationName: applicationName)
        };

    /// <summary>
    /// This method is used to generate a connection string
    /// </summary>
    /// <param name="type">Database Server Type</param>
    /// <param name="server">Database Server Network Name or Address</param>
    /// <param name="user">Database Server User Name</param>
    /// <param name="password">Database Server User Password</param>
    /// <param name="dbName">Database Name</param>
    /// <param name="odbc">Defines if the connection string will be used for the OdbcConnection object</param>
    /// <example>
    ///   <code lang="C#"><![CDATA[string connStr = GetConnectionString(DatabaseType.SQL, "localhost", "sa", "password", "SBODemoDE")]]></code>
    ///   <para>Return value will be "Server=localhost;uid=sa;pwd=password;Initial Catalog=SBODemoDE"</para>
    /// </example>
    public static string GetConnectionString(DatabaseType type, string server, string user, string password, string dbName, bool odbc = false) =>
        type switch {
            DatabaseType.HANA => HanaConnectionString(server, user, password, dbName, odbc: odbc),
            _                 => SqlConnectionString(server, user, password, dbName)
        };

    /// <summary>
    /// This method is used to generate a connection string
    /// </summary>
    /// <param name="server">Database Server Network Name or Address</param>
    /// <param name="user">Database Server User Name</param>
    /// <param name="password">Database Server User Password</param>
    /// <param name="dbName">Database Name</param>
    /// <param name="trustedConnection">Use Trusted Connection</param>
    /// <param name="applicationName">Application Name</param>
    /// <param name="odbc">Defines if the connection string will be used for the OdbcConnection object</param>
    /// <example>
    ///   <code lang="C#"><![CDATA[ string connStr = HanaConnectionString("ndb@localhost:30015", "sa", "password", "SBODemoDE")]]></code>
    ///   <para>Return value will be "Server=localhost:30015;UID=SYSTEM;PWD=password;CS=SBODemoDE;DATABASENAME=NDB"</para>
    /// </example>
    public static string HanaConnectionString(string server, string user, string password, string dbName = null, bool trustedConnection = false, string applicationName = null, bool odbc = false) {
        string tenantName = null;
        if (server.IndexOf("@") != -1) {
            string[] arr = server.Split('@');
            tenantName = arr[0];
            server     = arr[1];
        }

        string connStr = !odbc ? $"Server={server};" : $"driver={{HDBODBC}};ServerNode={server};";

        if (!trustedConnection)
            connStr += $"UID={user};PWD={password};";
        //else
        //    connStr += "Integrated Security=SSPI;";
        if (!string.IsNullOrWhiteSpace(dbName))
            connStr += $"CS={dbName};";
        //if (!string.IsNullOrWhiteSpace(applicationName))
        //    connStr += $"Application Name={applicationName};";

        if (!string.IsNullOrWhiteSpace(tenantName))
            connStr += $"DatabaseName={tenantName};";

        return connStr;
    }

    /// <summary>
    /// This method is used to generate a connection string
    /// </summary>
    /// <param name="server">Database Server Network Name or Address</param>
    /// <param name="user">Database Server User Name</param>
    /// <param name="password">Database Server User Password</param>
    /// <param name="dbName">Database Name</param>
    /// <param name="trustedConnection">Use Trusted Connection</param>
    /// <param name="applicationName">Application Name</param>
    /// <example>
    /// <code lang="C#"><![CDATA[ string connStr = SqlConnectionString("localhost", "sa", "password", "SBODemoDE")]]></code>
    /// <para>Return value will be &quot;Server=localhost;uid=sa;pwd=password;Initial Catalog=SBODemoDE&quot;</para>
    /// </example>
    public static string SqlConnectionString(string server, string user, string password, string dbName = null, bool trustedConnection = false, string applicationName = null) {
        string connStr = $"Server={server};";
        if (!trustedConnection)
            connStr += $"uid={user};pwd={password};";
        else
            connStr += "Integrated Security=SSPI;";
        if (!string.IsNullOrWhiteSpace(dbName))
            connStr += $"Initial Catalog={dbName};";
        if (!string.IsNullOrWhiteSpace(applicationName))
            connStr += $"Application Name={applicationName};";
        return connStr;
    }
}