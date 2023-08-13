using System;
using System.Globalization;
using SAPbobsCOM;
using Service.Shared.Data;
using Service.Shared.Utils;

#pragma warning disable 1587
/// <summary>
/// This namespace contains all the tools required to develop localized add-on /
/// application for SAP Business One with compatibility for MS SQL and SAP HANA
/// </summary>
/// <remarks>
/// Here is a diagram of the "Global" namespace
/// <img src="VSdocImages/Global\Global.cd"/>
/// </remarks>
#pragma warning restore 1587
namespace Service.Shared.Company; 

/// <summary>
/// This class contains all the connection objects to work with the SAP Business One DI API and UI API
/// </summary>
/// <remarks></remarks>
public static class ConnectionController {
    #region Variables

    private static string       connectionString;
    private static DatabaseType databaseType;
    private static string       database;
    private static string       server;
    private static string       dbServerPassword;
    private static string       dbServerUser;

    /// <summary>
    /// Shortcut to get company connected user name
    /// </summary>
    public static string UserName => Company.UserName;

    /// <summary>
    /// Shortcut to check if company object is connected
    /// </summary>
    public static bool IsConnected => Company.Connected;

    /// <summary>
    /// Gets or sets the DI API Company object
    /// </summary>
    public static SAPbobsCOM.Company Company { get; set; }

    /// <summary>
    /// Gets if the company is current in transaction
    /// </summary>
    public static bool InTransaction => Company?.InTransaction ?? false;

    /// <summary>
    /// Gets or sets the Database Server login user
    /// </summary>
    public static string DbServerUser {
        get => dbServerUser;
        set {
            dbServerUser     = value;
            connectionString = null;
        }
    }

    /// <summary>
    /// Gets or sets the Database Server login password
    /// </summary>
    public static string DbServerPassword {
        get => dbServerPassword;
        set {
            dbServerPassword = value;
            connectionString = null;
        }
    }

    /// <summary>
    /// Gets or sets the Server
    /// </summary>
    public static string Server {
        get => server;
        set {
            server           = value;
            connectionString = null;
        }
    }

    /// <summary>
    /// Gets or sets the Database
    /// </summary>
    public static string Database {
        get => database;
        set {
            database         = value;
            connectionString = null;
        }
    }

    /// <summary>
    /// Gets or sets the connection string to use with ADO.Net or HANA.Net Connection objects
    /// </summary>
    /// <value></value>
    /// <remarks>If the used get's the connection string and it's value is null it will automatically build the connection string using the DatabaseUser and Password properties and the Server and CompanyDB properties of the Company object<br/>
    /// This fields are encrypted and can only be set using the <see cref="StringUtils.EncryptData"/> function</remarks>
    public static string ConnectionString {
        get {
            if (!string.IsNullOrWhiteSpace(connectionString))
                return connectionString;

            if (string.IsNullOrWhiteSpace(Database))
                Database = Company.CompanyDB;
            if (string.IsNullOrWhiteSpace(Server))
                Server = Company.Server;
            connectionString = Data.ConnectionString.GetConnectionString(DatabaseType, Server, DbServerUser, DbServerPassword, Database);

            return connectionString;
        }
        set => connectionString = value;
    }

    /// <summary>
    /// Gets or sets the current database type. <br />
    /// This is set when the <see cref="ConnectCompany()"/> method is called but can also be set manually.
    /// </summary>
    public static DatabaseType DatabaseType {
        get => databaseType;
        set {
            databaseType = value;
            QueryHelper.SetValues();
            connectionString = null;
        }
    }

    /// <summary>
    /// Gets or sets the Format Number Culture Info
    /// </summary>
    /// <value></value>
    /// <remarks></remarks>
    public static CultureInfo NumberFormatInfo { get; set; } = new(CultureInfo.CurrentCulture.Name);

    /// <summary>
    /// Parse Number from SBO Dynamic fields Culture Info (Always en-US) since the Recordset Object and others always return decimal numbers as 123.56 format
    /// </summary>
    public static CultureInfo ParseFormatInfo { get; set; } = new("en-US");

    #endregion

    #region SAP


