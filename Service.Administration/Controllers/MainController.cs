using System;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.ServiceProcess;
using System.Threading.Tasks;
using System.Windows.Forms;
using Service.Shared;
using Service.Administration.Helpers;
using Service.Administration.Views;
using Microsoft.Win32;
using Newtonsoft.Json;
using SAPbobsCOM;
using Service.Shared.Company;
using Service.Shared.Data;
using Service.Shared.Utils;
using Version = Service.Shared.Utils.Version;

namespace Service.Administration.Controllers;

public class MainController {
    #region Variables

    private readonly IMain        view;
    private readonly IWin32Window owner;

    private int               loadRegCount;
    private string            server;
    private BoDataServerTypes serverType;
    private string            serverUser;
    private string            serverPassword;

    private string licenseServer;

    private DataConnector data;

    private DataTable dt;
    private DataView  dv;

    #endregion

    public MainController(IMain view, IWin32Window owner) {
        this.view  = view;
        this.owner = owner;
    }


    #region Load

    public void LoadData() {
        try {
            LoadRegistry();
        }
        catch (Exception ex) {
            Error("Error loading registry data: " + ex.Message);
            view.MinimizeForm();
        }

        try {
            LoadDatabases();
        }
        catch (Exception ex) {
            Error("Error loading databases data: " + ex.Message);
            view.MinimizeForm();
        }

        view.ServerName =  server;
        view.Text       += $" (v{ServiceVersion})";
    }

    private static string ServiceVersion {
        get {
            var    version = Assembly.GetExecutingAssembly().GetName().Version;
            string value   = $"{version.Major}.{version.Minor}.{version.Build}";
            if (version.Revision > 0)
                value += $".{version.Revision}";
            return value;
        }
    }

    private void LoadDatabases() {
        dv?.Dispose();
        dt?.Dispose();
        dt = data.GetDataTable(Queries.Load);
        dt.Columns.Add("Active", Type.GetType("System.String"));
        dt.Columns.Add("Account", Type.GetType("System.String"));
        dt.Columns.Add("Version", Type.GetType("System.String"));
        dt.Columns.Add("Status", Type.GetType("System.String"));
        dt.Columns.Add("StartStop", Type.GetType("System.String"));
        dt.Columns.Add("Restart", Type.GetType("System.String"));
        LoadActiveStatus();
        Task.Run(LoadVersions);
        FilterData();
        var services = ServiceController.GetServices();
        foreach (DataRowView drv in dv) {
            var service = services.FirstOrDefault(v => v.ServiceName == $"{Const.ServiceName}|{(string)drv["Name"]}");
            UpdateServiceAccountInfo(drv.Row);
            UpdateStatusInfo(drv.Row, service);
        }

        if (dv.Count == 0)
            view.ActiveOnly = false;
    }

    private static string ActiveStatusFile => Path.Combine(Application.StartupPath, "Settings", "active_databases.json");

