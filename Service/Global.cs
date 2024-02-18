using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading;
using Service.Models;
using Microsoft.Win32;
using SAPbobsCOM;
using Service.Shared;
using Service.Shared.Company;
using Service.Shared.Data;
using Service.Shared.Utils;
using Connection = Service.Shared.Company.ConnectionController;
using Path = System.IO.Path;
using Version = Service.Shared.Utils.Version;

namespace Service;

public static class Global {
    #region Variables & Properties

    public static  string          Database        { get; set; }
    public static  string          CompanyName     { get; set; }
    public static  bool            IsMain          { get; private set; }
    public static  int?            Port            { get; set; }
    public static  Service         Service         { get; set; }
    private static DataConnector   Data            { get; set; }
    public static  RestAPISettings RestAPISettings { get; private set; }
    public static  bool            Debug           { get; set; }

    //Connection Settings
    public static BoDataServerTypes ServerType { get; internal set; }

    //Database Settings
    public static string                               DBServiceVersion                    { get; set; }
    public static string                               User                                { get; set; }
    public static string                               Password                            { get; set; }
    public static bool                                 TestHelloWorld                      { get; private set; }
    public static bool                                 GRPODraft                           { get; private set; }
    public static bool                                 GRPOModificationsRequiredSupervisor { get; private set; }
    public static bool                                 GRPOCreateSupervisorRequired        { get; private set; }
    public static bool                                 PrintThread                         { get; private set; }
    public static bool                                 Background                          { get; set; }
    public static bool                                 Interactive                         { get; set; }
    public static bool                                 LoadBalancing                       { get; set; }
    public static ServiceNodes                         Nodes                               { get; set; }
    public static Dictionary<int, Authorization>       RolesMap                            { get; } = new();
    public static Dictionary<int, List<Authorization>> UserAuthorizations                  { get; } = new();

    #endregion

    #region Methods & Functions

    private static readonly  Mutex ConnectionMutex  = new(false, "connection");
    internal static readonly Mutex TransactionMutex = new(false, "transaction");

    public static void LoadArguments() {
        string[] args = Environment.GetCommandLineArgs();
        for (int i = 0; i < args.Length; i++) {
            switch (args[i]) {
                case "db:":
                    Connection.Database = args[++i];
                    break;
                case "port:":
                    Port = int.Parse(args[++i]);
                    break;
                case "background":
                    Background = true;
                    break;
                case "interactive":
                    Interactive = true;
                    break;
            }
        }

        IsMain = !Background && !Port.HasValue;
    }


    public static bool ConnectCompany() {
        ConnectionMutex.WaitOne();
        try {
            try {
                if (Connection.Company is { Connected: true })
                    return true;
            }
            catch (Exception) {
                // ignored
            }

            try {
                Connection.ConnectCompany(Connection.Server, ServerType, Connection.Database, User, Password, Connection.Server);
            }
            catch (Exception ex) {
                LogError("DI API connection error: " + ex.Message);
                return false;
            }
        }
        finally {
            ConnectionMutex.ReleaseMutex();
        }

        return true;
    }

    public static void LoadRegistrySettings() {
        var key = Registry.LocalMachine.OpenSubKey(Const.RegistryPath, false);
        if (key == null)
            throw new Exception($"Could not find Connection Parameters in the Windows Registry {Const.RegistryPath}");
        Connection.Server           = (string)key.GetValue("Server");
        Connection.Database         = Connection.Database;
        ServerType                  = (BoDataServerTypes)(int)key.GetValue("ServerType");
        Connection.DatabaseType     = ServerType == BoDataServerTypes.dst_HANADB ? DatabaseType.HANA : DatabaseType.SQL;
        Connection.DatabaseType     = ServerType == BoDataServerTypes.dst_HANADB ? DatabaseType.HANA : DatabaseType.SQL;
        Connection.DbServerUser     = ((string)key.GetValue("ServerUser")).DecryptString();
        Connection.DbServerPassword = ((string)key.GetValue("ServerPassword")).DecryptString();
        // LicenseServer               = (string)key.GetValue("LicenseServer");
        Data = DataObject;
    }

    public static DataConnector DataObject {
        get {
            string server     = Connection.Server;
            string dbUser     = Connection.DbServerUser;
            string dbPassword = Connection.DbServerPassword;
            string dbName     = Connection.Database;
            return ServerType switch {
                BoDataServerTypes.dst_HANADB => new HANADataConnector(ConnectionString.HanaConnectionString(server, dbUser, dbPassword, dbName)),
                _                            => new SQLDataConnector(ConnectionString.SqlConnectionString(server, dbUser, dbPassword, dbName))
            };
        }
    }