    /// <summary>
    /// This function is used to directly connect to a SAP Business One database through
    /// the DI API. <br />
    ///  This should be used when creating a Windows Forms, Windows Service or any other
    /// application type that does not uses the UI API.
    /// </summary>
    /// <param name="server">Server name / IP Address</param>
    /// <param name="dbServerType">Database server type</param>
    /// <param name="dbUser">Database server login user</param>
    /// <param name="dbPassword">Database server login password</param>
    /// <param name="dbName">Database (Company) name</param>
    /// <param name="sboUser">Company user name</param>
    /// <param name="sboPassword">Company password</param>
    /// <param name="licenseServer">SAP License Server name / IP Address.<br />
    ///  Optional. The default value is "". When using the default value it
    /// will use the Server name as the license server.</param>
    /// <example>
    ///   <para></para>
    ///   <code lang="C#"><![CDATA[ConnectionController.ConnectCompany("LAPTOP-K2UQK03D", BoDataServerTypes.dst_MSSQL2016, "sa", "password", "SBODemo_DE", "manager", "password", "LAPTOP-K2UQK03D");]]></code>
    ///   <para></para>
    /// </example>
    public static void ConnectCompany(string server, BoDataServerTypes dbServerType, string dbUser, string dbPassword, string dbName, string sboUser, string sboPassword, string licenseServer = "") {
        try {
            if (string.IsNullOrWhiteSpace(licenseServer))
                licenseServer = server;
            Company = new SAPbobsCOM.Company {
                Server       = server,
                DbServerType = dbServerType,
                CompanyDB    = dbName,
                UserName     = sboUser,
                Password     = sboPassword
            };
            int retVal = Company.Connect();
            if (retVal != 0)
                throw new Exception("Connection Error: " + Company.GetLastErrorDescription());
            Server           = server;
            Database         = dbName;
            DbServerUser     = dbUser;
            DbServerPassword = dbPassword;
            DatabaseType     = dbServerType != BoDataServerTypes.dst_HANADB ? DatabaseType.SQL : DatabaseType.HANA;
        }
        catch (Exception e) {
            if (e.Message.IndexOf("RPC_E_SERVERFAULT") != -1)
                ConnectCompany(server, dbServerType, dbUser, dbPassword, sboUser, sboPassword, licenseServer);
            else if (e.Message.IndexOf("-8037") != -1)
                ConnectCompany(server, dbServerType, dbUser, dbPassword, sboUser, sboPassword, licenseServer);
            else if (e.Message.IndexOf("-105") != -1)
                ConnectCompany(server, dbServerType, dbUser, dbPassword, sboUser, sboPassword, licenseServer);
            else
                throw new Exception(e.Message);
        }
    }

    /// <summary>
    /// This method is used to directly server the connection parameters. <br />
    /// This can be used for example an ActiveX component that required manually to set the connection parameters
    /// </summary>
    /// <param name="server">Server Name</param>
    /// <param name="database">Database Name</param>
    /// <param name="databaseType">Database Type</param>
    /// <param name="user">Server User Name</param>
    /// <param name="password">Server User Password</param>
    public static void SetDatabaseConnectionParameters(string server, string database, DatabaseType databaseType, string user, string password) {
        Server           = server;
        Database         = database;
        DatabaseType     = databaseType;
        DbServerUser     = user;
        DbServerPassword = password;
    }

    /// <summary>
    /// Begin company transaction
    /// </summary>
    public static void BeginTransaction() {
        Utils.Shared.ReleaseExecuteQuery();
        Company?.StartTransaction();
    }

    /// <summary>
    /// Commit company transaction
    /// </summary>
    public static void Commit() {
        if (Company is { InTransaction: true })
            Company.EndTransaction(BoWfTransOpt.wf_Commit);
        Utils.Shared.ReleaseExecuteQuery();
    }

    /// <summary>
    /// Rollback company transaction
    /// </summary>
    public static void Rollback() {
        if (Company is { InTransaction: true })
            Company?.EndTransaction(BoWfTransOpt.wf_RollBack);
        Utils.Shared.ReleaseExecuteQuery();
    }

    #endregion
}