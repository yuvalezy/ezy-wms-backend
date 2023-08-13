using System;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Windows.Forms;
using Service.Shared;
using Service.Shared.Utils;
using Path = System.IO.Path;

namespace Service.Administration.Helpers;

public class ServiceRegistration {
    private readonly string          appPath = Path.GetDirectoryName(Application.ExecutablePath);
    private readonly string          dbName;
    private readonly RestAPISettings settings;
    private readonly Action<string>  error;

    private bool active;

    public ServiceRegistration(bool active, string dbName, Action<string> error) {
        this.active = active;
        this.dbName = dbName;
        this.error  = error;
        if (active)
            throw new Exception("RestAPISettings parameter is mandatory for active registration!");
    }

    public ServiceRegistration(bool active, string dbName, RestAPISettings settings, Action<string> error) {
        this.active   = active;
        this.dbName   = dbName;
        this.settings = settings;
        this.error    = error;
        if (active && settings == null)
            throw new Exception("RestAPISettings parameter is mandatory for active registration!");
    }

    public void Execute() {
        try {
            AddRemoveService();
        }
        catch (Exception ex) {
            error($"Error {(!active ? "un-" : "")}installing data service for database \"{dbName}\"\n{ex.Message}");
        }
    }

    public void RemoveServices() {
        var services = ServerValidation.GetServices(dbName);
        foreach (var service in services) {
            int id = int.Parse(service.ServiceName.Split('|').Last());
            if (service.Status == ServiceControllerStatus.StartPending)
                service.WaitForStatus(ServiceControllerStatus.Running);
            if (service.Status == ServiceControllerStatus.Running) {
                try {
                    service.Stop();
                    service.WaitForStatus(ServiceControllerStatus.Stopped);
                }
                catch {
                    //ignore
                }

                service.Dispose();
            }

            AddRemoveService((id, 0));
        }

        foreach (var service in services)
            service.Dispose();
    }

    public void ReinstallNodes() {
        active = false;
        AddRemoveNodes();
        active = true;
        AddRemoveNodes();
    }

    public void AddRemoveNodes() {
        RemoveServices();
        string lb = string.Empty;
        if (settings.LoadBalancing) {
            AddRemoveService(background: true);

            for (int i = 0; i < settings.Nodes.Count; i++)
                AddRemoveService((i + 1, settings.Nodes[i].Port));
            lb = " Master";
        }

        string args = $"config \"{Const.ServiceName}|{dbName}\" DisplayName= \"Light WMS Service - {dbName}{lb}\"";
        var    info = GetCreateInfo(args);
        var process = new Process {
            StartInfo = info
        };
        process.Start();
        process.WaitForExit();
    }

    private void AddRemoveService((int ID, int Port)? node = null, bool background = false) {
        var    args        = new StringBuilder();
        string serviceName = AddRemoveServiceName(node, background, dbName);
        if (active)
            AddServiceCommand(node, background, args, serviceName);
        else
            args.Append($"delete \"{serviceName}\"");

        var info = GetCreateInfo(args.ToString());
        var process = new Process {
            StartInfo = info
        };
        process.Start();
        process.WaitForExit();
    }

    private static string AddRemoveServiceName((int ID, int Port)? node, bool background, string dbName) {
        string serviceName = $"{Const.ServiceName}|{dbName}";
        if (node.HasValue)
            serviceName += $"|{node.Value.ID}";
        if (background)
            serviceName += "|0";
        return serviceName;
    }

    private void AddServiceCommand((int ID, int Port)? node, bool background, StringBuilder args, string serviceName) {
        args.Append($"create \"{serviceName}\"");
        args.Append($" binpath=\"{appPath}\\LMService.exe ");
        args.Append("db: ");
        args.Append(dbName);
        if (node.HasValue) {
            args.Append(" port: ");
            args.Append(node.Value.Port);
        }

        if (background)
            args.Append(" background");

        args.Append("\" ");
        args.Append("displayname=\"Light WMS Service - ");
        args.Append(dbName);
        if (node.HasValue) {
            args.Append(" Node ");
            args.Append(node.Value.ID);
        }

        if (background)
            args.Append(" Operations");

        args.Append("\"");
        if (node == null && !background)
            args.Append(" start=delayed-auto");

        if (settings.AccountInfo is not { Type: AccountType.Account }) 
            return;
        string userName = Encryption.DecryptFromBase64(settings.AccountInfo.UserName);
        string password = Encryption.DecryptFromBase64(settings.AccountInfo.Password);
        args.Append($" obj= \"{userName.ToQuery()}\" password= \"{password.ToQuery()}\"");
    }

    private static ProcessStartInfo GetCreateInfo(string args) {
        var startInfo = new ProcessStartInfo {
            FileName        = "sc.exe",
            WindowStyle     = ProcessWindowStyle.Hidden,
            UseShellExecute = true,
            Arguments       = args
        };
        return startInfo;
    }
}