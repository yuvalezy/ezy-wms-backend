using System;
using System.ComponentModel;
using System.Configuration.Install;
using System.Text;

namespace Service; 

[RunInstaller(true)]
public partial class ProjectInstaller : Installer {
    public ProjectInstaller() => InitializeComponent();

    private const string UninstallInfo = "Usage:\ninstallutil /u /db=<Database> Service.exe";
    private const string InstallInfo   = "Usage:\ninstallutil /i /db=<Database> Service.exe";

    public override void Uninstall(System.Collections.IDictionary savedState) {
        string dbName = GetParam("db");

        if (string.IsNullOrEmpty(dbName)) {
            Console.WriteLine(UninstallInfo);
            throw new InstallException("Missing parameter \"db\"");
        }

        RetrieveServiceName(dbName);
        base.Uninstall(savedState);
    }

    public override void Install(System.Collections.IDictionary stateSaver) {
        string dbName = GetParam("db");

        if (string.IsNullOrEmpty(dbName)) {
            Console.WriteLine(InstallInfo);
            throw new InstallException("Missing parameter \"db\"");
        }

        var path = new StringBuilder(Context.Parameters["assemblypath"]);
        if (path[0] != '"') {
            path.Insert(0, '"');
            path.Append('"');
        }
        path.Append($" \"{dbName}\"");
        Context.Parameters["assemblypath"] = path.ToString();

        RetrieveServiceName(dbName);

        base.Install(stateSaver);
    }


    private string GetParam(string id) {
        try {
            string lParamValue = Context?.Parameters?[id];
            if (lParamValue != null)
                return lParamValue;
        }
        catch (Exception) {
            //ignore
        }
        return string.Empty;
    }

    private void RetrieveServiceName(string dbName) {
        serviceInstaller1.ServiceName = $"{serviceInstaller1.ServiceName}-{dbName}";
        serviceInstaller1.DisplayName = $"{serviceInstaller1.DisplayName} ({dbName})";
    }
}