    private void LoadActiveStatus() {
        string[] active = Array.Empty<string>();
        if (File.Exists(ActiveStatusFile)) {
            try {
                //read ActiveStatusFile into a string array
                string activeStatus = File.ReadAllText(ActiveStatusFile);
                //deserialize the string array into a List of strings
                active = JsonConvert.DeserializeObject<string[]>(activeStatus);
            }
            catch (Exception e) {
                MessageBox.Show(owner, "Error loading active databases json file: " + e.Message, "Load Active DatabaseS", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        foreach (DataRow dr in dt.Rows)
            dr["Active"] = active.Contains((string)dr["Name"]).ToYesNo();
    }

    private void LoadVersions() {
        foreach (DataRow dr in dt.Rows) {
            string dbName = (string)dr["Name"];
            try {
                string version = data.GetValue<string>($"select \"U_Version\" from \"{dbName}\"{QueryHelper.OtherDB}\"@LW_YUVAL08_COMMON\"");
                dr["Version"] = version;
            }
            catch {
                //ignore
            }
        }
    }

    private void LoadRegistry() {
        try {
            using (var key = Registry.LocalMachine.OpenSubKey(Const.RegistryPath, false)) {
                server         = (string)key.GetValue("Server");
                serverType     = (BoDataServerTypes)(int)key.GetValue("ServerType");
                serverUser     = ((string)key.GetValue("ServerUser")).DecryptString();
                serverPassword = ((string)key.GetValue("ServerPassword")).DecryptString();
                licenseServer  = (string)key.GetValue("LicenseServer");
            }

            switch (serverType) {
                case BoDataServerTypes.dst_HANADB:
                    data                                  = new HANADataConnector();
                    ConnectionController.ConnectionString = ConnectionString("SBOCOMMON");
                    break;
                default:
                    data                                  = new SQLDataConnector();
                    ConnectionController.ConnectionString = ConnectionString("SBO-Common");
                    break;
            }
        }
        catch (Exception ex) {
            if (loadRegCount++ == 0) {
                var frmConn = new Connection();
                if (frmConn.ShowDialog() == DialogResult.OK)
                    LoadRegistry();
                else {
                    Application.Exit();
                }
            }
            else {
                Error($"Error loading registry data: {ex.Message}");
                Application.Exit();
            }
        }
    }

    #endregion

    private DialogResult Question(string message) => MessageBox.Show(owner, message, view.Text, MessageBoxButtons.YesNo, MessageBoxIcon.Question);
    private void         Error(string    message) => MessageBox.Show(owner, message, view.Text, MessageBoxButtons.OK, MessageBoxIcon.Error);

    public void OpenAPISettings() {
        using var service = LoadController();
        if (service.Status == ServiceControllerStatus.Running) {
            var apiSettings = new APISettings(view.Database);
            if (apiSettings.ShowDialog(owner) != DialogResult.OK)
                return;
            var registration = new ServiceRegistration(true, view.Database, apiSettings.Settings, Error);
            registration.ReinstallNodes();
            service.ExecuteCommand(Const.ReloadRestAPISettings);
        }
        else
            MessageBox.Show("Cannot open printer settings without starting the service", view.Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
    }

    public void OpenPrintAPISettings() {
        using var service = LoadController();
        if (service.Status == ServiceControllerStatus.Running) {
            var apiSettings = new PrintAPISettings(DatabaseDataConnector, view.Database);
            if (apiSettings.ShowDialog(owner) != DialogResult.OK)
                return;
            service.ExecuteCommand(Const.ReloadRestAPISettings);
        }
        else
            MessageBox.Show("Cannot open printer settings without starting the service", view.Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
    }

    private DataConnector DatabaseDataConnector {
        get {
            DataConnector dbData = serverType switch {
                BoDataServerTypes.dst_HANADB => new HANADataConnector(ConnectionString(view.Database)),
                _                            => new SQLDataConnector(ConnectionString(view.Database))
            };
            return dbData;
        }
    }

    public void OpenServiceAccount() {
        var    dr             = dv[view.CurrentRow].Row;
        string db             = (string)dr["Name"];
        string dbName         = (string)dr["Desc"];
        string currentAccount = (string)dr["Account"];
        var    account        = new Account(db, dbName, currentAccount);
        account.AccountChanged += AccountChanged;
        account.ShowDialog(owner);
    }

    private void AccountChanged(AccountType accountType, string userName, string password) {
        if (accountType == AccountType.LocalSystem) {
            userName = "LocalSystem";
            password = string.Empty;
        }


        var dr = dv[view.CurrentRow].Row;
        try {
            bool isRunning = dr["Status"].ToString() == "Running";
            if (isRunning)
                StopService().Wait();

            Execute();
            var settings = RestAPISettings.Load(APISettings.SettingsFilePath(view.Database));
            try {
                if (!settings.LoadBalancing)
                    return;
                for (int i = 0; i <= settings.Nodes.Count; i++)
                    Execute(i);

                if (isRunning)
                    StartService();
            }
            finally {
                switch (accountType) {
                    case AccountType.LocalSystem:
                        userName = string.Empty;
                        password = string.Empty;
                        break;
                    case AccountType.Account:
                        userName = Encryption.EncryptToBase64(userName);
                        password = Encryption.EncryptToBase64(password);
                        break;
                }

                settings.AccountInfo = new AccountInfo {
                    Type     = accountType,
                    UserName = userName,
                    Password = password
                };
                settings.Save();
            }
        }
        catch (Exception e) {
            Error($"Error updating accounts: {e.Message}");
        }
        finally {
            dr["Account"] = accountType == AccountType.LocalSystem ? "LocalSystem" : userName;
        }

        void Execute(int? id = null) {
            try {
                string args  = $"CONFIG \"{Const.ServiceName}|{view.Database}{{0}}\" obj= \"{userName.ToQuery()}\" password= \"{password.ToQuery()}\"";
                string argID = string.Empty;
                if (id.HasValue)
                    argID = $"|{id}";
                var info = new ProcessStartInfo("sc", string.Format(args, argID)) {
                    WindowStyle = ProcessWindowStyle.Hidden
                };
                var process = new Process { StartInfo = info };
                process.Start();
                process.WaitForExit();
            }
            catch (Exception e) {
                Error($"Error settings service {id} account: {e.Message}");
            }
        }
    }

    private void CheckStatus(DataRow dr) {
        string dbName = (string)dr["Name"];
        try {
            using var sc = LoadController(dbName);
            UpdateStatusInfo(dr, sc);
        }
        catch (Exception ex) {
            Error($"Error checking service status for database \"{view.Database}\": {ex.Message}");
        }
    }

    public ServiceController LoadController(string dbName = null) => new($"{Const.ServiceName}|{dbName ?? view.Database}");

    private void UpdateServiceAccountInfo(DataRow dr) {
        string dbName = (string)dr["Name"];
        try {
            using var key     = Registry.LocalMachine.OpenSubKey($@"SYSTEM\CurrentControlSet\Services\{Const.ServiceName}|{dbName}");
            string    account = (string)key.GetValue("ObjectName");
            if (account.StartsWith(".\\"))
                account = $"{Environment.MachineName}\\{account.Substring(2)}";
            dr["Account"] = account;
        }
        catch (Exception e) {
            Error($"Error loading service \"{dbName}\" registry data: {e.Message}");
        }
    }

    private void UpdateStatusInfo(DataRow dr, ServiceController sc = null, bool validations = false, ServiceControllerStatus? status = null) {
        dr["StartStop"] = "";
        dr["Restart"]   = "";
        if (validations) {
            dr["Status"] = "Validating";
            return;
        }

        if (status == null && sc == null) {
            dr["Status"] = "Not Registered";
            return;
        }

        switch (status ?? sc.Status) {
            case ServiceControllerStatus.StartPending:
                dr["Status"] = "Starting";
                break;
            case ServiceControllerStatus.Running:
                dr["Status"]    = "Running";
                dr["StartStop"] = "Stop";
                dr["Restart"]   = "Restart";
                break;
            case ServiceControllerStatus.StopPending:
                dr["Status"] = "Stopping";
                break;
            default:
                dr["Status"]    = "Stopped";
                dr["StartStop"] = "Start";
                break;
        }
    }

    public void FilterData() {
        dv = new DataView(dt);
        if (view.ActiveOnly)
            dv.RowFilter = "Active = 'Y'";
        view.Source = dv;
    }

    public void RestartService() {
        StopService().Wait();
        StartService();
    }

    public void StartService() {
        int index = view.CurrentRow;
        var row   = dv[index].Row;
        UpdateStatusInfo(row, validations: true);
        Task.Run(() => {
            try {
                var       settings = RestAPISettings.Load(APISettings.SettingsFilePath(view.Database));
                using var sc       = LoadController();
                try {
                    if (sc.Status == ServiceControllerStatus.StopPending)
                        sc.WaitForStatus(ServiceControllerStatus.Stopped);
                    if (sc.Status == ServiceControllerStatus.StartPending) {
                        sc.WaitForStatus(ServiceControllerStatus.Running);
                        return;
                    }

                    if (sc.Status != ServiceControllerStatus.Stopped)
                        return;

                    var validation = new ServerValidation(view.Database, DatabaseDataConnector, settings, owner);
                    if (!validation.Execute()) {
                        UpdateStatusInfo(row, sc);
                        return;
                    }

                    sc.Start();
                    UpdateStatusInfo(row, sc, status: ServiceControllerStatus.StartPending);
                    sc.WaitForStatus(ServiceControllerStatus.Running);
                }
                finally {
                    UpdateStatusInfo(row, sc);
                }
            }
            catch (Exception ex) {
                Error($"Error starting service: {ex.Message}");
            }
        });
    }

    public Task StopService() =>
        Task.Run(() => {
            try {
                using var sc    = LoadController();
                int       index = view.CurrentRow;
                var       row   = dv[index].Row;
                try {
                    if (sc.Status == ServiceControllerStatus.StartPending)
                        sc.WaitForStatus(ServiceControllerStatus.Running);
                    bool waitForStop = false;
                    if (sc.Status == ServiceControllerStatus.Running) {
                        sc.Stop();
                        waitForStop = true;
                    }

                    UpdateStatusInfo(row, sc, status: ServiceControllerStatus.StopPending);
                    if (waitForStop)
                        sc.WaitForStatus(ServiceControllerStatus.Stopped);
                }
                finally {
                    UpdateStatusInfo(row, sc);
                }
            }
            catch (Exception ex) {
                Error($"Error stopping service: {ex.Message}");
            }
        });

    public void ChangeServiceActive() {
        if (!view.IsActive)
            Activate();
        else
            Deactivate();
    }

    private void Activate() {
        if (Question($"Are you sure you want to active LW Service for database \"{view.Database}\"?") != DialogResult.Yes)
            return;
        try {
            if (!ValidateDatabase())
                return;
            var settings     = RestAPISettings.Load(APISettings.SettingsFilePath(view.Database));
            var registration = new ServiceRegistration(true, view.Database, settings, Error);
            registration.Execute();
            if (settings.LoadBalancing)
                registration.AddRemoveNodes();
            var dr = dv[view.CurrentRow].Row;
            dr["Active"] = "Y";
            SaveActiveStatus();
            UpdateServiceAccountInfo(dr);
            CheckStatus(dr);
        }
        catch (Exception ex) {
            Error($"Could not change the service active status for database \"{view.Database}\": {ex.Message}");
        }

        view.SetIsActive(true);
        view.ClearSelection();
    }

    private void Deactivate() {
        if (Question($"Are you sure you want to de-active LW Service for database \"{view.Database}\"?") != DialogResult.Yes)
            return;
        var row = dv[view.CurrentRow].Row;
        try {
            if (ServerValidation.ExistsService($"{Const.ServiceName}|{view.Database}")) {
                using var service = LoadController();
                if (service.Status == ServiceControllerStatus.Running) {
                    service.Stop();
                    service.WaitForStatus(ServiceControllerStatus.Stopped);
                }
            }

            var registration = new ServiceRegistration(false, view.Database, Error);
            registration.Execute();
            row["Active"] = "N";
            SaveActiveStatus();
            registration.RemoveServices();
            row["Status"]    = "";
            row["StartStop"] = "";
            row["Restart"]   = "";
        }
        catch (Exception ex) {
            Error($"Error changing status for database \"{view.Database}\": {ex.Message}");
            return;
        }

        view.SetIsActive(false);
        view.ClearSelection();
    }

    private void SaveActiveStatus() {
        try {
            string[] active = dt.Rows.Cast<DataRow>().Where(v => (string)v["Active"] == "Y").Select(v => (string)v["Name"]).ToArray();
            File.WriteAllText(ActiveStatusFile, JsonConvert.SerializeObject(active));
        }
        catch (Exception e) {
            Error($"Error saving active status: " + e.Message);
        }
    }

    private bool ValidateDatabase() {
        try {
            DataConnector dbData = serverType switch {
                BoDataServerTypes.dst_HANADB => new HANADataConnector(Shared.Data.ConnectionString.HanaConnectionString(server, serverUser, serverPassword, view.Database)),
                _                            => new SQLDataConnector(Shared.Data.ConnectionString.SqlConnectionString(server, serverUser, serverPassword, view.Database))
            };
            var md = new MetaData(dbData);
            md.Check();
            var    v              = Assembly.GetExecutingAssembly().GetName().Version;
            string currentVersion = $"{v.Major}.{v.Minor}.{v.Build}";
            const string sqlStr = """
                                  select "U_Version" "Version", "U_User" "User", "U_Password" "Password" from "@LW_YUVAL08_COMMON"
                                  """;
            var    dr        = dbData.GetDataTable(sqlStr).Rows[0];
            string dbVersion = (string)dr["Version"];
            
            string dbUser    = dr["User"].ToString();
            try {
                if (!string.IsNullOrWhiteSpace(dbUser))
                    dbUser = dbUser.DecryptString();
            }
            catch {
                string encryptedUser = dbUser.EncryptData();
                dbData.Execute($"update \"@LW_YUVAL08_COMMON\" set \"U_User\" = '{encryptedUser.ToQuery()}'");
            }

            string dbPassword = dr["Password"].ToString();
            try {
                if (!string.IsNullOrWhiteSpace(dbPassword))
                    dbPassword = dbPassword.DecryptString();
            }
            catch {
                string encryptedPassword = dbPassword.EncryptData();
                dbData.Execute($"update \"@LW_YUVAL08_COMMON\" set \"U_Password\" = '{encryptedPassword.ToQuery()}'");
            }
            
            if (Version.CheckVersion(dbVersion, currentVersion) != VersionCheck.Current) {
                Error($"Cannot activate service for database \"{view.Database}\".\nDatabase version is {dbVersion} and service version is {currentVersion}.");
                return false;
            }

            if (!ValidateDBUser(dbUser, dbPassword, out string errorMessage)) {
                Error(
                    $"""
                     Cannot activate service for database "{view.Database}".
                     Database service user or password is not valid
                     Update database service password and try again.
                     Error: {errorMessage}
                     """);
                //todo popoup to ask new user
                return false;
            }
        }
        catch (Exception ex) {
            Error($"Error checking LW version in database \"{view.Database}\":\n{ex.Message}");
            return false;
        }
        finally {
            data?.Dispose();
        }

        return true;
    }

    private bool ValidateDBUser(string dbUser, string dbPassword, out string errorMessage) {
        errorMessage = string.Empty;
        var cmp = new Company {
            CompanyDB    = view.Database,
            Server       = server,
            DbServerType = serverType,
            UserName     = dbUser,
            Password     = dbPassword,
            language     = BoSuppLangs.ln_Spanish_La
        };
        try {
            int lRetCode = cmp.Connect();
            if (lRetCode != 0) {
                errorMessage = cmp.GetLastErrorDescription();
                return false;
            }
        }
        finally {
            cmp.Disconnect();
            Marshal.ReleaseComObject(cmp);
            GC.Collect();
        }

        return true;
    }

    public void ReloadSettings() {
        using var service = LoadController();
        if (service.Status == ServiceControllerStatus.Running)
            service.ExecuteCommand(Const.ReloadSettings);
        else
            MessageBox.Show("Cannot run re test process without starting the service", view.Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
    }

    private string ConnectionString(string dbName) =>
        Service.Shared.Data.ConnectionString.GetConnectionString(serverType, server, serverUser, serverPassword, dbName, applicationName: "Light WMS Service");
}