    public static void LoadDatabaseSettings() {
        if (IsMain)
            Service.LogInfo("Loading database settings");
        string sqlStr = Queries.DatabaseSettings;
        var    dr     = Data.GetDataTable(sqlStr).Rows[0];
        DBServiceVersion                    = dr["Version"].ToString();
        User                                = dr["User"].ToString().DecryptString();
        Password                            = dr["Password"].ToString().DecryptString();
        TestHelloWorld                      = dr["TestHelloWorld"].ToString() == "Y";
        GRPODraft                           = dr["GRPODraft"].ToString() == "Y";
        GRPOModificationsRequiredSupervisor = dr["GRPOModSup"].ToString().Equals("Y");
        GRPOCreateSupervisorRequired        = dr["GRPOCreateSup"].ToString().Equals("Y");
        CompanySettings.CrystalLegacy       = Convert.ToBoolean(dr["CrystalLegacy"]);
        CompanyName                         = (string)dr["CompanyName"];

        if (new BooleanSwitch("EnableTrace", "Enable Trace").Enabled || dr["DEBUG"].ToString() == "Y")
            Debug = true;
    }


    public static void LoadRestAPISettings() {
        string fileName = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Settings", $"{Connection.Database}.json");
        RestAPISettings = RestAPISettings.Load(fileName);
        LoadBalancing   = RestAPISettings.Enabled && RestAPISettings.LoadBalancing;
    }


    public static bool CheckVersion() {
        if (IsMain)
            LogInfo("Checking service version");
        try {
            var    version        = Assembly.GetExecutingAssembly().GetName().Version;
            string currentVersion = $"{version.Major}.{version.Minor}.{version.Build}";
            if (Version.CheckVersion(DBServiceVersion, currentVersion) != VersionCheck.Current) {
                LogError(
                    $"Cannot activate service for database \"{Connection.Database}\".\nDatabase version is {Connection.Database} and service version is {currentVersion}.");
                return false;
            }
        }
        catch (Exception ex) {
            LogError($"Error checking LW version in database \"{Connection.Database}\":\n{ex.Message}");
            return false;
        }

        return true;
    }

    public static void LogInfo(string message) => Service.LogInfo(message);

    // public static void   LogWarning(string message) => Service.LogWarning(message);
    public static void   LogError(string  message) => Service.LogError(message);
    public static Tracer GetTracer(string id)      => !Debug ? null : new Tracer($"lw_service_{id}", 10, true);

    #endregion

    public static void Load() {
        LoadRoles();
    }

    private static void LoadRoles() {
        LogInfo("Loading roles");
        const string sqlStr = "select \"typeID\", \"name\" from OHTY";

        var dt = Data.GetDataTable(sqlStr);
        foreach (DataRow dr in dt.Rows) {
            int id = (int)dr["typeID"];
            switch ((string)dr["name"]) {
                case Const.GoodsReceipt:
                    RolesMap.Add(id, Authorization.GoodsReceipt);
                    break;
                case Const.GoodsReceiptSupervisor:
                    RolesMap.Add(id, Authorization.GoodsReceiptSupervisor);
                    break;
                case Const.Picking:
                    RolesMap.Add(id, Authorization.Picking);
                    break;
                case Const.PickingSupervisor:
                    RolesMap.Add(id, Authorization.PickingSupervisor);
                    break;
                case Const.Counting:
                    RolesMap.Add(id, Authorization.Counting);
                    break;
                case Const.CountingSupervisor:
                    RolesMap.Add(id, Authorization.CountingSupervisor);
                    break;
                case Const.Transfer:
                    RolesMap.Add(id, Authorization.Transfer);
                    break;
                case Const.TransferSupervisor:
                    RolesMap.Add(id, Authorization.TransferSupervisor);
                    break;
            }
        }
    }

    public static bool ValidateAuthorization(int employeeID, params Authorization[] roles) {
        if (!UserAuthorizations.ContainsKey(employeeID))
            LoadAuthorization(employeeID);
        return UserAuthorizations[employeeID].Intersect(roles).Any();
    }

    public static void LoadAuthorization(int empID) {
        if (!UserAuthorizations.ContainsKey(empID))
            UserAuthorizations.Add(empID, new List<Authorization>());
        var authorizations = UserAuthorizations[empID];
        authorizations.Clear();

        string sqlStr = $"select \"roleID\" from HEM6 where \"empID\" = {empID}";
        var    dt     = DataObject.GetDataTable(sqlStr);
        var    data   = dt.Rows.Cast<DataRow>().Select(dr => (int)dr["roleID"]);
        authorizations.AddRange(from roleID in data where RolesMap.ContainsKey(roleID) select RolesMap[roleID]);
    }
}